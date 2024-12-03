using System.Text.RegularExpressions;
using StardewMods.BusSchedules.Framework.Data;
using StardewValley.Pathfinding;

namespace StardewMods.BusSchedules.Framework.Services;

internal class NpcSchedulesIntercept : Service
{
    private static NpcSchedulesIntercept Instance = null!;

    private readonly LocationProvider locations;
    private readonly Timings busTimings;

    private readonly Regex busTimeRegex = new Regex(@"b(\d{3,})");

    // State
    private readonly Dictionary<string, VisitorData> visitorsData;

    public NpcSchedulesIntercept(
        LocationProvider locations,
        Timings busTimings,
        Dictionary<string, VisitorData> visitorsData,
        IModEvents events,
        Harmony harmony,
        ILogger logger,
        IManifest manifest)
        : base(logger, manifest)
    {
        Instance = this;

        this.locations = locations;
        this.busTimings = busTimings;
        this.visitorsData = visitorsData;

        events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        events.GameLoop.DayStarted += this.OnDayStarted;
        events.Content.AssetRequested += this.OnAssetRequested;

        harmony.Patch(
            original: AccessTools.Method(typeof(NPC), nameof(NPC.TryLoadSchedule), [typeof(string)]),
            prefix: new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(Before_TryLoadSchedule))),
            postfix: new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(After_TryLoadSchedule)))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(NPC), nameof(NPC.checkSchedule)),
            postfix: new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(After_checkSchedule)))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(NPC), "getRouteEndBehaviorFunction"),
            postfix: new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(After_getRouteEndBehaviorFunction)))
        );
    }

    /// <summary>
    /// Before TryLoadSchedule is called, change the NPC's DefaultMap and DefaultPosition to the bus location and position, storing
    /// the original in a state variable. Only if the NPC is a bus visitor
    /// </summary>
    private static bool Before_TryLoadSchedule(NPC __instance, string key, ref bool __result, out (string, Vector2)? __state)
    {
        if (Instance.visitorsData.ContainsKey(__instance.Name))
        {
            __state = new(__instance.DefaultMap, __instance.DefaultPosition);

            __instance.DefaultMap = "BusStop";
            __instance.DefaultPosition = Values.BusDoorTile.ToVector2() * 64f;
        }
        else
            __state = null;

        return true;
    }

    /// <summary>
    /// Restore the original DefaultMap and DefaultPosition after loading schedule
    /// </summary>
    private static void After_TryLoadSchedule(NPC __instance, string key, ref bool __result, ref (string, Vector2)? __state)
    {
        if (__state != null)
        {
            __instance.DefaultMap = __state.Value.Item1;
            __instance.DefaultPosition = __state.Value.Item2;
        }
    }

    /// <summary>
    /// TODO move somewhere else
    /// Warp Pam to her standing point at the time she's supposed to be there
    /// </summary>
    private static void After_checkSchedule(NPC __instance, int timeOfDay)
    {
        if (__instance.Name.Equals("Pam"))
        {
            if (__instance.currentLocation.Equals(Instance.locations.BusStop) && __instance.controller != null &&
                __instance.controller.pathToEndPoint.TryPeek(out Point result) && result is { X: 22, Y: 9 } &&
                timeOfDay == __instance.DirectionsToNewLocation.time)
            {
                __instance.Position = result.ToVector2() * 64f;
            }
        }
    }

    /// <summary>
    /// Add the "end behavior" method to an NPC's schedule if their schedule line requests BoardBus
    /// </summary>
    private static void After_getRouteEndBehaviorFunction(NPC __instance, string behaviorName, string endMessage, ref PathFindController.endBehavior? __result)
    {
        if (__result == null && Instance.visitorsData.ContainsKey(__instance.Name) && __instance.Schedule != null && behaviorName == "BoardBus")
        {
            __result = ModEntry.VisitorReachBusEndBehavior;
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

                foreach (var entry in assetData.Data)
                {
                    string[] splitCommands = NPC.SplitScheduleCommands(entry.Value);
                    int startIndex = 0;

                    if (splitCommands[0].StartsWith("NOT friendship"))
                        startIndex = 1;
                    else if (splitCommands[0].StartsWith("MAIL"))
                        startIndex = 2;

                    Match? arrivalTimeMatch = this.busTimeRegex.Match(splitCommands[startIndex].Split(' ')[0]);
                    if (!arrivalTimeMatch.Success)
                        continue;

                    VisitorData visitorData;
                    if (!this.visitorsData.ContainsKey(npcName))
                    {
                        visitorData = new VisitorData();
                        this.visitorsData.Add(npcName, visitorData);
                    }
                    else
                    {
                        visitorData = this.visitorsData[npcName];
                    }

                    int arrivalTime = int.Parse(arrivalTimeMatch.Groups[1].Value);
                    if (!Values.BusArrivalTimes.Contains(arrivalTime) || arrivalTime == Values.BusArrivalTimes[^1])
                    {
                        this.Log.Error($"Invalid time given in schdule for {npcName} in key {entry.Key}: {entry.Value}");
                        arrivalTime = Values.BusArrivalTimes[0];
                    }
                    splitCommands[startIndex] = splitCommands[startIndex]
                        .Replace(arrivalTimeMatch.Groups[0].Value, arrivalTimeMatch.Groups[1].Value);

                    // Leaving time (Look at last command in schedule entry)
                    int busDepartureTime;
                    Match departureTimeMatch = this.busTimeRegex.Match(splitCommands[^1]);
                    if (departureTimeMatch.Success && splitCommands.Length > startIndex + 1 && Values.BusArrivalTimes.Contains(int.Parse(departureTimeMatch.Groups[1].Value)))
                    {
                        busDepartureTime = int.Parse(departureTimeMatch.Groups[1].Value);
                    }
                    else
                    {
                        busDepartureTime = Values.BusArrivalTimes.Last();
                        splitCommands = [.. splitCommands, ""];
                    }

                    // Last command will be a<time> so they arrive at the bus by the given time
                    splitCommands[^1] = $"a{busDepartureTime + 10} BusStop 22 9 0 BoardBus";

                    // Perform Edit on the asset
                    assetData.Data[entry.Key] = string.Join('/', splitCommands);
                    keysEdited.Add(entry.Key);
                    visitorData.BusVisitSchedules.Add(entry.Key, (arrivalTime, busDepartureTime));
                }

                if (keysEdited.Count > 0)
                    this.Log.Info($"Edited schedule for {npcName} in keys [{string.Join(',', keysEdited)}]");
            },
                AssetEditPriority.Late
            );
        }
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (Context.IsMainPlayer && this.visitorsData.Count == 0)
        {
            // Force load NPC schedules so they can be edited by AssetRequested
            Utility.ForEachCharacter(delegate (NPC n)
            {
                _ = n.getMasterScheduleRawData();
                return true;
            });
        }
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        foreach (var pair in this.visitorsData)
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

            // If their home is set to bus stop , warp them to bus stop
            if (npc.GetData().Home is { Count: > 0 } &&
                npc.GetData().Home[0].Location == "BusStop")
            {
                this.Log.Debug($"Setting character {pair.Key} to their bus stop home out of map");
                Game1.warpCharacter(npc, this.locations.BusStop, new Vector2(-10000f / 64, -10000f / 64));
            }

            // If NPC will arrive by bus today, warp them to a hidden place in bus stop
            if (pair.Value.BusVisitSchedules.ContainsKey(npc.ScheduleKey))
            {
                this.Log.Debug($"Setting character {pair.Key} to the bus stop");
                Game1.warpCharacter(npc, this.locations.BusStop, new Vector2(-10000f / 64, -10000f / 64));
            }
            else
            {
                this.Log.Debug($"Letting the game warp character {pair.Key} to their home");
                // Otherwise let the game warp them to their home
                npc.dayUpdate(Game1.dayOfMonth);
            }
        }

    }
}
