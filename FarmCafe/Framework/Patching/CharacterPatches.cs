using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using FarmCafe.Framework.Characters;
using FarmCafe.Framework.Managers;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using Object = StardewValley.Object;

namespace FarmCafe.Framework.Patching
{
    internal class CharacterPatches : PatchList
    {
        public CharacterPatches()
        {
            Patches = new List<Patch>
            {
                new (
                    typeof(PathFindController),
                    "moveCharacter",
                    null,
                    GetType(),
                    transpiler: nameof(MoveCharacterTranspiler)),
                new (
                    typeof(Character),
                    "updateEmote",
                    null,
                    GetType(),
                    transpiler: nameof(UpdateEmoteTranspiler)),
                new (
                    typeof(Game1),
                    "warpCharacter",
                    new[] { typeof(NPC), typeof(GameLocation), typeof(Vector2) },
                    GetType(),
                    postfix: nameof(WarpCharacterPostfix)),
                new (
                    typeof(PathFindController),
                    "GetFarmTileWeight",
                    null,
                    GetType(),
                    transpiler: nameof(GetFarmTileWeightTranspiler)
                ),

            };
        }

        private static IEnumerable<CodeInstruction> UpdateEmoteTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            Label fadingTrueJump = new Label();
            List<CodeInstruction> codes = instructions.ToList();
            int pointToInsert = 0;

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].LoadsConstant(1) && codes[i + 1].StoresField(AccessTools.Field(typeof(StardewValley.Character), "emoteFading")))
                {
                    fadingTrueJump = generator.DefineLabel();
                    codes[i - 1].labels.Add(fadingTrueJump);
                    pointToInsert = i - 1;
                }
            }

            var addedCode = new List<CodeInstruction>()
            {
                new (OpCodes.Ldarg_0),
                new (OpCodes.Isinst, typeof(Customer)),
                new (OpCodes.Brfalse, fadingTrueJump),

                new (OpCodes.Ldarg_0),
                CodeInstruction.LoadField(typeof(Customer), "emoteLoop"),
                new (OpCodes.Brfalse, fadingTrueJump),

                new (OpCodes.Ldarg_0),
                new (OpCodes.Ldarg_0),
                CodeInstruction.LoadField(typeof(StardewValley.Character), "currentEmote"),
                CodeInstruction.StoreField(typeof(StardewValley.Character), "currentEmoteFrame"),
                new (OpCodes.Ret),
            };

            codes.InsertRange(pointToInsert, addedCode);
            return codes.AsEnumerable();

        }

        private static IEnumerable<CodeInstruction> MoveCharacterTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var fPathfindercharacter = AccessTools.Field(typeof(PathFindController), "character");
            var mObjectIsPassableMethod = AccessTools.Method(typeof(Object), "isPassable");

            var codeList = instructions.ToList();
            int start_pos = -1;

            for (int i = 0; i < codeList.Count; i++)
            {
                if (codeList[i].Calls(mObjectIsPassableMethod))
                {
                    start_pos = i + 1;
                    break;
                }
            }

            if (codeList[start_pos].Branches(out var jumpLabel) && jumpLabel != null)
            {
                var patchCodes = new List<CodeInstruction>
                {
                    new (OpCodes.Ldarg_0), // this
			        new (OpCodes.Ldfld, fPathfindercharacter), // this.character
			        CodeInstruction.Call(typeof(Character), "GetType"), // this.character.GetType()
			        new (OpCodes.Ldstr, "Seat"), // this.character.GetType(), "seat"
			        CodeInstruction.Call("System.Type:GetField", new[] { typeof(string) }), // FieldInfo this.character.GetType().GetField("seat")
			        new (OpCodes.Brtrue, jumpLabel) // branch to the same one found earlier
		        };

                codeList.InsertRange(start_pos + 1, patchCodes);
            }
            else
                Debug.Log("Couldn't find the break after isPassable check", LogLevel.Error);

            return codeList.AsEnumerable();
        }


        private static IEnumerable<CodeInstruction> GetFarmTileWeightTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            int stage = 0;
            foreach (var code in instructions)
            {
                if (stage == 0 && code.Is(OpCodes.Isinst, typeof(StardewValley.TerrainFeatures.Flooring)))
                {
                    stage++;
                }

                if (stage == 1 && code.LoadsConstant())
                {
                    stage++;
                    code.operand = 150;
                }

                yield return code;
            }
        }

        private static void WarpCharacterPostfix(NPC character, GameLocation targetLocation, Vector2 position)
        {
            if (character is Customer customer)
            {
                Debug.Log($"Warped customer to {targetLocation.Name} - {position}");
                CustomerManager.HandleWarp(customer, targetLocation, position);
            }
        }
    }
}
