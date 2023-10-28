using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using xTile.Dimensions;
using Rectangle = xTile.Dimensions.Rectangle;
using Sickhead.Engine.Util;
using StardewCafe.Framework;
using StardewCafe.Framework.Objects;

namespace StardewCafe.Patching
{
    internal class GameLocationPatches : PatchList
    {
        public GameLocationPatches()
        {
            Patches = new List<Patch>
            {
                new (
                    typeof(GameLocation),
                    "checkAction",
                    new [] { typeof(Location), typeof(Rectangle), typeof(Farmer) },
                    postfix: nameof(CheckActionPostfix)),
            };
        }

        private static void CheckActionPostfix(GameLocation __instance, Location tileLocation, Rectangle viewport, Farmer who, ref bool __result)
        {
            if (!CafeManager.CafeLocations.Contains(__instance)) return;

            foreach (MapTable table in CafeManager.Tables.OfType<MapTable>())
            {
                if (table.BoundingBox.Contains(tileLocation.X * 64, tileLocation.Y * 64))
                {
                    if (!Context.IsMainPlayer)
                    {
                        Sync.SendTableClick(table, who);
                    }
                    else
                    {
                        CafeManager.FarmerClickTable(table, who);
                    }

                    __result = true;
                    return;
                }
            }
        }
    }
}
