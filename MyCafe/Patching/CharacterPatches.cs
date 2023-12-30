using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MyCafe.Framework.Customers;
using MyCafe;
using MyCafe.Framework;
using MyCafe.Patching;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Pathfinding;
using Object = StardewValley.Object;
using Patch = HarmonyLib.Patch;

namespace MyCafe.Patching
{
    internal class CharacterPatches : PatchCollection
    {
        public CharacterPatches()
        {
            Patches = new List<Patch>
            {
                new (
                    typeof(PathFindController),
                    "moveCharacter",
                    new [] { typeof(GameTime) },
                    transpiler: MoveCharacterTranspiler),
                new (
                    typeof(Character),
                    "doEmote",
                    new[] { typeof(int) , typeof(bool), typeof(bool) },
                    postfix: DoEmotePostfix),
                new (
                    typeof(NPC),
                    "tryToReceiveActiveObject",
                    new[] { typeof(Farmer), typeof(bool) },
                    prefix: TryToReceiveActiveObjectPrefix),
                new (
                    typeof(Game1),
                    "warpCharacter",
                    new[] { typeof(NPC), typeof(GameLocation), typeof(Vector2) },
                    postfix: WarpCharacterPostfix),
            };
        }

        private static bool TryToReceiveActiveObjectPrefix(NPC __instance, Farmer who, bool probe)
        {
            if (!probe && __instance is Customer c) // TODO: make work for regular NPCs
            {
                if (who.ActiveObject?.ItemId != c.GetOrderItem())
                    return true;

                //c.OrderReceive();
                who.reduceActiveItemByOne();
                return false;
            }

            return true;
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
                    new (OpCodes.Isinst, typeof(Customer)),
                    new (OpCodes.Brtrue, jumpLabel) // branch to the same one found earlier
		        };

                codeList.InsertRange(start_pos + 1, patchCodes);
            }
            else
                Log.Debug("Couldn't find the break after isPassable check", LogLevel.Error);

            return codeList.AsEnumerable();
        }

        private static void WarpCharacterPostfix(NPC character, GameLocation targetLocation, Vector2 position)
        {
            if (character is Customer c)
            {
                Log.Debug($"Warped Visitor to {targetLocation.Name} - {position}");
                //CafeManager.HandleWarp(c, targetLocation, position);
            }
        }

        private static void DoEmotePostfix(Character __instance, int whichEmote, bool playSound, bool nextEventCommand)
        {
            if (__instance is Customer c && Context.IsMainPlayer)
            {
                //Sync.VisitorDoEmote(c, whichEmote);
            }
        }
    }
}
