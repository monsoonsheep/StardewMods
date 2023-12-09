#region Usings

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using VisitorFramework.Framework;

#endregion

namespace VisitorFramework.Patching;

internal class BusStopPatches : PatchList
{
    internal static BusManager Bm = ModEntry.Instance.BusManager;

    public BusStopPatches()
    {
        Patches = new List<Patch>
        {
            new(
                typeof(BusStop),
                "UpdateWhenCurrentLocation",
                new[] { typeof(GameTime) },
                postfix: nameof(BusStopUpdateWhenCurrentLocationPostfix)),
            new(
                typeof(GameLocation),
                "cleanupForVacancy",
                null,
                postfix: nameof(BusStopCleanupForVacancyPostfix)),
            new(
                typeof(BusStop),
                "resetLocalState",
                null,
                postfix: nameof(BusStopResetLocalStatePostfix))
        };
    }

    internal static void BusStopResetLocalStatePostfix(BusStop __instance)
    {
        if (Bm.BusGone)
        {
            Bm.BusPosition = new Vector2(-1000f, Bm.BusPosition.Y);
            Bm.BusDoor.Position = Bm.BusPosition + new Vector2(16f, 26f) * 4f;
        }
    }

    internal static void BusStopCleanupForVacancyPostfix(GameLocation __instance)
    {
        if (__instance is BusStop && Bm.BusReturning && !__instance.farmers.Any())
        {
            Bm.StopBus();
            Bm.OpenDoor();
        }
    }

    internal static void BusStopUpdateWhenCurrentLocationPostfix(BusStop __instance, GameTime time)
    {
        if (Bm.BusLeaving)
        {
            // Accelerate toward left
            Bm.BusMotion = new Vector2(Bm.BusMotion.X - 0.075f, Bm.BusMotion.Y);

            // Stop moving the bus once it's off screen
            if (Bm.BusPosition.X < -512f)
            {
                Bm.BusGone = true;
                Bm.BusLeaving = false;
            }
        }

        if (Bm.BusReturning)
        {
            // Decelerate after getting to X=15 from the right
            if (Bm.BusPosition.X - 704f < 256f)
                Bm.BusMotion = new Vector2(Math.Min(-1f, Bm.BusMotion.X * 0.98f),
                    Bm.BusMotion.Y);

            // Teleport to position bus reached the stop
            if (Math.Abs(Bm.BusPosition.X - 704f) <= Math.Abs(Bm.BusMotion.X * 1.5f))
            {
                Bm.StopBus();
                Bm.OpenDoor();
            }
        }
    }
}