#region Usings

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Pathfinding;

#endregion

namespace BusSchedules.Patching;

internal class CharacterPatches : PatchCollection
{
    private static readonly BusManager Bm = BusSchedules.Instance.BusManager;

    public CharacterPatches()
    {
        Patches = new List<Patch>
        {
            new(
                typeof(NPC),
                "checkSchedule",
                new [] { typeof(int) },
                postfix: nameof(NpcCheckSchedulePostfix)),
            new(
                typeof(NPC),
                "getRouteEndBehaviorFunction",
                new[] { typeof(string), typeof(string) },
                postfix: nameof(NpcGetRouteEndBehaviorPostfix))
        };
    }

    private static void NpcCheckSchedulePostfix(NPC __instance, int timeOfDay)
    {
        if (__instance.Name.Equals("Pam"))
        {
            if (__instance.currentLocation.Equals(Bm.BusLocation) && __instance.controller != null &&
                __instance.controller.pathToEndPoint.TryPeek(out Point result) && result is { X: 12, Y: 9 } &&
                timeOfDay == __instance.DirectionsToNewLocation.time)
            {
                __instance.Position = result.ToVector2() * 64f;
            }
        }
    }

    private static void NpcGetRouteEndBehaviorPostfix(NPC __instance, string behaviorName, string endMessage, ref PathFindController.endBehavior __result)
    {
        if (__result == null && BusSchedules.Instance.VisitorsData.ContainsKey(__instance.Name) && __instance.Schedule != null && behaviorName == "BoardBus")
            __result = BusSchedules.Instance.CharacterReachBusEndBehavior;
    }
}