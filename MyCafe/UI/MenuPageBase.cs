using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace MyCafe.UI;
public abstract class MenuPageBase : IClickableMenu
{
    internal Rectangle Bounds;
    internal string Name;

    protected Texture2D Sprites;

    internal string? HoverTitle;
    internal string? HoverText;

    internal bool InFocus;
    protected int DefaultComponent;

    protected new CafeMenu _parentMenu;

    internal MenuPageBase(string name, Rectangle bounds, CafeMenu parentMenu, Texture2D sprites) : base(bounds.X, bounds.Y, bounds.Width, bounds.Height)
    {
        this.Sprites = sprites;

        this.Name = name;
        this._parentMenu = parentMenu;
        this.Bounds = bounds;
    }

    public virtual bool TryHover(int x, int y)
    {
        this.HoverText = "";
        this.HoverTitle = "";
        return this.Bounds.Contains(x, y);
    }

    public override void snapCursorToCurrentSnappedComponent()
    {
        if (this.InFocus)
            base.snapCursorToCurrentSnappedComponent();
        else
            this.currentlySnappedComponent = null;
    }

    public override void snapToDefaultClickableComponent()
    {
        this.InFocus = true;
        this.setCurrentlySnappedComponentTo(this.DefaultComponent);
    }

    public override void setCurrentlySnappedComponentTo(int id)
    {
        base.setCurrentlySnappedComponentTo(id);
        this.snapCursorToCurrentSnappedComponent();
    }

    public override void receiveKeyPress(Keys key)
    {
        if (Game1.globalFade)
        {
            return;
        }
        if (Game1.options.menuButton.Contains(new InputButton(key)))
        {
            Game1.playSound("smallSelect");
            if (this.readyToClose())
            {
                Game1.exitActiveMenu();
            }
        }
        else if (Game1.options.SnappyMenus)
        {
            base.receiveKeyPress(key);
        }
    }

    protected virtual void SnapOut(int direction = 3)
    {
        this.InFocus = false;
        this._parentMenu.SnapOutInDirection(direction);
    }
}
