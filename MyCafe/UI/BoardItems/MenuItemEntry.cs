using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace MyCafe.UI.BoardItems;

internal class MenuItemEntry : MenuEntry
{
    internal Item Item;
    internal float Scale = 1f;

    internal MenuItemEntry(Item item) : base()
    {
        this.Item = item;
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
