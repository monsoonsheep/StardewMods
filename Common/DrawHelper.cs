using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonsoonSheep.Stardew.Common.UI;
using StardewValley;
using StardewValley.Menus;

namespace MonsoonSheep.Stardew.Common
{
    /// <summary>Provides utility methods for drawing to the screen.</summary>
    internal static class DrawHelper
    {
        /*********
        ** Public methods
        *********/
       
        /****
        ** Drawing
        ****/
        /// <summary>Draw a sprite to the screen.</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        /// <param name="sheet">The sprite sheet containing the sprite.</param>
        /// <param name="sprite">The sprite coordinates and dimensions in the sprite sheet.</param>
        /// <param name="x">The X-position at which to draw the sprite.</param>
        /// <param name="y">The X-position at which to draw the sprite.</param>
        /// <param name="color">The color to tint the sprite.</param>
        /// <param name="scale">The scale to draw.</param>
        public static void DrawSprite(this SpriteBatch spriteBatch, Texture2D sheet, Rectangle sprite, float x, float y, Color? color = null, float scale = 1)
        {
            spriteBatch.Draw(sheet, new Vector2(x, y), sprite, color ?? Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }

        /// <summary>Draw a sprite to the screen scaled and centered to fit the given dimensions.</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        /// <param name="sprite">The sprite to draw.</param>
        /// <param name="x">The X-position at which to draw the sprite.</param>
        /// <param name="y">The X-position at which to draw the sprite.</param>
        /// <param name="size">The size to draw.</param>
        /// <param name="color">The color to tint the sprite.</param>
        public static void DrawSpriteWithin(this SpriteBatch spriteBatch, SpriteInfo? sprite, float x, float y, Vector2 size, Color? color = null)
        {
            sprite?.Draw(spriteBatch, (int)x, (int)y, size, color);
        }

        /// <summary>Draw a sprite to the screen scaled and centered to fit the given dimensions.</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        /// <param name="sheet">The sprite sheet containing the sprite.</param>
        /// <param name="sprite">The sprite coordinates and dimensions in the sprite sheet.</param>
        /// <param name="x">The X-position at which to draw the sprite.</param>
        /// <param name="y">The X-position at which to draw the sprite.</param>
        /// <param name="size">The size to draw.</param>
        /// <param name="color">The color to tint the sprite.</param>
        public static void DrawSpriteWithin(this SpriteBatch spriteBatch, Texture2D sheet, Rectangle sprite, float x, float y, Vector2 size, Color? color = null)
        {
            // calculate dimensions
            float largestDimension = Math.Max(sprite.Width, sprite.Height);
            float scale = size.X / largestDimension;
            float leftOffset = Math.Max((size.X - (sprite.Width * scale)) / 2, 0);
            float topOffset = Math.Max((size.Y - (sprite.Height * scale)) / 2, 0);

            // draw
            spriteBatch.DrawSprite(sheet, sprite, x + leftOffset, y + topOffset, color ?? Color.White, scale);
        }

        /// <summary>Draw a sprite to the screen.</summary>
        /// <param name="batch">The sprite batch.</param>
        /// <param name="x">The X-position at which to start the line.</param>
        /// <param name="y">The X-position at which to start the line.</param>
        /// <param name="size">The line dimensions.</param>
        /// <param name="color">The color to tint the sprite.</param>
        public static void DrawLine(this SpriteBatch batch, float x, float y, Vector2 size, Color? color = null)
        {
            batch.Draw(CommonHelper.Pixel, new Rectangle((int)x, (int)y, (int)size.X, (int)size.Y), color ?? Color.White);
        }

        /****
        ** UI
        ****/
        /// <summary>Draw a sprite to the screen.</summary>
        /// <param name="batch">The sprite batch.</param>
        /// <param name="sheet">The sprite sheet containing the sprite.</param>
        /// <param name="sprite">The sprite coordinates and dimensions in the sprite sheet.</param>
        /// <param name="x">The X-position at which to draw the sprite.</param>
        /// <param name="y">The X-position at which to draw the sprite.</param>
        /// <param name="width">The width to draw.</param>
        /// <param name="height">The height to draw.</param>
        /// <param name="color">The color to tint the sprite.</param>
        public static void Draw(this SpriteBatch batch, Texture2D sheet, Rectangle sprite, int x, int y, int width, int height, Color? color = null)
        {
            batch.Draw(sheet, new Rectangle(x, y, width, height), sprite, color ?? Color.White);
        }

        /// <summary>Draw a pretty hover box for the given text.</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        /// <param name="label">The text to display.</param>
        /// <param name="position">The position at which to draw the text.</param>
        /// <param name="wrapWidth">The maximum width to display.</param>
        public static Vector2 DrawHoverBox(SpriteBatch spriteBatch, string label, in Vector2 position, float wrapWidth)
        {
            const int paddingSize = 27;
            const int gutterSize = 20;

            Vector2 labelSize = spriteBatch.DrawTextBlock(Game1.smallFont, label, position + new Vector2(gutterSize), wrapWidth); // draw text to get wrapped text dimensions
            IClickableMenu.drawTextureBox(spriteBatch, Game1.menuTexture, new Rectangle(0, 256, 60, 60), (int)position.X, (int)position.Y, (int)labelSize.X + paddingSize + gutterSize, (int)labelSize.Y + paddingSize, Color.White);
            spriteBatch.DrawTextBlock(Game1.smallFont, label, position + new Vector2(gutterSize), wrapWidth); // draw again over texture box

            return labelSize + new Vector2(paddingSize);
        }

        /// <summary>Draw a tab texture to the screen.</summary>
        /// <param name="spriteBatch">The sprite batch to which to draw.</param>
        /// <param name="x">The X position at which to draw.</param>
        /// <param name="y">The Y position at which to draw.</param>
        /// <param name="innerWidth">The width of the button's inner content.</param>
        /// <param name="innerHeight">The height of the button's inner content.</param>
        /// <param name="innerDrawPosition">The position at which the content should be drawn.</param>
        /// <param name="align">The button's horizontal alignment relative to <paramref name="x"/>. The possible values are 0 (left), 1 (center), or 2 (right).</param>
        /// <param name="alpha">The button opacity, as a value from 0 (transparent) to 1 (opaque).</param>
        /// <param name="forIcon">Whether the button will contain an icon instead of text.</param>
        /// <param name="drawShadow">Whether to draw a shadow under the tab.</param>
        public static void DrawTab(SpriteBatch spriteBatch, int x, int y, int innerWidth, int innerHeight, out Vector2 innerDrawPosition, int align = 0, float alpha = 1, bool forIcon = false, bool drawShadow = true)
        {
            // calculate outer coordinates
            int outerWidth = innerWidth + CommonHelper.ButtonBorderWidth * 2;
            int outerHeight = innerHeight + Game1.tileSize / 3;
            int offsetX = align switch
            {
                1 => -outerWidth / 2,
                2 => -outerWidth,
                _ => 0
            };

            // calculate inner coordinates
            {
                int iconOffsetX = forIcon ? -Game1.pixelZoom : 0;
                int iconOffsetY = forIcon ? 2 * -Game1.pixelZoom : 0;
                innerDrawPosition = new Vector2(x + CommonHelper.ButtonBorderWidth + offsetX + iconOffsetX, y + CommonHelper.ButtonBorderWidth + iconOffsetY);
            }

            // draw texture
            IClickableMenu.drawTextureBox(spriteBatch, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x + offsetX, y, outerWidth, outerHeight + Game1.tileSize / 16, Color.White * alpha, drawShadow: drawShadow);
        }

        /// <summary>Draw a button background.</summary>
        /// <param name="spriteBatch">The sprite batch to which to draw.</param>
        /// <param name="position">The top-left pixel coordinate at which to draw the button.</param>
        /// <param name="contentSize">The button content's pixel size.</param>
        /// <param name="contentPos">The pixel position at which the content begins.</param>
        /// <param name="bounds">The button's outer bounds.</param>
        /// <param name="padding">The padding between the content and border.</param>
        public static void DrawButton(SpriteBatch spriteBatch, in Vector2 position, in Vector2 contentSize, out Vector2 contentPos, out Rectangle bounds, int padding = 0)
        {
            DrawContentBox(
                spriteBatch: spriteBatch,
                texture: CommonSprites.Button.Sheet,
                background: CommonSprites.Button.Background,
                top: CommonSprites.Button.Top,
                right: CommonSprites.Button.Right,
                bottom: CommonSprites.Button.Bottom,
                left: CommonSprites.Button.Left,
                topLeft: CommonSprites.Button.TopLeft,
                topRight: CommonSprites.Button.TopRight,
                bottomRight: CommonSprites.Button.BottomRight,
                bottomLeft: CommonSprites.Button.BottomLeft,
                position: position,
                contentSize: contentSize,
                contentPos: out contentPos,
                bounds: out bounds,
                padding: padding
            );
        }

        /// <summary>Draw a scroll background.</summary>
        /// <param name="spriteBatch">The sprite batch to which to draw.</param>
        /// <param name="position">The top-left pixel coordinate at which to draw the scroll.</param>
        /// <param name="contentSize">The scroll content's pixel size.</param>
        /// <param name="contentPos">The pixel position at which the content begins.</param>
        /// <param name="bounds">The scroll's outer bounds.</param>
        /// <param name="padding">The padding between the content and border.</param>
        public static void DrawScroll(SpriteBatch spriteBatch, in Vector2 position, in Vector2 contentSize, out Vector2 contentPos, out Rectangle bounds, int padding = 5)
        {
            DrawContentBox(
                spriteBatch: spriteBatch,
                texture: CommonSprites.Scroll.Sheet,
                background: in CommonSprites.Scroll.Background,
                top: CommonSprites.Scroll.Top,
                right: CommonSprites.Scroll.Right,
                bottom: CommonSprites.Scroll.Bottom,
                left: CommonSprites.Scroll.Left,
                topLeft: CommonSprites.Scroll.TopLeft,
                topRight: CommonSprites.Scroll.TopRight,
                bottomRight: CommonSprites.Scroll.BottomRight,
                bottomLeft: CommonSprites.Scroll.BottomLeft,
                position: position,
                contentSize: contentSize,
                contentPos: out contentPos,
                bounds: out bounds,
                padding: padding
            );
        }

        /// <summary>Draw a generic content box like a scroll or button.</summary>
        /// <param name="spriteBatch">The sprite batch to which to draw.</param>
        /// <param name="texture">The texture to draw.</param>
        /// <param name="background">The source rectangle for the background.</param>
        /// <param name="top">The source rectangle for the top border.</param>
        /// <param name="right">The source rectangle for the right border.</param>
        /// <param name="bottom">The source rectangle for the bottom border.</param>
        /// <param name="left">The source rectangle for the left border.</param>
        /// <param name="topLeft">The source rectangle for the top-left corner.</param>
        /// <param name="topRight">The source rectangle for the top-right corner.</param>
        /// <param name="bottomRight">The source rectangle for the bottom-right corner.</param>
        /// <param name="bottomLeft">The source rectangle for the bottom-left corner.</param>
        /// <param name="position">The top-left pixel coordinate at which to draw the button.</param>
        /// <param name="contentSize">The button content's pixel size.</param>
        /// <param name="contentPos">The pixel position at which the content begins.</param>
        /// <param name="bounds">The box's outer bounds.</param>
        /// <param name="padding">The padding between the content and border.</param>
        public static void DrawContentBox(SpriteBatch spriteBatch, Texture2D texture, in Rectangle background, in Rectangle top, in Rectangle right, in Rectangle bottom, in Rectangle left, in Rectangle topLeft, in Rectangle topRight, in Rectangle bottomRight, in Rectangle bottomLeft, in Vector2 position, in Vector2 contentSize, out Vector2 contentPos, out Rectangle bounds, int padding)
        {
            CommonHelper.GetContentBoxDimensions(topLeft, contentSize, padding, out int innerWidth, out int innerHeight, out int outerWidth, out int outerHeight, out int cornerWidth, out int cornerHeight);
            int x = (int)position.X;
            int y = (int)position.Y;

            // draw scroll background
            spriteBatch.Draw(texture, new Rectangle(x + cornerWidth, y + cornerHeight, innerWidth, innerHeight), background, Color.White);

            // draw borders
            spriteBatch.Draw(texture, new Rectangle(x + cornerWidth, y, innerWidth, cornerHeight), top, Color.White);
            spriteBatch.Draw(texture, new Rectangle(x + cornerWidth, y + cornerHeight + innerHeight, innerWidth, cornerHeight), bottom, Color.White);
            spriteBatch.Draw(texture, new Rectangle(x, y + cornerHeight, cornerWidth, innerHeight), left, Color.White);
            spriteBatch.Draw(texture, new Rectangle(x + cornerWidth + innerWidth, y + cornerHeight, cornerWidth, innerHeight), right, Color.White);

            // draw corners
            spriteBatch.Draw(texture, new Rectangle(x, y, cornerWidth, cornerHeight), topLeft, Color.White);
            spriteBatch.Draw(texture, new Rectangle(x, y + cornerHeight + innerHeight, cornerWidth, cornerHeight), bottomLeft, Color.White);
            spriteBatch.Draw(texture, new Rectangle(x + cornerWidth + innerWidth, y, cornerWidth, cornerHeight), topRight, Color.White);
            spriteBatch.Draw(texture, new Rectangle(x + cornerWidth + innerWidth, y + cornerHeight + innerHeight, cornerWidth, cornerHeight), bottomRight, Color.White);

            // set out params
            contentPos = new Vector2(x + cornerWidth + padding, y + cornerHeight + padding);
            bounds = new Rectangle(x, y, outerWidth, outerHeight);
        }

        /// <summary>Draw a sprite to the screen.</summary>
        /// <param name="batch">The sprite batch.</param>
        /// <param name="x">The X-position at which to start the line.</param>
        /// <param name="y">The X-position at which to start the line.</param>
        /// <param name="size">The line dimensions.</param>
        /// <param name="color">The color to tint the sprite.</param>
        public static void DrawLine(this SpriteBatch batch, float x, float y, in Vector2 size, in Color? color = null)
        {
            batch.Draw(CommonHelper.Pixel, new Rectangle((int)x, (int)y, (int)size.X, (int)size.Y), color ?? Color.White);
        }

        /// <summary>Draw a block of text to the screen with the specified wrap width.</summary>
        /// <param name="batch">The sprite batch.</param>
        /// <param name="font">The sprite font.</param>
        /// <param name="text">The block of text to write.</param>
        /// <param name="position">The position at which to draw the text.</param>
        /// <param name="wrapWidth">The width at which to wrap the text.</param>
        /// <param name="color">The text color.</param>
        /// <param name="bold">Whether to draw bold text.</param>
        /// <param name="scale">The font scale.</param>
        /// <returns>Returns the text dimensions.</returns>
        public static Vector2 DrawTextBlock(this SpriteBatch batch, SpriteFont font, string? text, in Vector2 position, float wrapWidth, in Color? color = null, bool bold = false, float scale = 1)
        {
            if (text == null)
                return new Vector2(0, 0);

            // get word list
            List<string> words = new List<string>();
            foreach (string word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                // split on newlines
                string wordPart = word;
                int newlineIndex;
                while ((newlineIndex = wordPart.IndexOf(Environment.NewLine, StringComparison.Ordinal)) >= 0)
                {
                    if (newlineIndex == 0)
                    {
                        words.Add(Environment.NewLine);
                        wordPart = wordPart.Substring(Environment.NewLine.Length);
                    }
                    else if (newlineIndex > 0)
                    {
                        words.Add(wordPart.Substring(0, newlineIndex));
                        words.Add(Environment.NewLine);
                        wordPart = wordPart.Substring(newlineIndex + Environment.NewLine.Length);
                    }
                }

                // add remaining word (after newline split)
                if (wordPart.Length > 0)
                    words.Add(wordPart);
            }

            // track draw values
            float xOffset = 0;
            float yOffset = 0;
            float lineHeight = font.MeasureString("ABC").Y * scale;
            float spaceWidth = CommonHelper.GetSpaceWidth(font) * scale;
            float blockWidth = 0;
            float blockHeight = lineHeight;
            foreach (string word in words)
            {
                // check wrap width
                float wordWidth = font.MeasureString(word).X * scale;
                if (word == Environment.NewLine || ((wordWidth + xOffset) > wrapWidth && (int)xOffset != 0))
                {
                    xOffset = 0;
                    yOffset += lineHeight;
                    blockHeight += lineHeight;
                }
                if (word == Environment.NewLine)
                    continue;

                // draw text
                Vector2 wordPosition = new Vector2(position.X + xOffset, position.Y + yOffset);
                if (bold)
                    Utility.drawBoldText(batch, word, font, wordPosition, color ?? Color.Black, scale);
                else
                    batch.DrawString(font, word, wordPosition, color ?? Color.Black, 0, Vector2.Zero, scale, SpriteEffects.None, 1);

                // update draw values
                if (xOffset + wordWidth > blockWidth)
                    blockWidth = xOffset + wordWidth;
                xOffset += wordWidth + spaceWidth;
            }

            // return text position & dimensions
            return new Vector2(blockWidth, blockHeight);
        }
    }
}
