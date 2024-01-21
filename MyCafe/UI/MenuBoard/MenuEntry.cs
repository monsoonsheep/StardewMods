using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;

namespace MyCafe.UI.MenuBoard;

internal abstract class MenuEntry
{
    internal static Rectangle Bounds;

    internal abstract void Draw(SpriteBatch b, int slotX, int slotY, bool editMode);
}