using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonsoonSheep.Stardew.Common.Patching;
using MyCafe.Characters;
using MyCafe.Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Pathfinding;
using Object = StardewValley.Object;

namespace MyCafe.Patching;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony patching requirement")]
internal class CharacterPatcher : BasePatcher
{
    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(
            original: this.RequireMethod<NPC>(nameof(NPC.ChooseAppearance)),
            prefix: this.GetHarmonyMethod(nameof(Before_ChooseAppearance))
        );
        harmony.Patch(
            original: this.RequireMethod<Character>(nameof(Character.doEmote), [typeof(int), typeof(bool), typeof(bool)]),
            postfix: this.GetHarmonyMethod(nameof(After_doEmote))
        );
        harmony.Patch(
            original: this.RequireMethod<NPC>(nameof(NPC.update), [typeof(GameTime), typeof(GameLocation)]),
            postfix: this.GetHarmonyMethod(nameof(After_update))
        );
        harmony.Patch(
            original: this.RequireMethod<NPC>(nameof(NPC.draw), [typeof(SpriteBatch), typeof(float)]),
            postfix: this.GetHarmonyMethod(nameof(After_draw))
        );
    }

    private static bool Before_ChooseAppearance(NPC __instance, LocalizedContentManager content)
    {
        if (__instance.Name.StartsWith(ModKeys.CUSTOMER_NPC_NAME_PREFIX))
            return false;

        return true;
    }

    private static void After_doEmote(Character __instance, int whichEmote, bool playSound, bool nextEventCommand)
    {
        // Check if emoting as a customer, then msg to clients
        if (Context.IsMainPlayer)
        {
            // send emote command to clients
        }
    }

    private static void After_update(NPC __instance, GameTime time, GameLocation location)
    {
        if ((Mod.Cafe.NpcCustomers.Contains(__instance.Name) || __instance.Name.StartsWith(ModKeys.CUSTOMER_NPC_NAME_PREFIX)) && !__instance.IsInvisible)
        {
            if (__instance.controller != null
                && !__instance.currentLocation.farmers.Any()
                && __instance.currentLocation.Name.Equals("BusStop")
                && (bool?) AccessTools.Field(typeof(Character), "freezeMotion").GetValue(__instance) is false)
            {
                while (__instance.currentLocation.Name.Equals("BusStop") && __instance.controller.pathToEndPoint?.Count > 2)
                {
                    __instance.controller.pathToEndPoint.Pop();
                    __instance.controller.handleWarps(new Rectangle(__instance.controller.pathToEndPoint.Peek().X * 64, __instance.controller.pathToEndPoint.Peek().Y * 64, 64, 64));
                    __instance.Position = new Vector2(__instance.controller.pathToEndPoint.Peek().X * 64, __instance.controller.pathToEndPoint.Peek().Y * 64 + 16);
                }
            }

            __instance.speed = 4;

            if (!__instance.IsInvisible)
            {
                if (__instance.get_LerpPosition() >= 0f)
                {
                    __instance.set_LerpPosition(__instance.get_LerpPosition() + (float)time.ElapsedGameTime.TotalSeconds);

                    if (__instance.get_LerpPosition() >= __instance.get_LerpDuration())
                    {
                        __instance.set_LerpPosition(__instance.get_LerpDuration());
                    }

                    __instance.Position = new Vector2(StardewValley.Utility.Lerp(__instance.get_LerpStartPosition().X, __instance.get_LerpEndPosition().X, __instance.get_LerpPosition() / __instance.get_LerpDuration()), StardewValley.Utility.Lerp(__instance.get_LerpStartPosition().Y, __instance.get_LerpEndPosition().Y, __instance.get_LerpPosition() / __instance.get_LerpDuration()));
                    if (__instance.get_LerpPosition() >= __instance.get_LerpDuration())
                    {
                        __instance.get_AfterLerp()?.Invoke(__instance);
                        __instance.set_AfterLerp(null);
                        __instance.set_LerpPosition(-1f);
                    }
                }
            }
        }
    }

    private static void After_draw(NPC __instance, SpriteBatch b, float alpha)
    {
        if ((Mod.Cafe.NpcCustomers.Contains(__instance.Name) || __instance.Name.StartsWith(ModKeys.CUSTOMER_NPC_NAME_PREFIX)) && !__instance.IsInvisible)
        {
            float layerDepth = Math.Max(0f, __instance.StandingPixel.Y / 10000f);
            Vector2 localPosition = __instance.getLocalPosition(Game1.viewport);

            if (__instance.get_DrawName().Value == true)
            {
                b.DrawString(
                    Game1.dialogueFont,
                    __instance.displayName,
                    localPosition - new Vector2(40, 64),
                    Color.White * 0.75f,
                    0f,
                    Vector2.Zero,
                    new Vector2(0.3f, 0.3f),
                    SpriteEffects.None,
                    layerDepth + 0.001f
                );
            }

            if (__instance.get_DrawOrderItem().Value == true && __instance.get_OrderItem().Value != null)
            {
                Vector2 offset = new Vector2(0,
                    (float) Math.Round(4f * Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0)));

                localPosition.Y -= 32 + __instance.Sprite.SpriteHeight * 3;

                b.Draw(
                    Mod.Sprites,
                    localPosition + offset,
                    new Rectangle(0, 16, 16, 16),
                    Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None,
                    0.99f);

                __instance.get_OrderItem().Value.drawInMenu(b, localPosition + offset, 0.35f, 1f, 0.992f);
            }
        }
    }
}
