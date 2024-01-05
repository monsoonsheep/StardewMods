using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Objects;

namespace StuffGathersDust.Patching;

internal class DrawPatches : PatchCollection
{
    public DrawPatches()
    {
        Patches = new List<Patch>
        {
            new (
                typeof(Furniture),
                "draw",
                new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) },
                postfix: nameof(FurnitureDrawPostfix)),
        };
    }

    private static void FurnitureDrawPostfix(Furniture __instance, SpriteBatch spriteBatch, int x, int y, float alpha)
    {
        if (!Furniture.isDrawingLocationFurniture || ModEntry.FurnitureDustTexture == null)
            return;

        Vector2 local = Game1.GlobalToLocal(Game1.viewport, 
            new Vector2(__instance.boundingBox.X, (__instance.boundingBox.Y - (__instance.sourceRect.Height * 4 - __instance.boundingBox.Height))) 
            + (__instance.shakeTimer > 0 ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero));

        spriteBatch.Draw(
            ModEntry.FurnitureDustTexture, 
            local, 
            __instance.sourceRect.Value, 
            Color.White, 0.0f, Vector2.Zero, 4f, __instance.flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 
            __instance.furniture_type.Value == 12 ? 
                (float) (1.9999999434361371E-09 + __instance.TileLocation.Y / 100000.0) 
                : (__instance.boundingBox.Value.Bottom - (__instance.furniture_type.Value is 6 or 17 or 13 ? 48 : 8))
                / 10000f + 0.000001f) ;
    }
}