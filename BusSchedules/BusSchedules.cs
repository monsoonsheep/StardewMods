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
using static BusSchedules.BusSchedules;

namespace BusSchedules;

/// <summary>The mod entry point.</summary>
internal sealed class BusSchedules : Mod
{
    internal static BusSchedules Instance;
    internal BusManager BusManager;
    internal static IModHelper ModHelper;
    internal new static IManifest ModManifest;

    internal readonly Dictionary<string, VisitorData> VisitorsData = new();

    internal byte BusArrivalsToday;
    internal int[] BusArrivalTimes = { 630, 1200, 1500, 1800, 2400 };
    internal List<int> BusLeaveTimes = new List<int>();

    internal int NextArrivalTime => BusArrivalsToday < BusArrivalTimes.Length ? BusArrivalTimes[BusArrivalsToday] : 9999;
    internal int LastArrivalTime => BusArrivalsToday == 0 ? 0 : BusArrivalTimes[BusArrivalsToday - 1];
    internal int TimeUntilNextArrival => Utility.CalculateMinutesBetweenTimes(Game1.timeOfDay, NextArrivalTime);
    internal int TimeSinceLastArrival => Utility.CalculateMinutesBetweenTimes(LastArrivalTime, Game1.timeOfDay);


    internal class VisitorData
    {
        internal Dictionary<string, (int, int)> ScheduleKeysForBusArrival = new();
    }

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        Instance = this;
        ModHelper = helper;
        Log.Monitor = Monitor;
        ModManifest = base.ModManifest;

        BusManager = new BusManager();

        // Harmony patches
        try
        {
            var harmony = new Harmony(ModManifest.UniqueID);
            var patchListsList = new List<PatchCollection>
            {
                new BusStopPatches(), 
                new CharacterPatches()
            };
            patchListsList.ForEach(l => l.ApplyAll(harmony));
        }
        catch (Exception e)
        {
            Log.Error($"Couldn't patch methods - {e}");
            return;
        }

        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
        helper.Events.Content.AssetRequested += OnAssetRequested;
        ModHelper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;

#if DEBUG
        helper.Events.GameLoop.SaveLoaded += Debug.SetUp;
        helper.Events.Input.ButtonPressed += Debug.ButtonPress;
#endif
    }

    private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
    {
        // Clients and Host
        ModHelper.Events.Player.Warped -= OnWarped;
        ModHelper.Events.World.LocationListChanged -= OnLocationListChanged;
        Events.BusArrive -= BusManager.OnDoorOpen;

        // Host only
        ModHelper.Events.GameLoop.Saving -= OnSaving;
        ModHelper.Events.GameLoop.TimeChanged -= OnTimeChanged;
        Events.BusArrive -= OnBusArrive;
        Events.BusArrive -= BusManager.PamBackToSchedule;
        ModHelper.Events.GameLoop.DayStarted -= OnDayStarted;

        BusLeaveTimes.Clear();
        BusArrivalsToday = 0;
        BusManager.BusGone = false;
        BusManager.BusLeaving = false;
        BusManager.BusReturning = false;
    }

    private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
    {
        BusManager.UpdateLocation(ModHelper, (BusStop) Game1.getLocationFromName("BusStop"));

        if (Game1.MasterPlayer.mailReceived.Contains("ccVault"))
        {
            // Hooks for host and clients
            ModHelper.Events.Player.Warped += OnWarped;
            ModHelper.Events.World.LocationListChanged += OnLocationListChanged;
            Events.BusArrive += BusManager.OnDoorOpen;

            if (Context.IsMainPlayer)
            {
                // Hooks for host only
                ModHelper.Events.GameLoop.DayStarted += OnDayStarted;
                ModHelper.Events.GameLoop.Saving += OnSaving;
                ModHelper.Events.GameLoop.TimeChanged += OnTimeChanged;
                Events.BusArrive += OnBusArrive;
                Events.BusArrive += BusManager.PamBackToSchedule;
            }
        }

        if (Context.IsMainPlayer)
        {
            if (VisitorsData.Count == 0)
            {
                // Force load NPC schedules so they can be edited by AssetRequested
                Utility.ForEachCharacter(delegate(NPC n)
                {
                    _ = n.getMasterScheduleRawData();
                    return true;
                });
            }
        }
    }

    private void OnDayStarted(object sender, DayStartedEventArgs e)
    {
        BusArrivalsToday = 0;

        foreach (var pair in VisitorsData)
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
                Game1.warpCharacter(npc, BusManager.BusLocation, new Vector2(-10000f / 64, -10000f / 64));
            }

            // If NPC will arrive by bus today, warp them to a hidden place in bus stop
            if (pair.Value.ScheduleKeysForBusArrival.ContainsKey(npc.ScheduleKey))
            {
                Game1.warpCharacter(npc, BusManager.BusLocation, new Vector2(-10000f / 64, -10000f / 64));
            }
            else
            {
                // Otherwise let the game warp them to their home
                npc.dayUpdate(Game1.dayOfMonth);
            }
        }

        if (Game1.player.mailReceived.Contains("ccVault"))
        {
            // Early morning bus departure
            BusManager.BusLeave();
        }

        // Populate bus departure times for the day
        for (var i = 0; i < BusArrivalTimes.Length; i++)
        {
            if (i == 0)
                BusLeaveTimes.Add(Utility.ModifyTime(BusArrivalTimes[1], -70));
                //BusManager.BusLeaveTimes.Add(Utility.ModifyTime(BusManager.BusArrivalTimes[i], 50));
            else
                BusLeaveTimes.Add(BusArrivalTimes[i] + 20);
        }
    }

    private void OnSaving(object sender, SavingEventArgs e)
    {
        if (Context.IsMainPlayer)
            BusManager.ResetBus();
    }

    private void OnTimeChanged(object sender, TimeChangedEventArgs e)
    {
        if (Utility.CalculateMinutesBetweenTimes(e.NewTime, NextArrivalTime) == 10)
        {
            BusArrivalsToday++;
            BusManager.BusReturn();
        }
        // Bus leaves based on leaving times
        else if (BusLeaveTimes.Contains(e.NewTime))
        {
            BusManager.BusLeave();
        }
    }

    private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;

        if (e.Name.IsDirectlyUnderPath("Characters/schedules"))
        {
            var npcName = e.Name.BaseName.Replace("Characters/schedules/", "");

            e.Edit(asset =>
                {
                    var assetData = asset.AsDictionary<string, string>();
                    List<string> keysEdited = new List<string>();

                    var busTimeRegex = new Regex(@"b(\d{3,})");
                    foreach (var entry in assetData.Data)
                    {
                        var splitScheduleCommands = NPC.SplitScheduleCommands(entry.Value);
                        var startIndex = 0;

                        if (splitScheduleCommands[0].StartsWith("NOT friendship"))
                            startIndex = 1;
                        else if (splitScheduleCommands[0].StartsWith("MAIL")) startIndex = 2;

                        var firstMatch = busTimeRegex.Match(splitScheduleCommands[startIndex].Split(' ')[0]);
                        if (firstMatch.Success)
                        {
                            VisitorData visitorData;
                            if (!VisitorsData.ContainsKey(npcName))
                            {
                                visitorData = new VisitorData();
                                VisitorsData.Add(npcName, visitorData);
                            }
                            else
                            {

                                visitorData = VisitorsData[npcName];
                            }

                            int busArrivalTime = int.Parse(firstMatch.Groups[1].Value);

                            if (!BusArrivalTimes.Contains(busArrivalTime) || busArrivalTime == BusArrivalTimes[^1])
                            {
                                Log.Error($"Invalid time given in schdule for {npcName} in key {entry.Key}: {entry.Value}");
                                busArrivalTime = BusArrivalTimes[0];
                            }

                            splitScheduleCommands[startIndex] = splitScheduleCommands[startIndex].Replace(firstMatch.Groups[0].Value, firstMatch.Groups[1].Value);

                            // Leaving time (Look at last command in schedule entry)
                            int busDepartureTime;
                            var d = busTimeRegex.Match(splitScheduleCommands[^1]);

                            if (d.Success && splitScheduleCommands.Length > startIndex + 1 && BusArrivalTimes.Contains(int.Parse(d.Groups[1].Value)))
                            {
                                busDepartureTime = int.Parse(d.Groups[1].Value);
                            }
                            else
                            {
                                busDepartureTime = BusArrivalTimes.Last();
                                splitScheduleCommands = splitScheduleCommands.AddItem("").ToArray();
                            }

                            splitScheduleCommands[^1] = "a" + (busDepartureTime + 10) +
                                        $" BusStop 12 9 0 BoardBus";

                            // Perform Edit on the asset
                            assetData.Data[entry.Key] = string.Join('/', splitScheduleCommands);
                            keysEdited.Add(entry.Key);
                            visitorData.ScheduleKeysForBusArrival.Add(entry.Key, (busArrivalTime, busDepartureTime));
                        }
                    }

                    if (keysEdited.Count > 0)
                    {
                        Log.Info($"Edited schedule for {npcName} in keys [{string.Join(',', keysEdited)}]");
                    }
                },
                AssetEditPriority.Late
            );
        }
    }

    private void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (!Context.IsMainPlayer && e.FromModID.Equals(ModManifest.UniqueID) && e.Type.Equals("BusSchedules"))
        {
            BusManager.UpdateLocation(ModHelper, (BusStop) Game1.getLocationFromName("BusStop"));
            var data = e.ReadAs<string>();
            switch (data)
            {
                case "BusDoorClose":
                    BusManager.CloseDoor();
                    break;
                case "BusDriveBack":
                    BusManager.BusDriveBack();
                    break;
            }
        }
    }

    private void OnLocationListChanged(object sender, LocationListChangedEventArgs e)
    {
        foreach (var loc in e.Removed)
        {
            if (loc is BusStop busStop)
            {
                Log.Debug("bus stop removed");
            }
        }
    }

    private void OnWarped(object sender, WarpedEventArgs e)
    {
        if (e.IsLocalPlayer && !Context.IsMainPlayer && e.NewLocation is BusStop busStop)
        {
            Log.Debug("warped to bus");
        }
    } 

    private void OnBusArrive(object sender, EventArgs e)
    {
        int count = 0;

        foreach (var pair in VisitorsData)
        {
            var npc = Game1.getCharacterFromName(pair.Key);
            if (npc?.ScheduleKey == null || !pair.Value.ScheduleKeysForBusArrival.TryGetValue(npc.ScheduleKey, out var arrivalDepartureIndices))
            {
                continue;
            }

            if (arrivalDepartureIndices.Item1 == LastArrivalTime)
            {
                Game1.delayedActions.Add(new DelayedAction(count * 800 + Game1.random.Next(0, 100), delegate
                {
                    count++;
                    npc.Position = new Vector2(12 * 64, 9 * 64);
                    npc.checkSchedule(LastArrivalTime);
                    Log.Info($"Visitor {npc.displayName} arrived at {Game1.timeOfDay}");
                }));
            }
        }
    }

    public void VisitorReachBusEndBehavior(Character c, GameLocation location)
    {
        if (c is NPC npc)
        {
            Log.Info($"Visitor {npc.displayName} left at {Game1.timeOfDay} ");
            npc.Position = new Vector2(-1000, -1000);
            npc.controller = null;
            npc.followSchedule = false;
        }
    }
}