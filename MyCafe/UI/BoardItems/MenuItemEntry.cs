using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace MyCafe.UI.BoardItems;

internal class MenuItemEntry : MenuEntry
{
    internal Item Item;
    internal string Category;
    internal float Scale = 1f;

    internal MenuItemEntry(Item item, string category, Texture2D sprites) : base(sprites)
    {
        this.Item = item;
        this.Category = category;
    }

    internal override void Draw(SpriteBatch b, int slotX, int slotY)
    {
        this.Item.drawInMenu(
            b,
            new Vector2(slotX, slotY - 22),
            0.5f * this.Scale,
            1f,
            1f,
            StackDrawType.Hide,
            Color.White,
            false
        );

        b.DrawString(
            Game1.smallFont, this.Item.DisplayName,
            new Vector2(slotX + 64, slotY),
            Color.White,
            0f,
            Vector2.Zero,
            new Vector2(0.75f, 0.75f),
            SpriteEffects.None,
            1f);
    }
}