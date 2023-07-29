using StardewValley.Objects;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using FarmCafe.Framework.Managers;
using static FarmCafe.Framework.Utilities.Utility;

namespace FarmCafe.Framework.Patching
{
    internal class FurniturePatches
    {
        public List<Patch> Patches;

        public FurniturePatches()
        {
            Patches = new List<Patch>
            {
                new (
                    typeof(Furniture),
                    "performRemoveAction",
                    new[] { typeof(Vector2), typeof(GameLocation) },
                    typeof(FurniturePatches),
                    postfix: nameof(FurnitureRemovePostfix)
                ),
                new (
                    typeof(Furniture),
                    "canBeRemoved",
                    new[] { typeof(Farmer) },
                    typeof(FurniturePatches),
                    postfix: nameof(CanBeRemovedPostfix)
                ),
                new (
                    typeof(Furniture),
                    "HasSittingFarmers",
                    null,
                    typeof(FurniturePatches),
                    prefix: nameof(HasSittingFarmersPrefix)
                ),
                new (
                    typeof(Furniture),
                    "AddSittingFarmer",
                    new[] { typeof(Farmer) },
                    typeof(FurniturePatches),
                    postfix: nameof(AddSittingFarmerPostfix)
                ),
                new (
                    typeof(Furniture),
                    "placementAction",
                    new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(Farmer) },
                    typeof(FurniturePatches),
                    postfix: nameof(FurniturePlacePostfix)
                ),
            };
        }

        private static void CanBeRemovedPostfix(Furniture __instance, Farmer who, ref bool __result)
        {
            if (!__result) return;

            string val;
            if ((__instance.modData.TryGetValue("FarmCafeTableIsReserved", out val) && val == "T")
                || (__instance.modData.TryGetValue("FarmCafeChairIsReserved", out val) && val == "T"))
            {
                Debug.Log("Can't remove furniture. Is reserved!");
                __result = false;
            }
        }

        private static void FurnitureRemovePostfix(Furniture __instance, Vector2 tileLocation, GameLocation environment)
        {
            if (!CafeManager.CafeLocations.Contains(environment)) { return; }

            if (IsChair(__instance))
            {
                __instance.modData.TryGetValue("FarmCafeChairTable", out string val);
                if (val == null) return;


                int[] tablePos = val?.Split(' ').Select(int.Parse).ToArray();
                Furniture table = environment.GetFurnitureAt(new Vector2(tablePos[0], tablePos[1]));
                if (table != null)
                {
                    TableManager.TryRemoveChairFromTable(__instance, table);
                }
            }
            else if (IsTable(__instance))
            {
                if (TableManager.TrackedTables.ContainsKey(__instance))
                {
                    TableManager.TryRemoveTable(__instance);
                }
            }
        }

        private static void FurniturePlacePostfix(Furniture __instance, GameLocation location, int x, int y, Farmer who)
        {
            if (!CafeManager.CafeLocations.Contains(location)) { return; }

            Debug.Log("Furniture type = " + __instance.furniture_type.Value);
            if (IsChair(__instance))
            {
                // Get position of chair
                Vector2 tablePos = __instance.TileLocation;

                // Get position of table in front of the chair
                tablePos += DirectionIntToDirectionVector(__instance.currentRotation.Value) * new Vector2(1, -1);

                // Get table Furniture object
                Furniture table = location.GetFurnitureAt(tablePos);

                // Get Table object
                if (table == null || !IsTable(table))
                {
                    return;
                }

                if (!TableManager.TrackedTables.ContainsKey(table))
                {
                    TableManager.TryAddTable(table, location);
                }
                else
                {
                    TableManager.AddChairToTable(__instance, table);
                }
            }
            else if (IsTable(__instance))
            {
                TableManager.TryAddTable(__instance, location);
            }
        }

        // Drawing a chair's front texture requires that HasSittingFarmers returns true
        private static bool HasSittingFarmersPrefix(Furniture __instance, ref bool __result)
        {
            if (__instance.modData.ContainsKey("FarmCafeChairIsReserved") && __instance.modData["FarmCafeChairIsReserved"] == "T")
            {
                __result = true;
                return false;
            }
            return true;
        }


        private static void AddSittingFarmerPostfix(Furniture __instance, Farmer who, ref Vector2? __result)
        {
            if (__result == null) return;
            foreach (var customer in CustomerManager.CurrentCustomers)
            {
                if (customer.Seat?.TileLocation == __result)
                {
                    __result = null;
                    return;
                }
            }
        }
    }
}
