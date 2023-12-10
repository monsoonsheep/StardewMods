#region Usings

using System.Collections.Generic;
using StardewValley;
using StardewValley.Pathfinding;

#endregion

namespace BusSchedules.Patching;

internal class CharacterPatches : PatchCollection
{
    public CharacterPatches()
    {
        Patches = new List<Patch>
        {
            new(
                typeof(NPC),
                "getRouteEndBehaviorFunction",
                new[] { typeof(string), typeof(string) },
                postfix: nameof(NpcGetRouteEndBehaviorPostfix))
        };
    }

    private static void NpcGetRouteEndBehaviorPostfix(NPC __instance, string behaviorName, string endMessage, ref PathFindController.endBehavior __result)
    {
        if (__result == null && BusSchedules.Instance.VisitorsData.ContainsKey(__instance.Name) && __instance.Schedule != null && behaviorName == "BoardBus")
            __result = BusSchedules.Instance.CharacterReachBusEndBehavior;
    }
}