#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Characters;
using VisitorFramework.Framework;
using VisitorFramework.Patching;
using Utility = StardewValley.Utility;

#endregion

namespace VisitorFramework;

/// <summary>The mod entry point.</summary>
internal sealed class ModEntry : Mod
{
    internal static ModEntry Instance;
    internal BusManager BusManager;
    internal IModHelper ModHelper;

    internal Dictionary<string, VisitorData> VisitorsData = new();

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
        BusManager = new BusManager();

        // Harmony patches
        try
        {
            var harmony = new Harmony(ModManifest.UniqueID);
            var patchListsList = new List<PatchList> { new BusStopPatches(), new CharacterPatches() };
            patchListsList.ForEach(l => l.ApplyAll(harmony));
        }
        catch (Exception e)
        {
            Log.Debug($"Couldn't patch methods - {e}", LogLevel.Error);
            return;
        }

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.DayEnding += OnDayEnding;

        helper.Events.GameLoop.TimeChanged += OnTimeChanged;

        helper.Events.Content.AssetRequested += OnAssetRequested;

#if DEBUG
        helper.Events.Input.ButtonPressed += Debug.ButtonPress;
#endif
    }

    private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
    {
    }

    private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;

        BusManager.SetUp(ModHelper);

        BusManager.BusDoorOpen += BusManager.OnBusDoorOpen;
        BusManager.BusDoorOpen += delegate { SpawnVisitors(); };
    }

    private void OnDayStarted(object sender, DayStartedEventArgs e)
    {
        BusManager.DayUpdate(ModHelper);

        foreach (var pair in VisitorsData)
        {
            var npc = Game1.getCharacterFromName(pair.Key);
            if (npc == null)
                continue;
            npc.DefaultMap = "BusStop";
            npc.DefaultPosition = new Vector2(BusManager.BusDoorPosition.X * 64, BusManager.BusDoorPosition.Y * 64);
            npc.TryLoadSchedule();
            npc.reloadSprite();
            npc.Position = new Vector2(-1000, -1000);
        }
    }

    private void OnDayEnding(object sender, DayEndingEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;
    }

    private void OnTimeChanged(object sender, TimeChangedEventArgs e)
    {
        if (e.NewTime == 610 || Utility.CalculateMinutesBetweenTimes(e.NewTime, BusManager.LastArrivalTime) == -20)
            BusManager.BusLeave();
        else if (Utility.CalculateMinutesBetweenTimes(e.NewTime, BusManager.NextArrivalTime) == 10) 
            BusManager.BusReturn();
        //Game1.realMilliSecondsPerGameMinute = 300;
        //Game1.realMilliSecondsPerGameTenMinutes = 3000;
    }

    private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
    {
        if (e.Name.IsEquivalentTo("Data/Characters"))
        {
            e.Edit(asset =>
                {
                    foreach (var pair in asset.AsDictionary<string, CharacterData>().Data)
                        if (pair.Value is { Home.Count: > 0 } && pair.Value.Home[0].Location.Equals("BusStop"))
                        {
                            Log.Debug($"Adding bus visitor {pair.Key}");
                            pair.Value.Home[0].Tile = new Point(-100, -100);
                            VisitorsData.Add(pair.Key, new VisitorData());
                        }
                },
                AssetEditPriority.Late);
        }
        else if (e.Name.IsDirectlyUnderPath("Characters/schedules"))
        {
            var npcName = e.Name.BaseName.Replace("Characters/schedules/", "");

            if (!VisitorsData.ContainsKey(npcName))
                return;

            e.Edit(asset =>
                {
                    var assetData = asset.AsDictionary<string, string>();

                    var visitorData = VisitorsData[npcName];

                    var busTimeRegex = new Regex(@"Bus([0123])");
                    foreach (var entry in assetData.Data)
                    {
                        var split = NPC.SplitScheduleCommands(entry.Value);
                        var index = 0;

                        if (split[0].StartsWith("NOT friendship"))
                            index = 1;
                        else if (split[0].StartsWith("MAIL")) index = 2;

                        var m = busTimeRegex.Match(split[index].Split(' ')[0]);
                        if (m.Success)
                        {
                            var busArrivalIndex = int.Parse(m.Groups[1].Value);

                            split[index] = split[index].Replace(m.Value, BusManager.BusArrivalTimes[busArrivalIndex].ToString());

                            // Leaving time (Look at last command in schedule entry)
                            int busDepartureIndex;
                            var lastCommand = split[^1];
                            var d = busTimeRegex.Match(lastCommand);
                            if (d.Success && split.Length > index + 1)
                            {
                                busDepartureIndex = int.Parse(d.Groups[1].Value);
                            }
                            else
                            {
                                busDepartureIndex = 4;
                                split = split.AddItem("").ToArray();
                            }

                            split[^1] = "a" + BusManager.BusArrivalTimes[busDepartureIndex] +
                                        $" BusStop {BusManager.BusDoorPosition.X} {BusManager.BusDoorPosition.Y} 0 BoardBus";

                            visitorData.ScheduleKeysForBusArrival.Add(entry.Key, (busArrivalIndex, busDepartureIndex));

                            // Perform Edit on the asset
                            assetData.Data[entry.Key] = string.Join('/', split);
                        }
                    }
                },
                AssetEditPriority.Late
            );
        }
    }

    internal void SpawnVisitors()
    {
        foreach (var pair in VisitorsData)
        {
            var npc = Game1.getCharacterFromName(pair.Key);
            if (npc?.ScheduleKey == null || !pair.Value.ScheduleKeysForBusArrival.TryGetValue(npc.ScheduleKey, out var arrivalDepartureIndices))
            {
                Log.Debug($"Visitor {pair.Key} hasn't spawned or schedule doesn't have a schedule for bus");
                continue;
            }

            if (arrivalDepartureIndices.Item1 == BusManager.BusArrivalsToday - 1)
            {
                npc.Position = new Vector2(BusManager.BusDoorPosition.X * 64, BusManager.BusDoorPosition.Y * 64);
                npc.checkSchedule(BusManager.LastArrivalTime);
            }
        }
    }

    public void CharacterReachBusEndBehavior(Character c, GameLocation location)
    {
        if (c is NPC npc)
        {
            npc.Position = new Vector2(-1000, -1000);
            npc.controller = null;
            npc.followSchedule = false;
        }
    }
}