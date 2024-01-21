using Microsoft.Xna.Framework;
using MyCafe.Locations;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;
using System.Collections.Generic;
using System.Linq;

namespace MyCafe.Patching;

internal class FurniturePatches : PatchCollection
{
    public FurniturePatches()
    {
        Patches =
        [
            new(
                typeof(Furniture),
                "clicked",
                [typeof(Farmer)],
                prefix: ClickedPrefix
            ),

            new(
                typeof(Furniture),
                "GetAdditionalFurniturePlacementStatus",
                [typeof(GameLocation), typeof(int), typeof(int), typeof(Farmer)],
                postfix: GetAdditionalFurniturePlacementStatusPostfix
            ),

            new(
                typeof(Furniture),
                "performObjectDropInAction",
                [typeof(Item), typeof(bool), typeof(Farmer)],
                prefix: PerformObjectDropInActionPrefix
            ),

            new(
                typeof(Furniture),
                "canBeRemoved",
                [typeof(Farmer)],
                postfix: CanBeRemovedPostfix
            ),

            new(
                typeof(Furniture),
                "AddSittingFarmer",
                [typeof(Farmer)],
                prefix: AddSittingFarmerPrefix
            )

        ];
    }

    private static void GetAdditionalFurniturePlacementStatusPostfix(Furniture __instance, GameLocation location, int x, int y, Farmer who, ref int __result)
    {
        if (Utility.IsTable(__instance))
        {
            Furniture table = location.GetFurnitureAt(new Vector2(x, y));
            if (Utility.IsTableTracked(table, location, out FurnitureTable trackedTable) && trackedTable.IsReserved)
            {
                // What does this mean?
                __result = 2;
            }
        }
    }
    private static bool AddSittingFarmerPrefix(Furniture __instance, Farmer who, ref Vector2? __result)
    {
        if (Utility.IsChair(__instance) && Mod.Cafe.Tables.Any(t => t.Seats.OfType<FurnitureSeat>().Any(s => s.IsReserved && s.ActualChair.Value.Equals(__instance))))
        {
            __result = null;
            return false;
        }

        return true;
    }


    private static bool PerformObjectDropInActionPrefix(Furniture __instance, Item dropInItem, bool probe, Farmer who, ref bool __result)
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

    private static void CanBeRemovedPostfix(Furniture __instance, Farmer who, ref bool __result)
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

        // For chairs, the HasSittingFarmers patch does the work
    }

    private static bool ClickedPrefix(Furniture __instance, Farmer who, ref bool __result)
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