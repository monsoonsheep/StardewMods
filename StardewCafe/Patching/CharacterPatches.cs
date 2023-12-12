﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using VisitorFramework.Framework.Characters;
using VisitorFramework.Framework.Managers;
using VisitorFramework.Framework.Multiplayer;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Pathfinding;
using Object = StardewValley.Object;

namespace VisitorFramework.Patching
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
                    new [] { typeof(GameTime) },
                    transpiler: nameof(MoveCharacterTranspiler)),
                new (
                    typeof(Character),
                    "doEmote",
                    new[] { typeof(int) , typeof(bool), typeof(bool) },
                    postfix: nameof(DoEmotePostfix)),
                new (
                    typeof(NPC),
                    "tryToReceiveActiveObject",
                    new[] { typeof(Farmer), typeof(bool) },
                    prefix: nameof(TryToReceiveActiveObjectPrefix)),
                new (
                    typeof(Game1),
                    "warpCharacter",
                    new[] { typeof(NPC), typeof(GameLocation), typeof(Vector2) },
                    postfix: nameof(WarpCharacterPostfix)),
            };
        }

        private static bool TryToReceiveActiveObjectPrefix(NPC __instance, Farmer who, bool probe)
        {
            if (!probe && __instance is Visitor Visitor) // TODO: make work for regular NPCs
            {
                if (who.ActiveObject?.ItemId != Visitor.OrderItem.ItemId)
                    return true;

                Visitor.OrderReceive();
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
                    new (OpCodes.Isinst, typeof(Visitor)),
                    new (OpCodes.Brtrue, jumpLabel) // branch to the same one found earlier
		        };

                codeList.InsertRange(start_pos + 1, patchCodes);
            }
            else
                Logger.Log("Couldn't find the break after isPassable check", LogLevel.Error);

            return codeList.AsEnumerable();
        }

        private static void WarpCharacterPostfix(NPC character, GameLocation targetLocation, Vector2 position)
        {
            if (character is Visitor Visitor)
            {
                Logger.Log($"Warped Visitor to {targetLocation.Name} - {position}");
                CafeManager.HandleWarp(Visitor, targetLocation, position);
            }
        }

        private static void DoEmotePostfix(Character __instance, int whichEmote, bool playSound, bool nextEventCommand)
        {
            if (__instance is Visitor Visitor && Context.IsMainPlayer)
            {
                Sync.VisitorDoEmote(Visitor, whichEmote);
            }
        }
    }
}