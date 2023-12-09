#region Usings
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using VisitorFramework.Framework.Visitors;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Pathfinding;
using Object = StardewValley.Object;
using VisitorFramework.Framework;
using VisitorFramework.Framework.Managers;
using Utility = VisitorFramework.Framework.Utility;

#endregion

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
                    typeof(NPC),
                    "getRouteEndBehaviorFunction",
                    new [] { typeof(string), typeof(string) },
                    postfix: nameof(NpcGetRoundEndBehaviorPostfix)),
            };
        }

        private static void NpcGetRoundEndBehaviorPostfix(NPC __instance, string behaviorName, string endMessage, ref PathFindController.endBehavior __result)
        {
            if (__result == null && VisitorManager.VisitorsData.ContainsKey(__instance.Name) && __instance.Schedule != null && behaviorName == "BoardBus")
            {
                __result = VisitorManager.CharacterReachBusEndBehavior;
            }
        }

        /// <summary>
        /// Patch <see cref="PathFindController"/>'s moveCharacter method to skip the isPassable line so our custom characters can move on the farm
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="generator"></param>
        /// <returns></returns>
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
                Log.Debug("Couldn't find the break after isPassable check", LogLevel.Error);

            return codeList.AsEnumerable();
        }
    }
}
