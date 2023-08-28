using StardewValley.Tools;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using xTile.Dimensions;
using static FarmCafe.Framework.Utilities.Utility;

namespace FarmCafe.Framework.Patching
{
    internal class UtilityPatches : PatchList
    {
        public UtilityPatches()
        {
            Patches = new List<Patch>
            {
                //new (
                //    typeof(Game1),
                //    "drawMouseCursor",
                //    null,
                //    transpiler: nameof(DrawMouseCursorTranspiler)),
                new (
                    typeof(Tool),
                    "DoFunction",
                    new[] {typeof(GameLocation), typeof(int), typeof(int), typeof(int), typeof(Farmer) },
                    postfix: nameof(ToolDoFunctionPostfix)),
            };
        }

        private static IEnumerable<CodeInstruction> DrawMouseCursorTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = instructions.ToList();
            bool done = false;
            Label? jump = null;
            int pos = -1;
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(typeof(Game1).GetMethod("get_options")) && codes[i+1].Is(OpCodes.Ldfld, typeof(Options).GetField("gamepadControls")))
                {
                    i += 2;

                    if (codes[i].Branches(out jump))
                    {
                        done = true;
                        pos = i + 1;
                        
                        
                    }
                }
            }
            
            List<CodeInstruction> adds = new List<CodeInstruction>()
            {
                CodeInstruction.Call(typeof(Game1), "get_player"),
                new (OpCodes.Callvirt, typeof(Farmer).GetMethod("get_ActiveObject")),
                new (OpCodes.Isinst, typeof(Hoe)),
                new (OpCodes.Brtrue, jump),
                CodeInstruction.Call(typeof(Game1), "get_player"),
                new (OpCodes.Callvirt, typeof(Farmer).GetMethod("get_ActiveObject")),
                new (OpCodes.Isinst, typeof(WateringCan)),
                new (OpCodes.Brtrue, jump)
            };


            if (done)
            {
                codes.InsertRange(pos, adds);
            }

            Logger.Log(string.Join('\n', codes));
            return codes.AsEnumerable();
        }
        private static void ToolDoFunctionPostfix(Tool __instance, GameLocation location, int x, int y, int power, Farmer who)
        {
            switch (__instance)
            {
                case Axe:
                    //RepathCustomer(x, y);
                    break;
                case Pickaxe:
                    //RepositionCustomer(x, y);
                    break;
                case Hoe:
                    Logger.Log($"{x}, {y}: {GetTileProperties(location.Map.GetLayer("Buildings").PickTile(new Location(x, y), Game1.viewport.Size))}");
                    break;
                case FishingRod:
                    break;
                case WateringCan:
                    break;
            }
        }
    }
}
