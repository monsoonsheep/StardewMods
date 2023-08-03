using StardewValley.Objects;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using FarmCafe.Framework.Characters;
using Microsoft.Xna.Framework;
using FarmCafe.Framework.Managers;
using HarmonyLib;
using Netcode;
using StardewValley.Network;
using static FarmCafe.Framework.Utilities.Utility;
using Mono.Cecil.Cil;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace FarmCafe.Framework.Patching
{
    internal class FurniturePatches : PatchList
    {
        public FurniturePatches()
        {
            Patches = new List<Patch>
            {
                new (
                    typeof(Furniture),
                    "clicked",
                    new[] { typeof(Farmer) },
                    typeof(FurniturePatches),
                    prefix: nameof(ClickedPrefix)
                ),
                new (
                    typeof(Furniture),
                    "canBePlacedHere",
                    new[] { typeof(GameLocation), typeof(Vector2) },
                    typeof(FurniturePatches),
                    transpiler: nameof(CanBePlacedHereTranspiler)
                ),
                new (
                    typeof(Furniture),
                    "performObjectDropInAction",
                    new[] { typeof(Item), typeof(bool), typeof(Farmer) },
                    typeof(FurniturePatches),
                    prefix: nameof(PerformObjectDropInActionPrefix)
                ),
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
                    prefix: nameof(AddSittingFarmerPrefix)
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

        private static bool ClickedPrefix(Furniture __instance, Farmer who, ref bool __result)
        {
            if (IsTable(__instance) && TableManager.TrackedTables.Keys.Contains(__instance) &&
                TableManager.TableIsReserved(__instance))
            {
                __result = true;
                Debug.Show($"Item: {who.ActiveObject?.DisplayName}");

                CustomerGroup groupOnTable =
                    CustomerManager.CurrentGroups.FirstOrDefault(g => g.ReservedTable == __instance);
                if (groupOnTable == null)
                    return false;

                foreach (Customer member in groupOnTable.Members)
                {
                    member.OrderReceive();
                }
                return false;
            }

            return true;
        }

        private static IEnumerable<CodeInstruction> CanBePlacedHereTranspiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            Label jumpLabel = generator.DefineLabel();
            Label leaveLabel = new();

            List <CodeInstruction> codelist = instructions.ToList();
            int insertPoint = -1;

            for (int i = 0; i < codelist.Count; i++)
            {
                if (!codelist[i].Calls(AccessTools.Method(typeof(Furniture), "getTilesHigh"))) 
                    continue;

                i += 3;
                insertPoint = i;

                // This is ldc.i4.1 (returning true)
                codelist[i].labels.Add(jumpLabel);
                leaveLabel = (Label)codelist[i + 2].operand;
            }

            if (insertPoint == -1) 
                return codelist.AsEnumerable();


            List<CodeInstruction> addCodes = new()
            {
                new CodeInstruction(OpCodes.Ldloc, 6),
                CodeInstruction.LoadField(typeof(Item), "modData"),
                new CodeInstruction(OpCodes.Ldstr, "FarmCafeTableIsReserved"),
                CodeInstruction.Call(typeof(NetStringDictionary<string, NetString>), "ContainsKey"),
                new CodeInstruction(OpCodes.Brfalse_S, jumpLabel),
                new CodeInstruction(OpCodes.Ldloc, 6),
                CodeInstruction.LoadField(typeof(Item), "modData"),
                new CodeInstruction(OpCodes.Ldstr, "FarmCafeTableIsReserved"),
                CodeInstruction.Call(typeof(NetStringDictionary<string, NetString>), "get_Item"),
                new CodeInstruction(OpCodes.Ldstr, "T"),
                CodeInstruction.Call(typeof(String), "Equals", new [] {typeof(String)}),
                new CodeInstruction(OpCodes.Brfalse_S, jumpLabel),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Stloc, 8),
                new CodeInstruction(OpCodes.Leave, leaveLabel)
            };
            codelist.InsertRange(insertPoint, addCodes);
            return codelist.AsEnumerable();
        }

        private static bool PerformObjectDropInActionPrefix(Furniture __instance, Item dropInItem, bool probe,
            Farmer who, ref bool __result)
        {
            if (!IsTable(__instance) || !TableManager.TableIsReserved(__instance))
                return true;
            
            __instance.modData.ContainsKey("jo");
            Debug.Log("Can't place the item on table");
            __result = false;
            return false;
        }

        private static void CanBeRemovedPostfix(Furniture __instance, Farmer who, ref bool __result)
        {
            if (__result is false)
                return;

            if (IsTable(__instance) && TableManager.TableIsReserved(__instance))
            {
                Debug.Log("Can't remove");
                __result = false;
            }

            // For chairs, the HasSittingFarmers patch does the work
        }

        private static void FurnitureRemovePostfix(Furniture __instance, Vector2 tileLocation, GameLocation environment)
        {
            if (!CafeManager.CafeLocations.Contains(environment)) { return; }

            if (IsChair(__instance))
            {
                __instance.modData.TryGetValue("FarmCafeChairTable", out string val);
                if (val == null) 
                    return;

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

            //Debug.Log("Furniture type = " + __instance.furniture_type.Value);
            if (IsChair(__instance))
            {
                // Get position of table in front of the chair
                Vector2 tablePos = __instance.TileLocation + (DirectionIntToDirectionVector(__instance.currentRotation.Value) * new Vector2(1, -1));

                // Get table Furniture object
                Furniture table = location.GetFurnitureAt(tablePos);

                
                // Get Table object
                if (table == null || !IsTable(table))
                {
                    return;
                }

                if (table.getBoundingBox(table.TileLocation).Intersects(__instance.boundingBox.Value))
                    return;

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
            if (IsChair(__instance) && TableManager.ChairIsReserved(__instance))
            {
                //Debug.Log("Hass sitting");
                __result = true;
                return false;
            }
            return true;
        }


        private static bool AddSittingFarmerPrefix(Furniture __instance, Farmer who, ref Vector2? __result)
        {
            if (IsChair(__instance) && TableManager.ChairIsReserved(__instance))
            {
                __result = null;
                return false;
            }

            return true;
        }
    }
}
