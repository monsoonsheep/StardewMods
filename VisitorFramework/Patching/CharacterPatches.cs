#region Usings

using System.Collections.Generic;
using StardewValley;
using StardewValley.Pathfinding;

#endregion

namespace VisitorFramework.Patching;

internal class CharacterPatches : PatchList
{
    public CharacterPatches()
    {
        Patches = new List<Patch>
        {
            new(
                typeof(NPC),
                "getRouteEndBehaviorFunction",
                new[] { typeof(string), typeof(string) },
                postfix: nameof(NpcGetRoundEndBehaviorPostfix))
        };
    }

    internal static void NpcGetRoundEndBehaviorPostfix(NPC __instance, string behaviorName, string endMessage, ref PathFindController.endBehavior __result)
    {
        if (__result == null && ModEntry.Instance.VisitorsData.ContainsKey(__instance.Name) && __instance.Schedule != null && behaviorName == "BoardBus")
            __result = ModEntry.Instance.CharacterReachBusEndBehavior;
    }
}