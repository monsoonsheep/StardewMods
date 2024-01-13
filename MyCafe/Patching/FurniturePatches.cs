using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using System.Collections.Generic;
using Utility = MyCafe.Utility;
using MyCafe.ChairsAndTables;

namespace MyCafe.Patching;

internal class FurniturePatches : PatchCollection
{
    public FurniturePatches()
    {
        Patches = new List<Patch>
        {
            new(
                typeof(Furniture),
                "clicked",
                new[] { typeof(Farmer) },
                prefix: ClickedPrefix
            ),
            new(
                typeof(Furniture),
                "GetAdditionalFurniturePlacementStatus",
                new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(Farmer) },
                postfix: GetAdditionalFurniturePlacementStatusPostfix
            ),
            new(
                typeof(Furniture),
                "performObjectDropInAction",
                new[] { typeof(Item), typeof(bool), typeof(Farmer) },
                prefix: PerformObjectDropInActionPrefix
            ),
            new(
                typeof(Furniture),
                "canBeRemoved",
                new[] { typeof(Farmer) },
                postfix: CanBeRemovedPostfix
            ),
            new(
                typeof(Furniture),
                "AddSittingFarmer",
                new[] { typeof(Farmer) },
                prefix: AddSittingFarmerPrefix
            ),
        };
    }

    private static void GetAdditionalFurniturePlacementStatusPostfix(Furniture __instance, GameLocation location, int x, int y, Farmer who, ref int __result)
    {
        if (Utility.IsTable(__instance))
        {
            Furniture table = location.GetFurnitureAt(new Vector2(x, y));
            if (Utility.IsTableTracked(table, location, out FurnitureTable trackedTable) && trackedTable is { IsReserved: true })
            {
                __result = 2;
            }
        }
    }
    private static bool AddSittingFarmerPrefix(Furniture __instance, Farmer who, ref Vector2? __result)
    {
        if (Utility.IsChair(__instance) && Mod.Cafe.ChairIsReserved(__instance))
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
            if (Utility.IsTableTracked(__instance, who.currentLocation, out FurnitureTable trackedTable) && trackedTable is { IsReserved: true })
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
            if (!Context.IsMainPlayer && __instance.modData.TryGetValue(ModKeys.MODDATA_TABLERESERVED, out var val) && val == "T")
            {
                __result = false;
            }
            if (Utility.IsTableTracked(__instance, who.currentLocation, out FurnitureTable trackedTable) && trackedTable is { IsReserved: true })
            {
                Log.Debug("Can't remove");
                __result = false;
            }
        }

        // For chairs, the HasSittingFarmers patch does the work
    }

    private static bool ClickedPrefix(Furniture __instance, Farmer who, ref bool __result)
    {
        if (Utility.IsTable(__instance))
        {
            if (Utility.IsTableTracked(__instance, who.currentLocation, out FurnitureTable trackedTable) && trackedTable is { IsReserved: true })
            {
                if (!Context.IsMainPlayer)
                {
                    Sync.SendTableClick(trackedTable, who);
                }
                else
                {
                    //TableManager.Instance.FarmerClickTable(trackedTable, who);
                }
                __result = true;
                return false;
            }
        }

        return true;
    }
}