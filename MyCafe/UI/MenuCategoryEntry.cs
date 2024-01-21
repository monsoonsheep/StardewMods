using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace MyCafe.UI;

internal class MenuCategoryEntry : MenuEntry
{
    internal readonly string Name;
    private readonly Color _color = new Color(new Vector3(13 / 255f, 21 / 255f, 40 / 255f));
    private readonly int _xPositionForCentering;
    private readonly int _lengthOfText;
    private readonly Rectangle source_sideLine = new Rectangle(128, 32, 5, 16);
    private readonly int width;

    internal MenuCategoryEntry(string name, int width)
    {
        Name = name;
        this.width = width;
        _lengthOfText = (int)Game1.smallFont.MeasureString(name).X;
        _xPositionForCentering = (int)((384 - 27 * 2 - _lengthOfText) / 2f);
    }

    internal override void Draw(SpriteBatch b, int slotX, int slotY, bool editMode)
    {
        b.DrawString(
            Game1.smallFont,
            Name,
            new Vector2(slotX + _xPositionForCentering + 2, slotY),
            _color,
            0f,
            Vector2.Zero,
            Vector2.One,
            SpriteEffects.None,
            1f);

        Rectangle stretch = source_sideLine;

        b.Draw(
            Mod.Sprites,
            new Vector2(slotX + 16, slotY + 8),
            stretch,
            Color.White,
            0f,
            Vector2.Zero,
            new Vector2(1, 1),
            SpriteEffects.None,
            1f);
        b.Draw(
            Mod.Sprites,
            new Vector2(slotX + width - 16, slotY + 8),
            stretch,
            Color.White,
            0f,
            Vector2.Zero,
            new Vector2(1, 1),
            SpriteEffects.FlipHorizontally,
            1f);

        stretch.X += 4;
        stretch.Width = 1;

        b.Draw(
            Mod.Sprites,
            new Vector2(slotX + 20, slotY + 8),
            stretch,
            Color.White,
            0f,
            Vector2.Zero,
            new Vector2(_xPositionForCentering - 32, 1f),
            SpriteEffects.None,
            1f);
        b.Draw(
            Mod.Sprites,
            new Vector2(slotX + _xPositionForCentering + _lengthOfText + 18, slotY + 8),
            stretch,
            Color.White,
            0f,
            Vector2.Zero,
            new Vector2(_xPositionForCentering - 32, 1),
            SpriteEffects.None,
            1f);
    }
}