using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
// ReSharper disable InconsistentNaming

namespace MyCafe.UI;

public sealed class CafeMenu : IClickableMenu
{
    internal Texture2D Sprites;

    internal Item? HeldItem;

    private string? _hoverText;

    // Menu board
    internal Rectangle menuBoardBounds;

    // Side box
    internal Rectangle sideBoxBounds;

    private readonly MenuBoard _menuBoard;

    // Tabs
    private readonly List<ClickableTextureComponent> _tabs = new();
    private readonly List<MenuPageBase> _pages;
    private int _currentTab;

    public CafeMenu(Texture2D sprites)
        : base(
            Game1.uiViewport.Width / 2 - (800 + borderWidth * 2) / 2,
            Game1.uiViewport.Height / 2 - (600 + borderWidth * 2) / 2,
            800 + borderWidth * 2,
            Game1.uiViewport.Height,
            showUpperRightCloseButton: true)
    {
        this.Sprites = sprites;

        this.menuBoardBounds = new Rectangle(this.xPositionOnScreen, this.yPositionOnScreen + Game1.pixelZoom + Game1.tileSize,
            MenuBoard.source_Board.Width,
            MenuBoard.source_Board.Height);

        this.sideBoxBounds = new Rectangle(this.xPositionOnScreen + this.menuBoardBounds.Width + Game1.tileSize, this.yPositionOnScreen + Game1.pixelZoom + Game1.tileSize * 2, this.menuBoardBounds.Width, this.menuBoardBounds.Height - Game1.tileSize * 2);

        this._menuBoard = new MenuBoard(this, this.menuBoardBounds, this.Sprites);

        this._pages = [
            new ItemsPage(this, this.sideBoxBounds, this.Sprites),
            new TimingPage(this, this.sideBoxBounds, this.Sprites),
        ];

#if YOUTUBE || TWITCH
        _pages.Add(new ChatIntegrationPage(this, sideBoxBounds));
#endif
        this.PopulateTabs();
        this._menuBoard.populateClickableComponentList();
        this._menuBoard.snapToDefaultClickableComponent();
    }

    private void PopulateTabs()
    {
        this._tabs.Clear();
        for (int i = 0; i < this._pages.Count; i++)
        {
            this._tabs.Add(
                new ClickableTextureComponent(
                    new Rectangle(this.sideBoxBounds.X + 64 * i, this.sideBoxBounds.Y - Game1.tileSize - 12, 64, 64), this.Sprites,
                    new Rectangle(0 + 16 * i, 0, 16, 16),
                    4f)
                {
                    name = $"tab{i}",
                    hoverText = this._pages[i].Name,
                    myID = 12340 + i,
                    downNeighborID = -99999,
                    rightNeighborID = (i == 0) ? -99999 : 12340 + (i + 1),
                    leftNeighborID = (i == this._pages.Count - 1) ? -99999 : 12340 + (i - 1),
                    tryDefaultIfNoDownNeighborExists = true,
                    //fullyImmutable = true
                });
        }

        this.ChangeTab(0);
    }

    private void ChangeTab(int index)
    {
        bool focus = this._pages[this._currentTab].InFocus;

        this._currentTab = index;

        for (int i = 0; i < this._tabs.Count; i++) this._tabs[i].bounds.Y = this.sideBoxBounds.Y - Game1.tileSize - ((i == this._currentTab) ? 0 : 10);

        this._pages[this._currentTab].populateClickableComponentList();
        this._pages[this._currentTab].allClickableComponents.AddRange(this._tabs);
        if (focus) this._pages[this._currentTab].snapToDefaultClickableComponent();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        if (GameMenu.forcePreventClose)
            return;

        for (int i = 0; i < this._tabs.Count; i++)
        {
            if (this._tabs[i].containsPoint(x, y))
            {
                this.ChangeTab(i);
                return;
            }
        }

        if (this.menuBoardBounds.Contains(x, y))
        {
            this._menuBoard.receiveLeftClick(x, y);
        }
        else if (this.sideBoxBounds.Contains(x, y))
        {
            this._pages[this._currentTab].receiveLeftClick(x, y);
        }
    }

    public override void releaseLeftClick(int x, int y)
    {
        if (!GameMenu.forcePreventClose)
            return;

        base.releaseLeftClick(x, y);

        if (this.menuBoardBounds.Contains(x, y))
        {
            this._menuBoard.releaseLeftClick(x, y);
        }
        else if (this.sideBoxBounds.Contains(x, y))
        {
            this._pages[this._currentTab].releaseLeftClick(x, y);
        }

    }

    public override void leftClickHeld(int x, int y)
    {
        base.leftClickHeld(x, y);
        if (this.menuBoardBounds.Contains(x, y))
        {
            this._menuBoard.leftClickHeld(x, y);
        }
        else if (this.sideBoxBounds.Contains(x, y))
        {
            this._pages[this._currentTab].leftClickHeld(x, y);
        }
    }

    public override void performHoverAction(int x, int y)
    {
        this._hoverText = "";
        base.performHoverAction(x, y);

        foreach (var tab in this._tabs)
        {
            if (tab.containsPoint(x, y))
            {
                tab.tryHover(x, y);
                this._hoverText = tab.hoverText;
                return;
            }
        }

        this._menuBoard.TryHover(x, y);
        this._pages[this._currentTab].TryHover(x, y);
    }

    public override void receiveScrollWheelAction(int direction)
    {
        if (GameMenu.forcePreventClose)
            return;
        base.receiveScrollWheelAction(direction);

        var (x, y) = (Game1.getMouseX(), Game1.getMouseX());
        if (this.menuBoardBounds.Contains(x, y))
        {
            this._menuBoard.receiveScrollWheelAction(direction);
        }
        else if (this.sideBoxBounds.Contains(x, y))
        {
            this._pages[this._currentTab].receiveScrollWheelAction(direction);
        }
    }

    public override void snapToDefaultClickableComponent()
    {
        Log.Error("snaptodefault in cafemenu");
        this._menuBoard.snapToDefaultClickableComponent();
    }

    protected override void noSnappedComponentFound(int direction, int oldRegion, int oldID)
    {
        Log.Error("nosnappedfound in cafemenu");
        this._menuBoard.snapToDefaultClickableComponent();
    }

    public override ClickableComponent getCurrentlySnappedComponent()
    {
        Log.Warn("getcurrentlysnapped was called on cafemenu");
        return this._menuBoard.getCurrentlySnappedComponent();
    }

    public override void receiveGamePadButton(Buttons b)
    {
        base.receiveGamePadButton(b);
        switch (b)
        {
            case Buttons.RightTrigger:
                if (this._pages[this._currentTab].readyToClose())
                {
                    this.ChangeTab((this._currentTab + 1) % this._tabs.Count);
                }
                break;
            case Buttons.LeftTrigger:
                if (this._pages[this._currentTab].readyToClose())
                {
                    this.ChangeTab((this._currentTab == 0) ? this._tabs.Count - 1 : (this._currentTab - 1));
                }
                break;
            default:
                this._menuBoard.receiveGamePadButton(b);
                break;
        }
    }

    public override void setUpForGamePadMode()
    {
        base.setUpForGamePadMode();
        this._menuBoard.setUpForGamePadMode();
        this._pages[this._currentTab].setUpForGamePadMode();
    }

    public override void receiveKeyPress(Keys key)
    {
        if (this._pages[this._currentTab].currentlySnappedComponent != null)
            this._pages[this._currentTab].receiveKeyPress(key);
        else
            this._menuBoard.receiveKeyPress(key);
    }

    public override void draw(SpriteBatch b)
    {
        Game1.DrawBox(this.sideBoxBounds.X, this.sideBoxBounds.Y, this.sideBoxBounds.Width, this.sideBoxBounds.Height);

        b.End();
        b.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);

        this._menuBoard.draw(b);
        this._pages[this._currentTab].draw(b);

        foreach (var tab in this._tabs)
            tab.draw(b);

        b.End();
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        this.HeldItem?.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 8, Game1.getOldMouseY() + 8), 1f, 1f, 1f, StackDrawType.Hide);

        if (!string.IsNullOrEmpty(this._pages[this._currentTab].HoverTitle))
        {
            drawHoverText(b, this._pages[this._currentTab].HoverTitle, Game1.smallFont);
        }
        else if (!string.IsNullOrEmpty(this._hoverText))
        {
            drawHoverText(b, this._hoverText, Game1.smallFont);
        }

        this.drawMouse(b, ignore_transparency: true);
    }

    internal void SnapOutInDirection(int direction)
    {
        if (direction == 1)
            this._pages[this._currentTab].snapToDefaultClickableComponent();
        else if (direction == 3) this._menuBoard.snapToDefaultClickableComponent();
    }

    internal void SaveButtonPressed()
    {

    }

    internal void LoadButtonPressed()
    {

    }

    public static bool Action_OpenCafeMenu(GameLocation location, string[] args, Farmer player, Point tile)
    {
        if (!player.IsMainPlayer) return false;

        if (Game1.activeClickableMenu == null && Context.IsPlayerFree)
        {
            Log.Debug("Opened cafe menu menu!");
            Game1.activeClickableMenu = new CafeMenu(Mod.Sprites);
        }

        return true;
    }
}
