using Microsoft.Xna.Framework;
using StardewValley.Menus;

namespace MyCafe.UI.Options;
internal abstract class OptionsElementBase(string label, Rectangle bounds) : OptionsElement(label, bounds, -1)
{
    protected int CurrentlySnapped = 0;

    internal abstract Vector2 Snap(int direction);
}
