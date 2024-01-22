using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;

namespace MyCafe.UI;
public abstract class MenuPageBase
{
    private readonly CafeMenu _parentMenu;
    internal Rectangle Bounds;
    internal string Name;

    internal MenuPageBase(string name, CafeMenu parentMenu)
    {
        Name = name;
        _parentMenu = parentMenu;
        Bounds = parentMenu.sideBoxBounds;
    }
    internal abstract void LeftClick(int x, int y);

    internal abstract void LeftClickHeld(int x, int y);

    internal abstract void ReleaseLeftClick(int x, int y);

    internal abstract void ScrollWheelAction(int direction);

    internal abstract void HoverAction(int x, int y);

    internal abstract void Draw(SpriteBatch b);
}
