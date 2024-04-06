using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonsoonSheep.Stardew.Common;
using MonsoonSheep.Stardew.Common.Patching;
using MyCafe.Characters;
using MyCafe.Data.Customers;
using MyCafe.Locations.Objects;
using MyCafe.Netcode;
using Netcode;
using StardewModdingAPI;
using StardewValley;

namespace MyCafe.Patching;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony patching requirement")]
internal class CharacterPatcher : BasePatcher
{
    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(
            original: this.RequireMethod<NPC>(nameof(NPC.draw), [typeof(SpriteBatch), typeof(float)]),
            transpiler: this.GetHarmonyMethod(nameof(CharacterPatcher.Transpile_NpcDraw))
        );
        harmony.Patch(
            original: this.RequireMethod<NPC>(nameof(NPC.ChooseAppearance)),
            prefix: this.GetHarmonyMethod(nameof(CharacterPatcher.Before_ChooseAppearance))
        );
        harmony.Patch(
            original: this.RequireMethod<NPC>(nameof(NPC.update), [typeof(GameTime), typeof(GameLocation)]),
            postfix: this.GetHarmonyMethod(nameof(CharacterPatcher.After_update))
        );
        harmony.Patch(
            original: this.RequireMethod<NPC>(nameof(NPC.draw), [typeof(SpriteBatch), typeof(float)]),
            postfix: this.GetHarmonyMethod(nameof(CharacterPatcher.After_draw))
        );
    }

    /// <summary>
    /// Adjust the draw layer of NPC when drawing if they are a customer sitting down
    /// </summary>
    private static IEnumerable<CodeInstruction> Transpile_NpcDraw(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        CodeMatcher matcher = new CodeMatcher(instructions, generator);
        matcher.MatchEndForward(new CodeMatch(OpCodes.Ldloc_0));
        matcher.MatchEndForward(new CodeMatch(OpCodes.Conv_R4));
        matcher.Advance(1);

        Label l1 = generator.DefineLabel();

        matcher.AddLabels([l1]);

        matcher.Insert([
            new CodeInstruction(OpCodes.Ldarg_0),
            CodeInstruction.Call(typeof(NpcVirtualProperties), nameof(NpcVirtualProperties.get_IsSittingDown), [typeof(NPC)]),
            CodeInstruction.Call(typeof(NetBool), "get_Value", []),
            new CodeInstruction(OpCodes.Brfalse_S, l1),
            new CodeInstruction(OpCodes.Ldc_R4, 10f),
            new CodeInstruction(OpCodes.Add)
        ]);

        return matcher.InstructionEnumeration();
    }

    /// <summary>
    /// The game tries to load appearance for random NPC customers and fails, we block the method if the NPC's name starts with the prefix
    /// </summary>
    private static bool Before_ChooseAppearance(NPC __instance, LocalizedContentManager content)
    {
        return !__instance.Name.StartsWith(ModKeys.CUSTOMER_NPC_NAME_PREFIX);
    }

    private static void After_update(NPC __instance, GameTime time, GameLocation location)
    {
        // Run only if either villager customer (in the hashset) or random customer (name starts with a prefix)
        if ((Mod.Cafe.NpcCustomers.Contains(__instance.Name) || __instance.Name.StartsWith(ModKeys.CUSTOMER_NPC_NAME_PREFIX)))
        {
            // If they are in Bus Stop or town and there's no farmers there, warp them to the next location in their route
            if (__instance.controller != null
                && !__instance.currentLocation.farmers.Any()
                && (__instance.currentLocation.Name.Equals("BusStop") || __instance.currentLocation.Name.Equals("Town"))
                && (bool?) AccessTools.Field(typeof(Character), "freezeMotion").GetValue(__instance) is false)
            {
                while ((__instance.currentLocation.Name.Equals("BusStop") || __instance.currentLocation.Name.Equals("Town")) && __instance.controller.pathToEndPoint?.Count > 2)
                {
                    __instance.controller.pathToEndPoint.Pop();
                    __instance.controller.handleWarps(new Rectangle(__instance.controller.pathToEndPoint.Peek().X * 64, __instance.controller.pathToEndPoint.Peek().Y * 64, 64, 64));
                    __instance.Position = new Vector2(__instance.controller.pathToEndPoint.Peek().X * 64, __instance.controller.pathToEndPoint.Peek().Y * 64 + 16);
                }
            }

            __instance.speed = 4;

            // Lerping the position of the character (for sitting and getting up)
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
        // Run only if either villager customer (in the hashset) or random customer (name starts with a prefix)
        if ((Mod.Cafe.NpcCustomers.Contains(__instance.Name) || __instance.Name.StartsWith(ModKeys.CUSTOMER_NPC_NAME_PREFIX)))
        {
            float layerDepth = Math.Max(0f, __instance.StandingPixel.Y / 10000f);
            Vector2 drawPosition = __instance.getLocalPosition(Game1.viewport);

            if (__instance.get_DrawName().Value == true)
            {
                b.DrawString(
                    Game1.dialogueFont,
                    __instance.displayName,
                    drawPosition - new Vector2(40, 64),
                    Color.White * 0.75f,
                    0f,
                    Vector2.Zero,
                    new Vector2(0.3f, 0.3f),
                    SpriteEffects.None,
                    layerDepth + 0.001f
                );
            }

            // TODO move to the OnRenderedWorld event
            Item item;
            if (__instance.get_DrawOrderItem().Value == true && (item = __instance.get_OrderItem().Value) != null)
            {
                Vector2 offset = new Vector2(0,
                    (float) Math.Round(4f * Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0)));

                drawPosition.Y -= 32 + __instance.Sprite.SpriteHeight * 3;

                // Draw bubble
                b.Draw(
                    Mod.Sprites,
                    drawPosition + offset,
                    new Rectangle(0, 16, 16, 16),
                    Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None,
                    0.99f);

                // Item inside the bubble
                item.drawInMenu(b, drawPosition + offset, 0.40f, 1f, 0.992f, StackDrawType.Hide, Color.White, drawShadow: false);

                // Draw item name if hovering over bubble
                Vector2 mouse = new Vector2(Game1.getMouseX(), Game1.getMouseY());
                if (Vector2.Distance(drawPosition, mouse) <= Game1.tileSize)
                {
                    Vector2 size = Game1.dialogueFont.MeasureString(item.DisplayName) * 0.75f;
                    b.DrawString(Game1.dialogueFont, item.DisplayName, drawPosition + new Vector2(32 - size.X / 2f, -10f), Color.Black, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 1f);
                }
            }
        }
    }
}
