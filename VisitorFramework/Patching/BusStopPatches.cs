#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;

#endregion

namespace BusSchedules.Patching;

internal class BusStopPatches : PatchCollection
{
    private static BusManager Bm = BusSchedules.Instance.BusManager;

    internal BusStopPatches()
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
                postfix: nameof(BusStopResetLocalStatePostfix)),
            new(
                typeof(BusStop),
                "answerDialogue",
                new [] { typeof(Response) },
                transpiler: nameof(BusStopAnswerDialogueTranspiler))
        };
    }

    private static IEnumerable<CodeInstruction> BusStopAnswerDialogueTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var list = instructions.ToList();
        int idx1 = -1;
        int idx2 = -2;

        MethodInfo drawObjectDialogMethod = AccessTools.Method(typeof(Game1), nameof(Game1.drawObjectDialogue), new[] { typeof(string) });

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Is(OpCodes.Ldstr, "Pam"))
            {
                idx1 = i;
            }

            if (list[i].Calls(drawObjectDialogMethod))
            {
                idx2 = i + 2;
                break;
            }
        }

        if (idx1 != -1 && idx2 != -1)
        {
            list.RemoveRange(idx1, idx2 - idx1);
        }

        foreach (var i in list)
        {
            Log.Debug(i.ToString());
        }

        return list.AsEnumerable();
    }

    private static void BusStopResetLocalStatePostfix(BusStop __instance)
    {
        if (Bm.BusGone || Bm.BusLeaving)
        {
            Bm.BusLeaving = false;
            Bm.BusGone = true;
            Bm.BusPosition = new Vector2(-1000f, Bm.BusPosition.Y);
            Bm.BusDoor.Position = Bm.BusPosition + new Vector2(16f, 26f) * 4f;
        }
    }

    private static void BusStopCleanupForVacancyPostfix(GameLocation __instance)
    {
        if (__instance is BusStop && Bm.BusReturning && !__instance.farmers.Any())
        {
            Bm.StopBus();
        }
    }

    private static void BusStopUpdateWhenCurrentLocationPostfix(BusStop __instance, GameTime time)
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
            }
        }
    }
}