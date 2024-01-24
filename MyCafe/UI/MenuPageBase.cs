using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;

namespace MyCafe.UI;
public abstract class MenuPageBase : IClickableMenu
{
    internal Rectangle Bounds;
    internal string Name;

    internal string HoverTitle;
    internal string HoverText;

    internal MenuPageBase(string name, Rectangle bounds, IClickableMenu parentMenu)
    {
        Name = name;
        _parentMenu = parentMenu;
        Bounds = bounds;
    }
}
