#region Usings

using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MonsoonSheep.Stardew.Common.Patching;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Pathfinding;

#endregion

namespace BusSchedules.Patching;

internal class SchedulePatcher : BasePatcher
{
    private static BusManager Bm => Mod.Instance.BusManager;

    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(
            original: this.RequireMethod<NPC>(nameof(NPC.TryLoadSchedule), [typeof(string)]),
            prefix: this.GetHarmonyMethod(nameof(Before_TryLoadSchedule)),
            postfix: this.GetHarmonyMethod(nameof(After_TryLoadSchedule))
        );
        harmony.Patch(
            original: this.RequireMethod<NPC>(nameof(NPC.checkSchedule)),
            postfix: this.GetHarmonyMethod(nameof(After_checkSchedule))
        );
        harmony.Patch(
            original: this.RequireMethod<NPC>("getRouteEndBehaviorFunction"),
            prefix: this.GetHarmonyMethod(nameof(Before_getRouteEndBehaviorFunction))
        );
    }
    
    private static bool Before_TryLoadSchedule(NPC __instance, string key, ref bool __result, out KeyValuePair<string, Vector2> __state)
    {
        if (Mod.Instance.VisitorsData.ContainsKey(__instance.Name))
        {
            __state = new KeyValuePair<string, Vector2>(__instance.DefaultMap, __instance.DefaultPosition);
            __instance.DefaultMap = "BusStop";
            __instance.DefaultPosition = new Vector2(12, 9) * 64f;
        }

        __state = new KeyValuePair<string, Vector2>("", Vector2.Zero);

        return true;
    }

    private static void After_TryLoadSchedule(NPC __instance, string key, ref bool __result, ref KeyValuePair<string, Vector2> __state)
    {
        if (!string.IsNullOrEmpty(__state.Key))
        {
            __instance.DefaultMap = __state.Key;
            __instance.DefaultPosition = __state.Value;
        }
    }

    private static void After_checkSchedule(NPC __instance, int timeOfDay)
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

    private static void Before_getRouteEndBehaviorFunction(NPC __instance, string behaviorName, string endMessage, ref PathFindController.endBehavior? __result)
    {
        if (__result == null && Mod.Instance.VisitorsData.ContainsKey(__instance.Name) && __instance.Schedule != null && behaviorName == "BoardBus")
            __result = Mod.VisitorReachBusEndBehavior;
    }
}
