using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FarmCafe.Framework.Characters;
using FarmCafe.Framework.Managers;
using FarmCafe.Framework.Objects;
using FarmCafe.Locations;
using StardewModdingAPI;
using StardewValley;
using xTile.Dimensions;

namespace FarmCafe.Patching
{
    internal class GameLocationPatches : PatchList
    {
        public GameLocationPatches()
        {
            Patches = new List<Patch>
            {
                new (
                    typeof(GameLocation),
                    "cleanupBeforeSave",
                    null,
                    postfix: nameof(CleanupBeforeSavePostfix)),
                new (
                    typeof(GameLocation),
                    "checkAction",
                    new [] { typeof(Location), typeof(Rectangle), typeof(Farmer) },
                    postfix: nameof(CheckActionPostfix)),
            };
        }

        private static void CleanupBeforeSavePostfix(GameLocation __instance)
        {
            for (int i = __instance.characters.Count - 1; i >= 0; i--)
            {
                if (__instance.characters[i] is Customer)
                {
                    Logger.Log("Removing character before saving");
                    __instance.characters.RemoveAt(i);
                }
            }
        }

        private static void CheckActionPostfix(GameLocation __instance, Location tileLocation, Rectangle viewport, Farmer who, ref bool __result)
        {
            if (!ModEntry.CafeLocations.Contains(__instance)) return;

            foreach (MapTable table in ModEntry.Tables.OfType<MapTable>())
            {
                if (table.BoundingBox.Contains(tileLocation.X * 64, tileLocation.Y * 64))
                {
                    if (!Context.IsMainPlayer)
                    {
                        Multiplayer.Sync.SendTableClick(table, who);
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
