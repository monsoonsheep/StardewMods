using StardewValley.Tools;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                    //RepathCustomer(x, y);
                    break;
                case Pickaxe:
                    //RepositionCustomer(x, y);
                    break;
                case Hoe:
                    Debug.Log($"{x}, {y}: {GetTileProperties(location.Map.GetLayer("Back").PickTile(new Location(x, y), Game1.viewport.Size))}");
                    break;
                case FishingRod:
                    break;
                case WateringCan:
                    break;
            }
        }
    }
}
