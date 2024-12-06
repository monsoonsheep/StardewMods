using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;

namespace StardewMods.MyShops.Framework.UI.BoardItems;

internal class MenuItemEntry : MenuEntry
{
    internal Item Item;
    internal float Scale = 1f;
    internal int Price;

    internal MenuItemEntry(Item item) : base()
    {
        this.Item = item;
        this.Price = this.Item.modData.TryGetValue(Values.MODDATA_ITEM_PRICE, out string a)
            ? int.Parse(a)
            : item.sellToStorePrice();
    }

    internal override void Draw(SpriteBatch b, int slotX, int slotY)
    {
        // item sprite
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

        // truncate name
        string formattedDisplayName = this.Item.DisplayName;
        if (SpriteText.getWidthOfString(formattedDisplayName) > Bounds.Width - 300 && formattedDisplayName.Length > 27)
        {
            formattedDisplayName = formattedDisplayName.Substring(0, 27);
            formattedDisplayName += "...";
        }

        // item name
        b.DrawString(
            Game1.smallFont,
            formattedDisplayName,
            new Vector2(slotX + 64, slotY),
            Color.White,
            0f,
            Vector2.Zero,
            new Vector2(0.75f, 0.75f),
            SpriteEffects.None,
            1f);

        if (Mod.Config.ShowPricesInFoodMenu) {
            int right = slotX + Bounds.Width;

            // price
            b.DrawString(
                Game1.tinyFont,
                this.Price + " ",
                new Vector2(right - Game1.tinyFont.MeasureString(this.Price + " ").X - 39, slotY - 8),
                Color.White,
                0f, Vector2.Zero,
                1.5f,
                SpriteEffects.None,
                1f
            );

            // coin sprite
            StardewValley.Utility.drawWithShadow(
                b,
                Game1.mouseCursors,
                new Vector2(right - 30, slotY + 4),
                new Rectangle(193, 373, 9, 10),
                Color.White,
                0f,
                Vector2.Zero,
                2f,
                flipped: false,
                1f
            );
        }
    }
}
