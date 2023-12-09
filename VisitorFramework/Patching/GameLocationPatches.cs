#region Usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VisitorFramework.Framework.Visitors;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using xTile.Dimensions;
using Rectangle = xTile.Dimensions.Rectangle;
using Sickhead.Engine.Util;
using VisitorFramework.Framework.Managers;
#endregion

namespace VisitorFramework.Patching
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
                    typeof(BusStop),
                    "UpdateWhenCurrentLocation",
                    new [] { typeof(GameTime) },
                    postfix: nameof(BusStopUpdateWhenCurrentLocationPostfix)),
                new (
                    typeof(GameLocation),
                    "cleanupForVacancy",
                    null,
                    postfix: nameof(BusStopCleanupForVacancyPostfix)),
                new (
                    typeof(BusStop),
                    "resetLocalState",
                    null,
                    postfix: nameof(BusStopResetLocalStatePostfix)),
            };
        }

        private static void BusStopResetLocalStatePostfix(BusStop __instance)
        {
            if (BusManager.BusGone)
            {
                BusManager.BusPosition = new Vector2(-1000f, BusManager.BusPosition.Y);
                BusManager.BusDoor.Position = BusManager.BusPosition + new Vector2(16f, 26f) * 4f;
            }
        }

        private static void BusStopCleanupForVacancyPostfix(GameLocation __instance)
        {
            if (__instance is BusStop && BusManager.BusReturning && !__instance.farmers.Any())
            {
                BusManager.StopBus();
                BusManager.OpenDoor();
            }
        }

        private static void BusStopUpdateWhenCurrentLocationPostfix(BusStop __instance, GameTime time)
        {
            if (BusManager.BusLeaving)
            {
                // Accelerate toward left
                BusManager.BusMotion = new Vector2(BusManager.BusMotion.X - 0.075f, BusManager.BusMotion.Y);

                // Stop moving the bus once it's off screen
                if (BusManager.BusPosition.X < -512f)
                {
                    BusManager.BusGone = true;
                    BusManager.BusLeaving = false;
                }
            }

            if (BusManager.BusReturning)
            {
                // Decelerate after getting to X=15 from the right
                if (BusManager.BusPosition.X - 704f < 256f)
                {
                    BusManager.BusMotion = new Vector2(Math.Min(-1f, BusManager.BusMotion.X * 0.98f), BusManager.BusMotion.Y);
                }

                // Teleport to position bus reached the stop
                if (Math.Abs(BusManager.BusPosition.X - 704f) <= Math.Abs(BusManager.BusMotion.X * 1.5f))
                {
                    BusManager.StopBus();
                    BusManager.OpenDoor();
                }
            }
        }

        /// <summary>
        /// For removing <see cref="Visitor"/> instances from <see cref="GameLocation"/>s before game saving so the game doesn't try to serialize them
        /// </summary>
        private static void CleanupBeforeSavePostfix(GameLocation __instance)
        {
            for (int i = __instance.characters.Count - 1; i >= 0; i--)
            {
                if (__instance.characters[i] is Visitor)
                {
                    Log.Debug("Removing character before saving");
                    __instance.characters.RemoveAt(i);
                }
            }
        }
    }
}
