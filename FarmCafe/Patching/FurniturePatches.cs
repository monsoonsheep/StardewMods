﻿using StardewValley.Objects;
using StardewValley;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using FarmCafe.Framework.Managers;
using FarmCafe.Framework.Objects;
using FarmCafe.Framework.Multiplayer;
using static FarmCafe.Framework.Utility;
using StardewModdingAPI;

namespace FarmCafe.Patching
{
    internal class FurniturePatches : PatchList
    {
        public FurniturePatches()
        {
            Patches = new List<Patch>
            {
                new(
                    typeof(Furniture),
                    "clicked",
                    new[] { typeof(Farmer) },
                    prefix: nameof(ClickedPrefix)
                ),
                new(
                    typeof(Furniture),
                    "GetAdditionalFurniturePlacementStatus",
                    new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(Farmer) },
                    postfix: nameof(GetAdditionalFurniturePlacementStatusPostfix)
                ),
                new(
                    typeof(Furniture),
                    "performObjectDropInAction",
                    new[] { typeof(Item), typeof(bool), typeof(Farmer) },
                    prefix: nameof(PerformObjectDropInActionPrefix)
                ),
                new(
                    typeof(Furniture),
                    "canBeRemoved",
                    new[] { typeof(Farmer) },
                    postfix: nameof(CanBeRemovedPostfix)
                ),
                new(
                    typeof(Furniture),
                    "HasSittingFarmers",
                    null,
                    prefix: nameof(HasSittingFarmersPrefix)
                ),
                new(
                    typeof(Furniture),
                    "AddSittingFarmer",
                    new[] { typeof(Farmer) },
                    prefix: nameof(AddSittingFarmerPrefix)
                ),
            };
        }

        // Drawing a chair's front texture requires that HasSittingFarmers returns true
        private static bool HasSittingFarmersPrefix(Furniture __instance, ref bool __result)
        {
            if (IsChair(__instance) && CafeManager.ChairIsReserved(__instance))
            {
                __result = true;
                return false;
            }

            return true;
        }

        private static void GetAdditionalFurniturePlacementStatusPostfix(Furniture __instance, GameLocation location, int x, int y, Farmer who, ref int __result)
        {
            if (IsTable(__instance))
            {
                Furniture table = location.GetFurnitureAt(new Vector2(x, y));
                FurnitureTable trackedTable = IsTableTracked(table, location);
                if (trackedTable is { IsReserved: true })
                {
                    __result = 2;
                }
            }
        }
        private static bool AddSittingFarmerPrefix(Furniture __instance, Farmer who, ref Vector2? __result)
        {
            if (IsChair(__instance) && CafeManager.ChairIsReserved(__instance))
            {
                __result = null;
                return false;
            }

            return true;
        }


        private static bool PerformObjectDropInActionPrefix(Furniture __instance, Item dropInItem, bool probe, Farmer who, ref bool __result)
        {
            if (IsTable(__instance))
            {
                FurnitureTable trackedTable = IsTableTracked(__instance, who.currentLocation);
                if (trackedTable is { IsReserved: true })
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

            if (IsTable(__instance))
            {
                if (!Context.IsMainPlayer && __instance.modData.TryGetValue("FarmCafeTableIsReserved", out var val) && val == "T")
                {
                    __result = false;
                }
                FurnitureTable trackedTable = IsTableTracked(__instance, who.currentLocation);
                if (trackedTable is { IsReserved: true })
                {
                    Logger.Log("Can't remove");
                    __result = false;
                }
            }

            // For chairs, the HasSittingFarmers patch does the work
        }

        private static bool ClickedPrefix(Furniture __instance, Farmer who, ref bool __result)
        {
            if (IsTable(__instance))
            {
                FurnitureTable trackedTable = IsTableTracked(__instance, who.currentLocation);
                if (trackedTable is { IsReserved: true })
                {
                    if (!Context.IsMainPlayer)
                    {
                        Sync.SendTableClick(trackedTable, who);
                    }
                    else
                    {
                        CafeManager.FarmerClickTable(trackedTable, who);
                    }
                    __result = true;
                    return false;
                }
            }

            return true;
        }
    }
}