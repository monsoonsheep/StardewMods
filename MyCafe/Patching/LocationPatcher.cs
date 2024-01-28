using StardewModdingAPI;
using StardewValley;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsoonSheep.Stardew.Common.Patching;
using StardewValley.Network;
using xTile.Dimensions;
using MyCafe.Locations.Objects;

namespace MyCafe.Patching;

internal class LocationPatcher : BasePatcher
{
    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(
            original: this.RequireMethod<GameLocation>("checkAction"),
            postfix: this.GetHarmonyMethod(nameof(LocationPatcher.After_CheckAction))
            );
        harmony.Patch(
            original: this.RequireMethod<Farm>("initNetFields"),
            postfix: this.GetHarmonyMethod(nameof(LocationPatcher.After_InitNetFields))
            );
    }

    private static void After_InitNetFields(Farm __instance)
    {
        __instance.NetFields.AddField(__instance.get_Cafe(), $"{Mod.ModManifest.UniqueID}.Cafe");
        Mod.Cafe = __instance.get_Cafe().Value;
    }

    private static void After_CheckAction(Farm __instance, Location tileLocation, Rectangle viewport, Farmer who, ref bool __result)
    {
        if (__result == true || (!__instance.Equals(Mod.Cafe.Indoor) && !__instance.Equals(Mod.Cafe.Outdoor)))
            return;

        foreach (Table table in Mod.Cafe.Tables)
        {
            if (table.BoundingBox.Value.Contains(tileLocation.X * 64, tileLocation.Y * 64) 
                && Mod.Cafe.InteractWithTable(table, who))
            {
                __result = true;
                return;
            }
        }
    }
}