using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;

namespace MyCafe.UI.Options;
internal abstract class OptionsElementBase(string label, Rectangle bounds, Texture2D sprites) : OptionsElement(label, bounds, -1)
{
    protected Texture2D Sprites = sprites;
    protected int CurrentlySnapped = 0;

    internal abstract Vector2 Snap(int direction);
}
