using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BusSchedules.Patching;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Characters;
using StardewValley.Locations;
using StardewValley.Pathfinding;
using BusSchedules.Interfaces;
using MonsoonSheep.Stardew.Common.Patching;
using xTile.Tiles;

namespace BusSchedules;

/// <summary>The mod entry point.</summary>
internal sealed class Mod : StardewModdingAPI.Mod
{
    internal static Mod Instance = null!;
    internal static string UniqueId = null!;

    internal BusManager BusManager = null!;

    internal bool BusEnabled;
    internal readonly Dictionary<string, VisitorData> VisitorsData = [];
    internal readonly PriorityQueue<NPC, int> VisitorsForNextArrival = new();

    internal byte BusArrivalsToday;
    internal int[] BusArrivalTimes = [630, 1200, 1500, 1800, 2400];
    internal static Point BusDoorTile = new Point(12, 9);

    private static Tile RoadTile = null!;
    private static Tile LineTile = null!;
    private static Tile ShadowTile = null!;

    private readonly WeakReference<BusStop?> BusLocationRef = new WeakReference<BusStop?>(null);

    internal static BusStop BusLocation
    {
        get
        {
            if (Instance.BusLocationRef.TryGetTarget(out BusStop? b))
                return b;

            Log.Trace("Bus Location updating");
            BusStop busStop = (BusStop) Game1.getLocationFromName("BusStop");
            Instance.UpdateBusLocation(busStop);
            return busStop;
        }
        set => Instance.BusLocationRef.SetTarget(value);
    }

    internal int NextArrivalTime 
        => this.BusArrivalsToday < this.BusArrivalTimes.Length ? this.BusArrivalTimes[this.BusArrivalsToday] : 99999;

    internal int LastArrivalTime 
        => this.BusArrivalsToday == 0 ? 0 : this.BusArrivalTimes[this.BusArrivalsToday - 1];

    internal int TimeUntilNextArrival 
        => Utility.CalculateMinutesBetweenTimes(Game1.timeOfDay, this.NextArrivalTime);

    internal int TimeSinceLastArrival 
        => Utility.CalculateMinutesBetweenTimes(this.LastArrivalTime, Game1.timeOfDay);

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        Instance = this;
        Log.Monitor = this.Monitor;
        UniqueId = this.ModManifest.UniqueID;

        // Harmony patches
        if (HarmonyPatcher.TryApply(this,
                new BusStopPatcher(),
                new SchedulePatcher()
            ) is false)
            return;

        IModEvents events = this.Helper.Events;
        events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
        events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;
        events.Content.AssetRequested += this.OnAssetRequested;

#if DEBUG
        helper.Events.Input.ButtonPressed += Debug.ButtonPress;
#endif
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.BusManager = new BusManager();
        this.UpdateBusLocation((BusStop) Game1.getLocationFromName("BusStop"));

        if (this.BusEnabled == false && Game1.MasterPlayer.mailReceived.Contains("ccVault"))
        {
            Log.Debug("Enabling bus");
            this.BusEnabled = true;
            this.HookEvents();
        }

        if (Context.IsMainPlayer && this.VisitorsData.Count == 0)
        {
            // Force load NPC schedules so they can be edited by AssetRequested
            Utility.ForEachCharacter(delegate(NPC n)
            {
                _ = n.getMasterScheduleRawData();
                return true;
            });
        }

    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        this.UnhookEvents();
        this.BusEnabled = false;
        this.BusArrivalsToday = 0;
        this.BusManager.State = BusState.Parked;
    }

    private void HookEvents()
    {
        // Hooks for host and clients
        BusEvents.BusArrive += this.BusManager.OnDoorOpen;

        if (Context.IsMainPlayer)
        {
            // Hooks for host only
            this.Helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            this.Helper.Events.GameLoop.Saving += this.OnSaving;
            this.Helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;
            BusEvents.BusArrive += this.SpawnVisitors;
            BusEvents.BusArrive += this.BusManager.PamBackToSchedule;
        }
    }

    private void UnhookEvents()
    {
        // Clients and Host
        BusEvents.BusArrive -= this.BusManager.OnDoorOpen;

        // Host only
        this.Helper.Events.GameLoop.DayStarted -= this.OnDayStarted;
        this.Helper.Events.GameLoop.Saving -= this.OnSaving;
        this.Helper.Events.GameLoop.TimeChanged -= this.OnTimeChanged;

        BusEvents.BusArrive -= this.SpawnVisitors;
        BusEvents.BusArrive -= this.BusManager.PamBackToSchedule;
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (this.BusEnabled == false && Game1.MasterPlayer.mailReceived.Contains("ccVault"))
        {
            Log.Debug("Enabling bus");
            this.BusEnabled = true;
            this.HookEvents();
        }

        this.BusArrivalsToday = 0;

        foreach (var pair in this.VisitorsData)
        {
            var npc = Game1.getCharacterFromName(pair.Key);
            if (npc == null)
                continue;

            // If the game hasn't loaded the NPC's schedule today, force load it
            if (string.IsNullOrEmpty(npc.ScheduleKey))
            {
                npc.TryLoadSchedule();
                if (string.IsNullOrEmpty(npc.ScheduleKey))
                    continue;
            }

            // If their home is set to bus stop or they , warp them to bus stop
            if (npc.GetData().Home is { Count: > 0 } &&
                npc.GetData().Home[0].Location == "BusStop")
            {
                Log.Debug($"Setting character {pair.Key} to their bus stop home out of map");
                Game1.warpCharacter(npc, BusLocation, new Vector2(-10000f / 64, -10000f / 64));
            }

            // If NPC will arrive by bus today, warp them to a hidden place in bus stop
            if (pair.Value.ScheduleKeysForBusArrival.ContainsKey(npc.ScheduleKey))
            {
                Log.Debug($"Setting character {pair.Key} to the bus stop");
                Game1.warpCharacter(npc, BusLocation, new Vector2(-10000f / 64, -10000f / 64));
            }
            else
            {
                Log.Debug($"Letting the game warp character {pair.Key} to their home");
                // Otherwise let the game warp them to their home
                npc.dayUpdate(Game1.dayOfMonth);
            }
        }

        if (this.BusEnabled)
        {
            // Early morning bus departure
            this.BusLeave();

        }
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        if (Context.IsMainPlayer)
            this.BusManager.ParkBus();
    }

    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        if (Utility.CalculateMinutesBetweenTimes(e.NewTime, this.NextArrivalTime) == 10)
        {
            foreach (var pair in this.VisitorsData)
            {
                var npc = Game1.getCharacterFromName(pair.Key);
                if (npc?.ScheduleKey == null || !pair.Value.ScheduleKeysForBusArrival.TryGetValue(npc.ScheduleKey, out var arrivalDepartureIndices))
                {
                    continue;
                }

                if (arrivalDepartureIndices.Item1 == this.NextArrivalTime)
                {
                    this.VisitorsForNextArrival.Enqueue(npc, 0);
                }
            }

            this.BusArrivalsToday++;
            this.BusManager.DriveIn();
        }
        // Bus leaves based on leaving times
        else if (this.BusManager.State == BusState.Parked
                 && ((this.BusArrivalsToday == 1 && e.NewTime == this.NextArrivalTime - 70)
                     || this.BusArrivalsToday != 1 && e.NewTime == this.LastArrivalTime + 20))
        {
            this.BusLeave();
        }
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;

        if (e.Name.IsDirectlyUnderPath("Characters/schedules"))
        {
            string npcName = e.Name.BaseName.Replace("Characters/schedules/", "");

            e.Edit(asset =>
                {
                    var assetData = asset.AsDictionary<string, string>();
                    List<string> keysEdited = new List<string>();

                    var busTimeRegex = new Regex(@"b(\d{3,})");
                    foreach (var entry in assetData.Data)
                    {
                        string[] splitCommands = NPC.SplitScheduleCommands(entry.Value);
                        int startIndex = 0;

                        if (splitCommands[0].StartsWith("NOT friendship"))
                            startIndex = 1;
                        else if (splitCommands[0].StartsWith("MAIL"))
                            startIndex = 2;

                        var arrivalTimeMatch = busTimeRegex.Match(splitCommands[startIndex].Split(' ')[0]);
                        if (!arrivalTimeMatch.Success)
                            continue;

                        VisitorData visitorData;
                        if (!this.VisitorsData.ContainsKey(npcName))
                        {
                            visitorData = new VisitorData();
                            this.VisitorsData.Add(npcName, visitorData);
                        }
                        else
                        {
                            visitorData = this.VisitorsData[npcName];
                        }

                        int arrivalTime = int.Parse(arrivalTimeMatch.Groups[1].Value);

                        if (!this.BusArrivalTimes.Contains(arrivalTime) || arrivalTime == this.BusArrivalTimes[^1])
                        {
                            Log.Error($"Invalid time given in schdule for {npcName} in key {entry.Key}: {entry.Value}");
                            arrivalTime = this.BusArrivalTimes[0];
                        }

                        splitCommands[startIndex] = splitCommands[startIndex]
                            .Replace(arrivalTimeMatch.Groups[0].Value, arrivalTimeMatch.Groups[1].Value);

                        // Leaving time (Look at last command in schedule entry)
                        int busDepartureTime;

                        Match departureTimeMatch = busTimeRegex.Match(splitCommands[^1]);
                        if (departureTimeMatch.Success && splitCommands.Length > startIndex + 1 && this.BusArrivalTimes.Contains(int.Parse(departureTimeMatch.Groups[1].Value)))
                        {
                            busDepartureTime = int.Parse(departureTimeMatch.Groups[1].Value);
                        }
                        else
                        {
                            busDepartureTime = this.BusArrivalTimes.Last();
                            splitCommands = splitCommands.AddItem("").ToArray();
                        }

                        // Last command will be a<time> so they arrive at the bus by the given time
                        splitCommands[^1] = "a" + (busDepartureTime + 10) +
                                                    $" BusStop 12 9 0 BoardBus";

                        // Perform Edit on the asset
                        assetData.Data[entry.Key] = string.Join('/', splitCommands);
                        keysEdited.Add(entry.Key);
                        visitorData.ScheduleKeysForBusArrival.Add(entry.Key, (arrivalTime, busDepartureTime));
                    }

                    if (keysEdited.Count > 0)
                        Log.Info($"Edited schedule for {npcName} in keys [{string.Join(',', keysEdited)}]");
                },
                AssetEditPriority.Late
            );
        }
    }

    private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (!Context.IsMainPlayer && e.FromModID.Equals(this.ModManifest.UniqueID) && e.Type.Equals("BusSchedules"))
        {
            this.UpdateBusLocation((BusStop) Game1.getLocationFromName("BusStop"));
            string? data = e.ReadAs<string>();
            switch (data)
            {
                case "BusDoorClose":
                    Log.Debug("Received message to close door and drive away");
                    this.BusManager.DriveOut();
                    break;
                case "BusDriveBack":
                    Log.Debug("Received message to drive bus in");
                    this.BusManager.DriveIn();
                    break;
            }
        }
    }

    internal static void SendMessageToClient(string message)
    {
        Instance.Helper.Multiplayer.SendMessage(message, "BusSchedules", new [] { UniqueId });
    }

    /// <summary>
    ///     Call when all visitors have entered the bus and Pam is standing in position. This will make Pam get in and drive
    ///     away
    /// </summary>
    internal void BusLeave()
    {
        NPC pam = Game1.getCharacterFromName("Pam");
        if (!BusLocation.characters.Contains(pam) || pam.TilePoint is not { X: 11, Y: 10 })
            this.BusManager.DriveOut();
        else
            pam.temporaryController = new PathFindController(pam, BusLocation, Mod.BusDoorTile, 3, delegate(Character c, GameLocation _)
            {
                if (c is NPC p)
                    p.Position = new Vector2(-1000f, -1000f);

                this.BusManager.DriveOut();
            });
    }

    private void SpawnVisitors(object? sender, EventArgs e)
    {
        int count = 0;
        while (this.VisitorsForNextArrival.TryDequeue(out NPC? visitor, out int priority))
        {
            Game1.delayedActions.Add(new DelayedAction(count * 800 + Game1.random.Next(0, 100), delegate
            {
                visitor.Position = BusDoorTile.ToVector2() * 64f;

                if (visitor.IsReturningToEndPoint())
                {
                    AccessTools.Field(typeof(NPC), "returningToEndPoint").SetValue(visitor, false);
                    AccessTools.Field(typeof(Character), "freezeMotion").SetValue(visitor, false);
                }
                else
                {
                    visitor.checkSchedule(this.LastArrivalTime);
                }

                Log.Debug($"Visitor {visitor.displayName} arrived at {Game1.timeOfDay}");
            }));
            count++;
        }
    }

    internal void UpdateBusLocation(BusStop location)
    {
        BusLocation = location;
        this.BusManager.BusDoorField = this.Helper.Reflection.GetField<TemporaryAnimatedSprite>(location, "busDoor");

        TileArray tiles = location.Map.GetLayer("Buildings").Tiles;
        RoadTile = tiles[12, 7];
        LineTile = tiles[12, 8];
        ShadowTile = tiles[13, 8];
    }

    public static void VisitorReachBusEndBehavior(Character c, GameLocation location)
    {
        if (c is NPC npc)
        {
            Log.Info($"Visitor {npc.displayName} left at {Game1.timeOfDay} ");
            npc.Position = new Vector2(-1000, -1000);
            npc.controller = null;
            npc.followSchedule = false;
        }
    }

    public override object GetApi()
    {
        return new Api();
    }

    internal static void RemoveTiles()
    {
        TileArray tiles = BusLocation.Map.GetLayer("Buildings").Tiles;
        for (int i = 11; i <= 18; i++)
        {
            for (int j = 7; j <= 9; j++)
            {
                tiles[i, j] = null;
            }
        }
    }

    internal static void ResetTiles()
    {
        TileArray tiles = BusLocation.Map.GetLayer("Buildings").Tiles;
        for (int i = 11; i <= 18; i++)
        {
            for (int j = 7; j <= 9; j++)
            {
                if (j == 7 || j == 9)
                    tiles[i, j] = RoadTile;
                else if (j == 8)
                    tiles[i, j] = LineTile;
            }
        }
        tiles[13, 8] = ShadowTile;
        tiles[16, 8] = ShadowTile;
        tiles[12, 9] = null;
    }
}
