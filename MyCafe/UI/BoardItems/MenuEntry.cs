using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MyCafe.UI.BoardItems;

internal abstract class MenuEntry
{
    internal static Rectangle Bounds;
    internal bool EditMode;
    protected Texture2D Sprites;

    internal MenuEntry(Texture2D sprites)
    {
        this.Sprites = sprites;
    }

    internal abstract void Draw(SpriteBatch b, int slotX, int slotY);
}