using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsoonSheep.Stardew.Common.Patching;
using MyCafe.Locations.Objects;

namespace MyCafe.Patching;

internal class FurniturePatcher : BasePatcher
{
    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(
            original: this.RequireMethod<Furniture>("clicked"),
            prefix: this.GetHarmonyMethod(nameof(FurniturePatcher.Before_Clicked))
        );
        harmony.Patch(
            original: this.RequireMethod<Furniture>(nameof(Furniture.GetAdditionalFurniturePlacementStatus)),
            postfix: this.GetHarmonyMethod(nameof(FurniturePatcher.After_GetAdditionalFurniturePlacementStatus))
        );
        harmony.Patch(
            original: this.RequireMethod<Furniture>(nameof(Furniture.performObjectDropInAction)),
            prefix: this.GetHarmonyMethod(nameof(FurniturePatcher.Before_PerformObjectDropInAction))
        );
        harmony.Patch(
            original: this.RequireMethod<Furniture>(nameof(Furniture.canBeRemoved)),
            postfix: this.GetHarmonyMethod(nameof(FurniturePatcher.After_CanBeRemoved))
        );
        harmony.Patch(
            original: this.RequireMethod<Furniture>(nameof(Furniture.AddSittingFarmer)),
            prefix: this.GetHarmonyMethod(nameof(FurniturePatcher.Before_AddSittingFarmer))
        );
    }

    private static void After_GetAdditionalFurniturePlacementStatus(Furniture __instance, GameLocation location, int x, int y, Farmer who, ref int __result)
    {
        if (Utility.IsTable(__instance))
        {
            Furniture table = location.GetFurnitureAt(new Vector2(x, y));
            if (Utility.IsTableTracked(table, location, out FurnitureTable trackedTable) && trackedTable.IsReserved)
                __result = 2;
        }
    }
    private static bool Before_AddSittingFarmer(Furniture __instance, Farmer who, ref Vector2? __result)
    {
        if (Utility.IsChair(__instance) && Mod.Cafe.Tables.Any(t => t.Seats.OfType<FurnitureSeat>().Any(s => s.IsReserved && s.ActualChair.Value.Equals(__instance))))
        {
            __result = null;
            return false;
        }

        return true;
    }


    private static bool Before_PerformObjectDropInAction(Furniture __instance, Item dropInItem, bool probe, Farmer who, ref bool __result)
    {
        if (Utility.IsTable(__instance))
        {
            if (Utility.IsTableTracked(__instance, who.currentLocation, out FurnitureTable trackedTable) && trackedTable.IsReserved)
            {
                __result = false;
                return false;
            }
        }

        return true;
    }

    private static void After_CanBeRemoved(Furniture __instance, Farmer who, ref bool __result)
    {
        if (__result is false)
            return;

        if (Utility.IsTable(__instance))
        {
            if (Utility.IsTableTracked(__instance, who.currentLocation, out FurnitureTable trackedTable) && trackedTable.IsReserved)
            {
                Game1.addHUDMessage(new HUDMessage("Can't remove this furniture", 1000, fadeIn: false));
                __result = false;
            }
        }
    }

    private static bool Before_Clicked(Furniture __instance, Farmer who, ref bool __result)
    {
        if (!Utility.IsTable(__instance)) 
            return true;

        //if (Utility.IsTableTracked(__instance, who.currentLocation, out FurnitureTable trackedTable) && trackedTable.IsReserved)
        //{
        //    if (Mod.Cafe.ClickTable(trackedTable, who))
        //    {
        //        __result = true;
        //        return false;
        //    }
        //}

        return true;
    }

}