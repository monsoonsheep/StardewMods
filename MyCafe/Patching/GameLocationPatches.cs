using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.Xna.Framework;
using MyCafe.Framework;
using MyCafe.Framework.Managers;
using MyCafe.Framework.Objects;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using xTile.Dimensions;
using Rectangle = xTile.Dimensions.Rectangle;
using Sickhead.Engine.Util;

namespace MyCafe.Patching
{
    internal class GameLocationPatches : PatchCollection
    {
        public GameLocationPatches()
        {
            Patches = new List<Patch>
            {
                new (
                    typeof(GameLocation),
                    "checkAction",
                    new [] { typeof(Location), typeof(Rectangle), typeof(Farmer) },
                    postfix: CheckActionPostfix),
            };
        }

        private static void CheckActionPostfix(GameLocation __instance, Location tileLocation, Rectangle viewport, Farmer who, ref bool __result)
        {
            if (!Context.IsMainPlayer || !CafeManager.Instance.CafeIndoors.Equals(__instance)) 
                return;

            foreach (MapTable table in TableManager.Instance.CurrentTables.OfType<MapTable>())
            {
                if (table.BoundingBox.Contains(tileLocation.X * 64, tileLocation.Y * 64))
                {
                    if (!Context.IsMainPlayer)
                    {
                        Sync.SendTableClick(table, who);
                    }
                    else
                    {
                        TableManager.Instance.FarmerClickTable(table, who);
                    }

                    __result = true;
                    return;
                }
            }
        }
    }
}
