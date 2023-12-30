using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Pathfinding;
using xTile;

namespace BusSchedules.Patching;

internal class BusStopPatches : PatchCollection
{
    private static readonly BusManager Bm = BusSchedules.Instance.BusManager;

    internal BusStopPatches()
    {
        Patches = new List<Patch>
        {
            new(
                typeof(NPC),
                "TryLoadSchedule",
                new [] { typeof(string) },
                prefix: nameof(NpcTryLoadSchedulePrefix),
                postfix: nameof(NpcTryLoadSchedulePostfix)),
            new(
                typeof(BusStop),
                "doorOpenAfterReturn",
                null,
                postfix: nameof(BusStopDoorOpenAfterReturnPostfix)),
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
                transpiler: nameof(BusStopAnswerDialogueTranspiler),
                postfix: nameof(BusStopAnswerDialoguePostfix),
                prefix: nameof(BusStopAnswerDialoguePrefix)),
            new(
                typeof(BusStop),
                "draw",
                new [] { typeof(SpriteBatch) },
                postfix: nameof(BusStopDrawPostfix))
        };
    }


    private static bool NpcTryLoadSchedulePrefix(NPC __instance, string key, ref bool __result, out KeyValuePair<string, Vector2> __state)
    {
        if (BusSchedules.Instance.VisitorsData.ContainsKey(__instance.Name))
        {
            __state = new KeyValuePair<string, Vector2>(__instance.DefaultMap, __instance.DefaultPosition);
            __instance.DefaultMap = "BusStop";
            __instance.DefaultPosition = new Vector2(12, 9) * 64f;
        }

        __state = new KeyValuePair<string, Vector2>(null, Vector2.Zero);

        return true;
    }

    private static void NpcTryLoadSchedulePostfix(NPC __instance, string key, ref bool __result, ref KeyValuePair<string, Vector2> __state)
    {
        if (!string.IsNullOrEmpty(__state.Key))
        {
            __instance.DefaultMap = __state.Key;
            __instance.DefaultPosition = __state.Value;
        }
    }

    /// <summary>
    /// When the player comes back from the desert, reset the bus to default position
    /// </summary>
    private static void BusStopDoorOpenAfterReturnPostfix(BusStop __instance)
    {
        if (Bm.BusGone)
        {
            Bm.BusMotion = Vector2.Zero;
            Bm.SetBusOutOfFrame();
            Bm.ResetDoor();
        }
    }

    /// <summary>
    /// If the player tries to board the bus while it's about to arrive or just left, make the player wait
    /// </summary>
    private static bool BusStopAnswerDialoguePrefix(BusStop __instance, Response answer, ref bool __result)
    {
        // If bus is currently moving in the location or it's about to arrive in 20 minutes or it's only been 20 minutes since it left
        if (Bm.BusLeaving || Bm.BusReturning || (Bm.BusGone && (BusSchedules.Instance.TimeUntilNextArrival <= 20 || BusSchedules.Instance.TimeSinceLastArrival <= 20)))
        {
            Log.LogWithHudMessage("You must wait 10 minutes");
            __result = false;
            return false;
        }

        return true;
    }

    /// <summary>
    /// After the player confirms to use the bus, we intercept the commands. If the bus isn't present, we put the
    /// player's controller away, fade to black and return the controller after the fade
    /// </summary>
    private static void BusStopAnswerDialoguePostfix(BusStop __instance, Response answer, ref bool __result)
    {
        if (__result == true && Game1.player.controller != null)
        {
            if (Math.Abs(Bm.BusPosition.X - 704f) < 0.001)
            {
                Bm.BusMotion = Vector2.Zero;
            }
            else
            {
                if (BusSchedules.Instance.TimeUntilNextArrival <= 20 || BusSchedules.Instance.TimeSinceLastArrival <= 20) 
                    return;

                PathFindController controller = Game1.player.controller;
                Game1.player.controller = null;
                Game1.globalFadeToBlack(delegate
                {
                    Game1.player.controller = controller;
                    Game1.globalFadeToClear();
                    Bm.ResetDoor();
                    Bm.BusPosition = new Vector2(11f, 6f) * 64f;
                });
            }
        }
    }

    /// <summary>
    /// Prevent the game from stopping you from boarding the bus when Pam isn't there
    /// </summary>
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

        return list.AsEnumerable();
    }

    /// <summary>
    /// Called when player enters the bus stop location. We reset state and put bus out of frame if it shouldn't be there
    /// </summary>
    private static void BusStopResetLocalStatePostfix(BusStop __instance)
    {
        if (!Context.IsMainPlayer)
        {
            Bm.UpdateLocation(BusSchedules.ModHelper, __instance);
        }
        if (Bm.BusLeaving || Bm.BusGone)
        {
            Bm.SetBusOutOfFrame();
            Bm.ResetDoor(closed: true);
        }
    }

    /// <summary>
    /// Called when all players leave the bus stop
    /// </summary>
    private static void BusStopCleanupForVacancyPostfix(GameLocation __instance)
    {
        if (__instance is BusStop busStop)
        {
            if (Bm.BusLeaving || Bm.BusGone)
            {
                Bm.SetBusOutOfFrame();
                Bm.BusMotion = Vector2.Zero;
            }
            else if (Bm.BusReturning)
            {
                Bm.AfterDriveBack(animate: false);
            }
        }
    }

    /// <summary>
    /// When the player is in BusStop and the bus is moving, update the busMotion every tick
    /// </summary>
    private static void BusStopUpdateWhenCurrentLocationPostfix(BusStop __instance, GameTime time)
    {
        if (Bm.BusLeaving)
        {
            // Accelerate toward left
            Bm.BusMotion = new Vector2(Bm.BusMotion.X - 0.075f, Bm.BusMotion.Y);

            // Stop moving the bus once it's off screen
            if (Bm.BusPosition.X < -512f)
            {
                Bm.SetBusOutOfFrame();
                Bm.BusMotion = Vector2.Zero;
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
                Bm.AfterDriveBack(animate: true);
            }
        }
    }

    /// <summary>
    /// Prevent the game from stopping you from boarding the bus when Pam isn't there
    /// </summary>
    private static void BusStopDrawPostfix(BusStop __instance, SpriteBatch spriteBatch)
    {
        if ((Bm.BusLeaving || Bm.BusReturning) && __instance.characters.Any(x => x.Name == "Pam"))
            spriteBatch.Draw(Game1.mouseCursors, 
                Game1.GlobalToLocal(Game1.viewport, new Vector2((int)Bm.BusPosition.X, (int)Bm.BusPosition.Y) + new Vector2(0f, 29f) * 4f), 
                new Rectangle(384, 1311, 15, 19), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (Bm.BusPosition.Y + 192f + 4f) / 10000f);
    }
}