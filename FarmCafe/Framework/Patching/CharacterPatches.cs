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
                //new (
                //    typeof(PathFindController),
                //    "moveCharacter",
                //    null,
                //    transpiler: nameof(MoveCharacterTranspiler)),
                new (
                    typeof(Character),
                    "doEmote",
                    new[] { typeof(int) , typeof(bool), typeof(bool) },
                    postfix: nameof(DoEmotePostfix)),
                new (
                    typeof(Game1),
                    "warpCharacter",
                    new[] { typeof(NPC), typeof(GameLocation), typeof(Vector2) },
                    postfix: nameof(WarpCharacterPostfix)),
            };
        }

        // Is this really needed?
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

        private static void WarpCharacterPostfix(NPC character, GameLocation targetLocation, Vector2 position)
        {
            if (character is Customer customer)
            {
                Debug.Log($"Warped customer to {targetLocation.Name} - {position}");
                FarmCafe.CafeManager.HandleWarp(customer, targetLocation, position);
            }
        }

        private static void DoEmotePostfix(Character __instance, int whichEmote, bool playSound, bool nextEventCommand)
        {
            if (__instance is Customer customer && Context.IsMainPlayer)
            {
                Multiplayer.CustomerDoEmote(customer, whichEmote);
            }
        }
    }
}
