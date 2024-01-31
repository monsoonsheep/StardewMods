using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace MyCafe.UI.BoardItems;

internal class MenuCategoryEntry : MenuEntry
{
    private static readonly Rectangle source_sideLine = new Rectangle(128, 32, 5, 16);

    internal readonly string Name;
    private readonly Color _color = new Color(new Vector3(13 / 255f, 21 / 255f, 40 / 255f));
    private readonly int _xPositionForCentering;
    private readonly int _lengthOfText;

    internal MenuCategoryEntry(string name, Texture2D sprites) : base(sprites)
    {
        this.Name = name;
        this._lengthOfText = (int)Game1.smallFont.MeasureString(name).X;
        this._xPositionForCentering = (int)((Bounds.Width - this._lengthOfText) / 2f);
    }

    internal override void Draw(SpriteBatch b, int slotX, int slotY)
    {
        // Category name text
        b.DrawString(
            Game1.smallFont, this.Name,
            new Vector2(slotX + this._xPositionForCentering + 2, slotY), this._color,
            0f,
            Vector2.Zero,
            Vector2.One,
            SpriteEffects.None,
            1f);

        Rectangle stretch = source_sideLine;

        b.Draw(this.Sprites,
            new Vector2(slotX + 16, slotY + 8),
            stretch,
            Color.White,
            0f,
            Vector2.Zero,
            new Vector2(1, 1),
            SpriteEffects.None,
            1f);
        b.Draw(this.Sprites,
            new Vector2(slotX + Bounds.Width - 16, slotY + 8),
            stretch,
            Color.White,
            0f,
            Vector2.Zero,
            new Vector2(1, 1),
            SpriteEffects.FlipHorizontally,
            1f);

        stretch.X += 4;
        stretch.Width = 1;

        b.Draw(this.Sprites,
            new Vector2(slotX + 20, slotY + 8),
            stretch,
            Color.White,
            0f,
            Vector2.Zero,
            new Vector2(this._xPositionForCentering - 32, 1f),
            SpriteEffects.None,
            1f);
        b.Draw(this.Sprites,
            new Vector2(slotX + this._xPositionForCentering + this._lengthOfText + 18, slotY + 8),
            stretch,
            Color.White,
            0f,
            Vector2.Zero,
            new Vector2(this._xPositionForCentering - 32, 1),
            SpriteEffects.None,
            1f);


        if (this.EditMode)
        {

        }
    }
}