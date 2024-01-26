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

    protected bool snappedOut;
    protected int defaultComponent;

    internal MenuPageBase(string name, Rectangle bounds, IClickableMenu parentMenu) : base(bounds.X, bounds.Y, bounds.Width, bounds.Height)
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
        if (!snappedOut)
            base.snapCursorToCurrentSnappedComponent();
        else
            currentlySnappedComponent = null;
    }

    public override void snapToDefaultClickableComponent()
    {
        snappedOut = false;

        setCurrentlySnappedComponentTo(defaultComponent);
    }

    public override void setCurrentlySnappedComponentTo(int id)
    {
        base.setCurrentlySnappedComponentTo(id);
        snapCursorToCurrentSnappedComponent();
    }

    protected void SnapOutOfMenu(int direction)
    {
        snappedOut = true;
        _parentMenu.setCurrentlySnappedComponentTo(direction);
    }
}
