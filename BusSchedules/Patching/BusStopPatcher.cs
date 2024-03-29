using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonsoonSheep.Stardew.Common.Patching;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Pathfinding;

namespace BusSchedules.Patching;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony patching requirement")]
internal class BusStopPatcher : BasePatcher
{
    private static BusManager Bus => Mod.Instance.BusManager;

    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(
            original: this.RequireMethod<BusStop>("doorOpenAfterReturn"),
            postfix: this.GetHarmonyMethod(nameof(After_doorOpenAfterReturn))
        );
        harmony.Patch(
            original: this.RequireMethod<BusStop>("UpdateWhenCurrentLocation", [typeof(GameTime)]),
            postfix: this.GetHarmonyMethod(nameof(After_UpdateWhenCurrentLocation))
        );
        harmony.Patch(
            original: this.RequireMethod<GameLocation>("cleanupForVacancy"),
            postfix: this.GetHarmonyMethod(nameof(After_cleanupForVacancy))
        );
        harmony.Patch(
            original: this.RequireMethod<BusStop>("resetLocalState"),
            postfix: this.GetHarmonyMethod(nameof(After_resetLocalState))
        );
        harmony.Patch(
            original: this.RequireMethod<BusStop>("answerDialogue", [typeof(Response)]),
            prefix: this.GetHarmonyMethod(nameof(Before_answerDialogue)),
            postfix: this.GetHarmonyMethod(nameof(After_answerDialogue)),
            transpiler: this.GetHarmonyMethod(nameof(Transpiler_answerDialogue))
        );
        harmony.Patch(
            original: this.RequireMethod<BusStop>("draw", [typeof(SpriteBatch)]),
            postfix: this.GetHarmonyMethod(nameof(After_draw))
        );
    }

    /// <summary>
    /// When the player comes back from the desert, reset the bus to default position
    /// </summary>
    private static void After_doorOpenAfterReturn(BusStop __instance)
    {
        if (Bus.State == BusState.Gone)
        {
            __instance.busMotion = Vector2.Zero;
            Bus.MoveOutOfMap();
            Bus.ResetDoor();
        }
    }

    /// <summary>
    /// If the player tries to board the bus while it's about to arrive or just left, make the player wait
    /// </summary>
    private static bool Before_answerDialogue(BusStop __instance, Response answer, ref bool __result)
    {
        // If bus is currently moving in the location or it's about to arrive in 20 minutes or it's only been 20 minutes since it left
        if (Bus.IsMoving || (Bus.State is BusState.Gone && (Mod.Instance.TimeUntilNextArrival <= 20 || Mod.Instance.TimeSinceLastArrival <= 20)))
        {
            Game1.chatBox.addMessage("The bus will arrive shortly.", Color.White);
            __result = false;
            return false;
        }

        return true;
    }

    /// <summary>
    /// After the player confirms to use the bus, we intercept the commands. If the bus isn't present, we put the
    /// player's controller away, fade to black and return the controller after the fade
    /// </summary>
    private static void After_answerDialogue(BusStop __instance, Response answer, ref bool __result)
    {
        if (__result == true && Game1.player.controller != null)
        {
            if (Math.Abs(__instance.busPosition.X - 704f) < 0.001)
            {
                __instance.busMotion = Vector2.Zero;
            }
            else
            {
                if (Mod.Instance.TimeUntilNextArrival <= 20 || Mod.Instance.TimeSinceLastArrival <= 20) 
                    return;

                PathFindController controller = Game1.player.controller;
                Game1.player.controller = null;
                Game1.globalFadeToBlack(delegate
                {
                    Game1.player.controller = controller;
                    Game1.globalFadeToClear();
                    Bus.ResetDoor();
                    __instance.busPosition = new Vector2(11f, 6f) * 64f;
                });
            }
        }
    }

    /// <summary>
    /// Prevent the game from stopping you from boarding the bus when Pam isn't there
    /// </summary>
    private static IEnumerable<CodeInstruction> Transpiler_answerDialogue(IEnumerable<CodeInstruction> instructions)
    {
        var list = instructions.ToList();
        int idx1 = -1;
        int idx2 = -1;

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
    private static void After_resetLocalState(BusStop __instance)
    {
        if (!Context.IsMainPlayer)
        {
            Mod.Instance.UpdateBusLocation(__instance);
        }
        if (Bus.State is BusState.DrivingOut or BusState.Gone)
        {
            Bus.MoveOutOfMap();
            Bus.ResetDoor(closed: true);
        }
    }

    /// <summary>
    /// Called when all players leave the bus stop
    /// </summary>
    private static void After_cleanupForVacancy(GameLocation __instance)
    {
        if (__instance is BusStop busStop)
        {
            if (Bus.State is BusState.DrivingOut or BusState.Gone)
            {
                Bus.MoveOutOfMap();
                busStop.busMotion = Vector2.Zero;
            }
            else if (Bus.State is BusState.DrivingIn)
            {
                Bus.AfterDriveBack(animate: false);
            }
        }
    }

    /// <summary>
    /// When the player is in BusStop and the bus is moving, update the busMotion every tick
    /// </summary>
    private static void After_UpdateWhenCurrentLocation(BusStop __instance, GameTime time)
    {
        if (Bus.State == BusState.DrivingOut)
        {
            // Accelerate toward left
            __instance.busMotion = new Vector2(__instance.busMotion.X - 0.075f, __instance.busMotion.Y);

            // Stop moving the bus once it's off screen
            if (__instance.busPosition.X < -512f)
            {
                Bus.MoveOutOfMap();
                __instance.busMotion = Vector2.Zero;
            }
        }

        if (Bus.State is BusState.DrivingIn)
        {
            // Decelerate after getting to X=15 from the right
            if (__instance.busPosition.X - 704f < 256f)
                __instance.busMotion = new Vector2(Math.Min(-1f, __instance.busMotion.X * 0.98f),
                    __instance.busMotion.Y);

            // Teleport to position bus reached the stop
            if (Math.Abs(__instance.busPosition.X - 704f) <= Math.Abs(__instance.busMotion.X * 1.5f))
            {
                Bus.AfterDriveBack(animate: true);
            }
        }
    }

    /// <summary>
    /// Prevent the game from stopping you from boarding the bus when Pam isn't there
    /// </summary>
    private static void After_draw(BusStop __instance, SpriteBatch spriteBatch)
    {
        if ((Bus.IsMoving) && __instance.characters.Any(x => x.Name == "Pam"))
            spriteBatch.Draw(Game1.mouseCursors, 
                Game1.GlobalToLocal(Game1.viewport, new Vector2((int)__instance.busPosition.X, (int)__instance.busPosition.Y) + new Vector2(0f, 29f) * 4f), 
                new Rectangle(384, 1311, 15, 19), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (__instance.busPosition.Y + 192f + 4f) / 10000f);
    }
}
