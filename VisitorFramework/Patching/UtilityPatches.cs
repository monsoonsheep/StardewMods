using StardewValley.Tools;
using StardewValley;
using System.Collections.Generic;
using xTile.Dimensions;
using static VisitorFramework.Framework.Utility;

namespace VisitorFramework.Patching
{
    internal class UtilityPatches : PatchList
    {
        public UtilityPatches()
        {
            Patches = new List<Patch>
            {
                new (
                    typeof(Tool),
                    "DoFunction",
                    new[] {typeof(GameLocation), typeof(int), typeof(int), typeof(int), typeof(Farmer) },
                    postfix: nameof(ToolDoFunctionPostfix)),
            };
        }

        private static void ToolDoFunctionPostfix(Tool __instance, GameLocation location, int x, int y, int power, Farmer who)
        {
            switch (__instance)
            {
                case Axe:
                    //RepathVisitor(x, y);
                    break;
                case Pickaxe:
                    //RepositionVisitor(x, y);
                    break;
                case Hoe:
                    Logger.Log($"Buildings {x}, {y}: {GetTileProperties(location.Map.GetLayer("Buildings").PickTile(new Location(x, y), Game1.viewport.Size))}");
                    Logger.Log($"Back {x}, {y}: {GetTileProperties(location.Map.GetLayer("Back").PickTile(new Location(x, y), Game1.viewport.Size))}");
                    break;
                case FishingRod:
                    break;
                case WateringCan:
                    break;
            }
        }
    }
}
