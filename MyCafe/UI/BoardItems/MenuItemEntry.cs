using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace MyCafe.UI.BoardItems;

internal class MenuItemEntry : MenuEntry
{
    internal static Rectangle source_RemoveButton = new Rectangle(94, 32, 31, 32);

    internal Item Item;
    internal string Category;
    internal Rectangle target_removeButton;

    internal MenuItemEntry(Item item, string category)
    {
        Item = item;
        Category = category;
        target_removeButton = new Rectangle(Bounds.Width - 64, -4, source_RemoveButton.Width, source_RemoveButton.Height);
    }

    internal override void Draw(SpriteBatch b, int slotX, int slotY, bool editMode)
    {
        Item.drawInMenu(
            b,
            new Vector2(slotX, slotY - 22),
            0.5f,
            1f,
            1f,
            StackDrawType.Hide,
            Color.White,
            false
        );

        b.DrawString(
            Game1.smallFont,
            Item.DisplayName,
            new Vector2(slotX + 64, slotY),
            Color.White,
            0f,
            Vector2.Zero,
            new Vector2(0.75f, 0.75f),
            SpriteEffects.None,
            1f);

        if (editMode)
            b.Draw(
                Mod.Sprites,
                new Vector2(
                    slotX + target_removeButton.X,
                    slotY + target_removeButton.Y),
                source_RemoveButton,
                Color.White,
                0f,
                Vector2.Zero,
                Vector2.One,
                SpriteEffects.None,
                1f);
    }
}