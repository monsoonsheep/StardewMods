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
    
    private static bool Before_TryLoadSchedule(NPC instance, string key, ref bool result, out KeyValuePair<string, Vector2> state)
    {
        if (Mod.Instance.VisitorsData.ContainsKey(instance.Name))
        {
            state = new KeyValuePair<string, Vector2>(instance.DefaultMap, instance.DefaultPosition);
            instance.DefaultMap = "BusStop";
            instance.DefaultPosition = new Vector2(12, 9) * 64f;
        }

        state = new KeyValuePair<string, Vector2>("", Vector2.Zero);

        return true;
    }

    private static void After_TryLoadSchedule(NPC instance, string key, ref bool result, ref KeyValuePair<string, Vector2> state)
    {
        if (!string.IsNullOrEmpty(state.Key))
        {
            instance.DefaultMap = state.Key;
            instance.DefaultPosition = state.Value;
        }
    }

    private static void After_checkSchedule(NPC instance, int timeOfDay)
    {
        if (instance.Name.Equals("Pam"))
        {
            if (instance.currentLocation.Equals(Bm.BusLocation) && instance.controller != null &&
                instance.controller.pathToEndPoint.TryPeek(out Point result) && result is { X: 12, Y: 9 } &&
                timeOfDay == instance.DirectionsToNewLocation.time)
            {
                instance.Position = result.ToVector2() * 64f;
            }
        }
    }

    private static void Before_getRouteEndBehaviorFunction(NPC instance, string behaviorName, string endMessage, ref PathFindController.endBehavior? result)
    {
        if (result == null && Mod.Instance.VisitorsData.ContainsKey(instance.Name) && instance.Schedule != null && behaviorName == "BoardBus")
            result = Mod.VisitorReachBusEndBehavior;
    }
}
