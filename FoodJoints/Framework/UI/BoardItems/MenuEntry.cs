using Microsoft.Xna.Framework.Graphics;

namespace StardewMods.FoodJoints.Framework.UI.BoardItems;

internal abstract class MenuEntry
{
    internal static Rectangle Bounds;

    internal MenuEntry()
    {
    }

    internal abstract void Draw(SpriteBatch b, int slotX, int slotY);
}
