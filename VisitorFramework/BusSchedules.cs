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

namespace BusSchedules;

/// <summary>The mod entry point.</summary>
internal sealed class BusSchedules : Mod
{
    internal static BusSchedules Instance;
    internal BusManager BusManager;
    internal IModHelper ModHelper;

    internal readonly Dictionary<string, VisitorData> VisitorsData = new();

    internal class VisitorData
    {
        internal Dictionary<string, (int, int)> ScheduleKeysForBusArrival = new();
        internal IDictionary<string, string> CachedMasterScheduleData;
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
            var patchListsList = new List<PatchCollection> { new BusStopPatches(), new CharacterPatches() };
            patchListsList.ForEach(l => l.ApplyAll(harmony));
        }
        catch (Exception e)
        {
            Log.Error($"Couldn't patch methods - {e}");
            return;
        }

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.Saving += OnSaving;

        helper.Events.GameLoop.TimeChanged += OnTimeChanged;

        if (Context.IsMainPlayer)
        {
            helper.Events.Content.AssetRequested += OnAssetRequested;
        }

#if DEBUG
        helper.Events.Input.ButtonPressed += Debug.ButtonPress;
#endif
    }

    private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
    {
        Events.BusArrive += BusManager.OnDoorOpen;

        if (!Context.IsMainPlayer)
            return;

        Events.BusArrive += (_, _) => SpawnVisitors();
        Events.BusArrive += (_, _) => BusManager.PamBackToSchedule();
    }

    private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
    {
        BusManager.SetUp(ModHelper);

        if (!Context.IsMainPlayer)
            return;

        if (!Context.IsMultiplayer)
        {
            Game1.options.pauseWhenOutOfFocus = false;
        }
    }

    private void OnDayStarted(object sender, DayStartedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;

        BusManager.DayUpdate(ModHelper);

        foreach (var pair in VisitorsData)
        {
            var npc = Game1.getCharacterFromName(pair.Key);
            if (npc == null)
                continue;

            var npcData = npc.GetData();

            if (npcData.Home is { Count: > 0 } && npcData.Home[0].Location == "BusStop")
            {
                npc.Position = new Vector2(-1000, -1000);
            }

            if ((!string.IsNullOrEmpty(npc.ScheduleKey) && pair.Value.ScheduleKeysForBusArrival.ContainsKey(npc.ScheduleKey)))
            {
                if (!BusManager.BusLocation.characters.Contains(npc))
                {
                    Game1.warpCharacter(npc, BusManager.BusLocation, BusManager.BusPosition);
                }

                string defaultMap = npc.DefaultMap;
                Vector2 defaultPosition = npc.DefaultPosition;

                npc.DefaultMap = "BusStop";
                npc.DefaultPosition = BusManager.BusDoorPosition.ToVector2() * 64;

                npc.TryLoadSchedule();
                npc.reloadSprite();

                npc.DefaultMap = defaultMap;
                npc.DefaultPosition = defaultPosition;

                npc.Position = new Vector2(-1000, -1000);
            }
        }
    }

    private void OnSaving(object sender, SavingEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;
        
        BusManager.StopBus(animate: false);
    }

    private void OnTimeChanged(object sender, TimeChangedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;

        if (e.NewTime == 610 || Utility.CalculateMinutesBetweenTimes(e.NewTime, BusManager.NextArrivalTime) == 60)
            BusManager.BusLeave();
        else if (BusManager.BusArrivalsToday < BusManager.BusArrivalTimes.Length && Utility.CalculateMinutesBetweenTimes(e.NewTime, BusManager.NextArrivalTime) == 10)
            BusManager.BusReturn();

        //Game1.realMilliSecondsPerGameMinute = 300;
        //Game1.realMilliSecondsPerGameTenMinutes = 3000;
    }

    private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
    {
        if (e.Name.IsDirectlyUnderPath("Characters/schedules"))
        {
            var npcName = e.Name.BaseName.Replace("Characters/schedules/", "");

            e.Edit(asset =>
                {
                    var assetData = asset.AsDictionary<string, string>();

                    if (VisitorsData.TryGetValue(npcName, out var existingEntry))
                    {
                        // If the incoming and existing are equal
                        if (existingEntry.CachedMasterScheduleData.Count == assetData.Data.Count && !existingEntry.CachedMasterScheduleData.Except(assetData.Data).Any())
                        {
                            Log.Debug("Schedule cached");
                            foreach (var entry in existingEntry.CachedMasterScheduleData)
                                assetData.Data[entry.Key] = entry.Value;
                            return;
                        }
                    }
                    
                    List<string> keysEdited = new List<string>();

                    var busTimeRegex = new Regex(@"b(\d+)");
                    foreach (var entry in assetData.Data)
                    {
                        var split = NPC.SplitScheduleCommands(entry.Value);
                        var index = 0;

                        if (split[0].StartsWith("NOT friendship"))
                            index = 1;
                        else if (split[0].StartsWith("MAIL")) index = 2;

                        var firstMatch = busTimeRegex.Match(split[index].Split(' ')[0]);
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

                            split[index] = split[index].Replace(firstMatch.Groups[0].Value, firstMatch.Groups[1].Value);

                            // Leaving time (Look at last command in schedule entry)
                            int busDepartureTime;
                            var lastCommand = split[^1];
                            var d = busTimeRegex.Match(lastCommand);
                            if (d.Success && split.Length > index + 1)
                            {
                                busDepartureTime = int.Parse(d.Groups[1].Value);
                            }
                            else
                            {
                                busDepartureTime = BusManager.BusArrivalTimes[^1];
                                split = split.AddItem("").ToArray();
                            }

                            if (!BusManager.BusArrivalTimes.Contains(busArrivalTime) || !BusManager.BusArrivalTimes.Contains(busDepartureTime))
                            {
                                Log.Error($"Invalid time given in schdule for {npcName} in key {entry.Key}: {entry.Value}");
                                return;
                            }

                            split[^1] = "a" + (busDepartureTime + 10) +
                                        $" BusStop 12 9 0 BoardBus";

                            // Perform Edit on the asset
                            assetData.Data[entry.Key] = string.Join('/', split);
                            keysEdited.Add(entry.Key);
                            visitorData.ScheduleKeysForBusArrival.Add(entry.Key, (busArrivalTime, busDepartureTime));

                            // Cached the dictionary so we don't have to edit it again
                            visitorData.CachedMasterScheduleData = assetData.Data;
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

    private void SpawnVisitors()
    {
        foreach (var pair in VisitorsData)
        {
            var npc = Game1.getCharacterFromName(pair.Key);
            if (npc?.ScheduleKey == null || !pair.Value.ScheduleKeysForBusArrival.TryGetValue(npc.ScheduleKey, out var arrivalDepartureIndices))
            {
                continue;
            }

            if (arrivalDepartureIndices.Item1 == BusManager.LastArrivalTime)
            {
                npc.Position = new Vector2(BusManager.BusDoorPosition.X * 64, BusManager.BusDoorPosition.Y * 64);
                npc.checkSchedule(BusManager.LastArrivalTime);
                Log.Info($"Visitor {npc.displayName} arrived");
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