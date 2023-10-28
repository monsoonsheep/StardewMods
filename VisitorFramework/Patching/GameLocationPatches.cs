using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VisitorFramework.Framework.Characters;
using VisitorFramework.Framework.Managers;
using VisitorFramework.Framework.Multiplayer;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using xTile.Dimensions;
using Rectangle = xTile.Dimensions.Rectangle;
using Sickhead.Engine.Util;

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
                    null,
                    prefix: nameof(BusStopUpdateWhenCurrentLocationPostfix)),
            };
        }

        private static void BusStopUpdateWhenCurrentLocationPostfix(BusStop __instance, GameTime time)
        {
            if ((!BusManager.BusLeaving && !BusManager.BusReturning) || BusManager.busPosition == null)
                return;

            Vector2 busMotionValue = (Vector2) BusManager.busMotion.GetValue(__instance);
            Vector2 busPositionValue = (Vector2) BusManager.busPosition.GetValue(__instance);

            if (BusManager.BusLeaving && !BusManager.BusGone)
            {
                if (__instance.farmers.Count == 0)
                {
                    BusManager.busPosition.SetValue(__instance, new Vector2(-1000f, busPositionValue.Y));
                    BusManager.BusLeaving = false;
                    BusManager.BusGone = true;
                    return;
                }

                // Accelerate toward left
                BusManager.busMotion.SetValue(__instance,  new Vector2(busMotionValue.X - 0.075f, busMotionValue.Y));

                if (busPositionValue.X + 512f < 0f)
                {
                    BusManager.BusGone = true;
                    BusManager.busMotion.SetValue(__instance, Vector2.Zero);
                }
            }

            if (BusManager.BusReturning)
            {
                // Instantly teleport to position if no farmer is watching
                if (__instance.farmers.Count == 0)
                {
                    BusManager.BusFinishReturning(animate: false);
                    return;
                }

                if (Math.Abs(busPositionValue.X - 704f) <= Math.Abs(busMotionValue.X * 1.5f))
                {
                    BusManager.BusFinishReturning();
                    return;
                }

                // Decelerate after getting to X=15 from the right
                if (busPositionValue.X - 704f < 256f)
                {
                    BusManager.busMotion.SetValue(__instance, new Vector2(Math.Min(-1f, busMotionValue.X * 0.98f), busMotionValue.Y));
                }
            }
        }

        private static void CleanupBeforeSavePostfix(GameLocation __instance)
        {
            for (int i = __instance.characters.Count - 1; i >= 0; i--)
            {
                if (__instance.characters[i] is Visitor)
                {
                    Logger.Log("Removing character before saving");
                    __instance.characters.RemoveAt(i);
                }
            }
        }
    }
}
