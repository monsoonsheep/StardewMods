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

    internal bool InFocus;
    protected int defaultComponent;

    protected new CafeMenu _parentMenu;

    internal MenuPageBase(string name, Rectangle bounds, CafeMenu parentMenu) : base(bounds.X, bounds.Y, bounds.Width, bounds.Height)
    {
        Name = name;
        _parentMenu = parentMenu;
        Bounds = bounds;
    }

    public virtual bool TryHover(int x, int y)
    {
        HoverText = "";
        HoverTitle = "";
        return Bounds.Contains(x, y);
    }

    public override void snapCursorToCurrentSnappedComponent()
    {
        if (InFocus)
            base.snapCursorToCurrentSnappedComponent();
        else
            currentlySnappedComponent = null;
    }

    public override void snapToDefaultClickableComponent()
    {
        InFocus = true;
        setCurrentlySnappedComponentTo(defaultComponent);
    }

    public override void setCurrentlySnappedComponentTo(int id)
    {
        base.setCurrentlySnappedComponentTo(id);
        snapCursorToCurrentSnappedComponent();
    }

    protected virtual void SnapOut(int direction = 3)
    {
        InFocus = false;
        _parentMenu.SnapOutInDirection(direction);
    }
}
