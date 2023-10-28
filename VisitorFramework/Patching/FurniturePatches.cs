using StardewValley.Objects;
using StardewValley;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using VisitorFramework.Framework.Managers;
using VisitorFramework.Framework.Multiplayer;
using static VisitorFramework.Framework.Utility;
using StardewModdingAPI;

namespace VisitorFramework.Patching
{
    internal class FurniturePatches : PatchList
    {
        public FurniturePatches()
        {
            Patches = new List<Patch>
            {
                new(
                    typeof(Furniture),
                    "AddSittingFarmer",
                    new[] { typeof(Farmer) },
                    prefix: nameof(AddSittingFarmerPrefix)
                ),
            };
        }

        
        private static bool AddSittingFarmerPrefix(Furniture __instance, Farmer who, ref Vector2? __result)
        {
            if (VisitorManager.CurrentVisitors.Any(v => __instance.GetSeatPositions().Any(pos => pos.X == v.Tile.X && pos.Y == v.Tile.Y)))
            {
                __result = null;
                return false;
            }

            return true;
        }

    }
}