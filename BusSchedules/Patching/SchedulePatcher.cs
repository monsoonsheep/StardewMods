#region Usings

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MonsoonSheep.Stardew.Common.Patching;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Internal;
using StardewValley.Locations;
using StardewValley.Pathfinding;

#endregion

namespace BusSchedules.Patching;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony patching requirement")]
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
            postfix: this.GetHarmonyMethod(nameof(After_getRouteEndBehaviorFunction))
        );
    }

    /// <summary>
    /// Before TryLoadSchedule is called, change the NPC's DefaultMap and DefaultPosition to the bus location and position, storing
    /// the original in a state variable. Only if the NPC is a bus visitor
    /// </summary>
    private static bool Before_TryLoadSchedule(NPC __instance, string key, ref bool __result, out (string, Vector2)? __state)
    {
        if (Mod.Instance.VisitorsData.ContainsKey(__instance.Name))
        {
            __state = new(__instance.DefaultMap, __instance.DefaultPosition);

            __instance.DefaultMap = "BusStop";
            __instance.DefaultPosition = Mod.BusDoorTile.ToVector2() * 64f;
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
            (__instance.DefaultMap, __instance.DefaultPosition) = __state.Value;
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

    private static void After_getRouteEndBehaviorFunction(NPC __instance, string behaviorName, string endMessage, ref PathFindController.endBehavior? __result)
    {
        if (__result == null && Mod.Instance.VisitorsData.ContainsKey(__instance.Name) && __instance.Schedule != null && behaviorName == "BoardBus")
        {
            __result = Mod.VisitorReachBusEndBehavior;
        }
    }

}
