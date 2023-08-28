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
using FarmCafe.Framework.Objects;
using FarmCafe.Framework.Locations;
using FarmCafe.Framework.Multiplayer;
using HarmonyLib;
using Netcode;
using StardewValley.Network;
using static FarmCafe.Framework.Utilities.Utility;
using StardewModdingAPI;
using xTile;
using OpCodes = System.Reflection.Emit.OpCodes;
using xTile.Dimensions;

namespace FarmCafe.Framework.Patching
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
                    "canBePlacedHere",
                    new[] { typeof(GameLocation), typeof(Vector2) },
                    transpiler: nameof(CanBePlacedHereTranspiler)
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
            if (IsChair(__instance) && TableManager.ChairIsReserved(__instance))
            {
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

        private static IEnumerable<CodeInstruction> CanBePlacedHereTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            Label jumpLabel = generator.DefineLabel();
            Label leaveLabel;

            List<CodeInstruction> codelist = instructions.ToList();
            int insertPoint = -1;

            for (int i = 0; i < codelist.Count; i++)
            {
                if (!codelist[i].Calls(AccessTools.Method(typeof(Furniture), "getTilesHigh")))
                    continue;

                insertPoint = i + 3;
            }

            if (insertPoint == -1)
                return instructions;

            // This is ldc.i4.1 (returning true)
            codelist[insertPoint].labels.Add(jumpLabel);
            leaveLabel = (Label) codelist[insertPoint + 2].operand;

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
                CodeInstruction.Call(typeof(String), "Equals", new[] { typeof(String) }),
                new CodeInstruction(OpCodes.Brfalse_S, jumpLabel),

                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Stloc, 8),
                new CodeInstruction(OpCodes.Leave, leaveLabel)
            };
            codelist.InsertRange(insertPoint, addCodes);
            Logger.Log(string.Join('\n', codelist));
            return codelist.AsEnumerable();
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