using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Monsoonsheep.StardewMods.MyCafe.UI.BoardItems;

internal abstract class MenuEntry
{
    internal static Rectangle Bounds;

    internal MenuEntry()
    {
    }

    internal abstract void Draw(SpriteBatch b, int slotX, int slotY);
}
