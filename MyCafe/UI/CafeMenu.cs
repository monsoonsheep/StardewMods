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

        menuBoardBounds = new Rectangle(
            xPositionOnScreen, 
            yPositionOnScreen + Game1.pixelZoom + Game1.tileSize, 
            MenuBoard.source_Board.Width,
            MenuBoard.source_Board.Height);

        sideBoxBounds = new Rectangle(
            xPositionOnScreen + menuBoardBounds.Width + Game1.tileSize,
            yPositionOnScreen + Game1.pixelZoom + Game1.tileSize * 2,
            menuBoardBounds.Width,
            menuBoardBounds.Height - Game1.tileSize * 2);

        _menuBoard = new MenuBoard(this, menuBoardBounds, Sprites);

        _pages = [
            new ItemsPage(this, sideBoxBounds, Sprites),
            new TimingPage(this, sideBoxBounds, Sprites),
        ];

#if YOUTUBE || TWITCH
        _pages.Add(new ChatIntegrationPage(this, sideBoxBounds));
#endif
        PopulateTabs();
        _menuBoard.populateClickableComponentList();
        _menuBoard.snapToDefaultClickableComponent();
    }

    private void PopulateTabs()
    {
        _tabs.Clear();
        for (var i = 0; i < _pages.Count; i++)
        {
            _tabs.Add(
                new ClickableTextureComponent(
                    new Rectangle(sideBoxBounds.X + 64 * i, sideBoxBounds.Y - Game1.tileSize - 12, 64, 64),
                    Sprites,
                    new Rectangle(0 + 16 * i, 0, 16, 16),
                    4f)
                {
                    name = $"tab{i}",
                    hoverText = _pages[i].Name,
                    myID = 12340 + i,
                    downNeighborID = -99999,
                    rightNeighborID = (i == 0) ? -99999 : 12340 + (i + 1),
                    leftNeighborID = (i == _pages.Count - 1) ? -99999 : 12340 + (i - 1),
                    tryDefaultIfNoDownNeighborExists = true,
                    //fullyImmutable = true
                });
        }

        ChangeTab(0);
    }

    private void ChangeTab(int index)
    {
        bool focus = _pages[_currentTab].InFocus;

        _currentTab = index;

        for (int i = 0; i < _tabs.Count; i++)
            _tabs[i].bounds.Y = sideBoxBounds.Y - Game1.tileSize - ((i == _currentTab) ? 0 : 10);

        _pages[_currentTab].populateClickableComponentList();
        _pages[_currentTab].allClickableComponents.AddRange(_tabs);
        if (focus)
            _pages[_currentTab].snapToDefaultClickableComponent();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        if (GameMenu.forcePreventClose)
            return;

        for (int i = 0; i < _tabs.Count; i++)
        {
            if (_tabs[i].containsPoint(x, y))
            {
                ChangeTab(i);
                return;
            }
        }

        if (menuBoardBounds.Contains(x, y))
        {
            _menuBoard.receiveLeftClick(x, y);
        }
        else if (sideBoxBounds.Contains(x, y))
        {
            _pages[_currentTab].receiveLeftClick(x, y);
        }
    }

    public override void releaseLeftClick(int x, int y)
    {
        if (!GameMenu.forcePreventClose)
            return;

        base.releaseLeftClick(x, y);

        if (menuBoardBounds.Contains(x, y))
        {
            _menuBoard.releaseLeftClick(x, y);
        }
        else if (sideBoxBounds.Contains(x, y))
        {
            _pages[_currentTab].releaseLeftClick(x, y);
        }

    }

    public override void leftClickHeld(int x, int y)
    {
        base.leftClickHeld(x, y);
        if (menuBoardBounds.Contains(x, y))
        {
            _menuBoard.leftClickHeld(x, y);
        }
        else if (sideBoxBounds.Contains(x, y))
        {
            _pages[_currentTab].leftClickHeld(x, y);
        }
    }

    public override void performHoverAction(int x, int y)
    {
        _hoverText = "";
        base.performHoverAction(x, y);

        foreach (var tab in _tabs)
        {
            if (tab.containsPoint(x, y))
            {
                tab.tryHover(x, y);
                _hoverText = tab.hoverText;
                return;
            }
        }

        _menuBoard.TryHover(x, y);
        _pages[_currentTab].TryHover(x, y);
    }

    public override void receiveScrollWheelAction(int direction)
    {
        if (GameMenu.forcePreventClose) 
            return;
        base.receiveScrollWheelAction(direction);

        var (x, y) = (Game1.getMouseX(), Game1.getMouseX());
        if (menuBoardBounds.Contains(x, y))
        {
            _menuBoard.receiveScrollWheelAction(direction);
        }
        else if (sideBoxBounds.Contains(x, y))
        {
            _pages[_currentTab].receiveScrollWheelAction(direction);
        }
    }

    public override void snapToDefaultClickableComponent()
    {
        Log.Error("snaptodefault in cafemenu");
        _menuBoard.snapToDefaultClickableComponent();
    }

    protected override void noSnappedComponentFound(int direction, int oldRegion, int oldID)
    {
        Log.Error("nosnappedfound in cafemenu");
        _menuBoard.snapToDefaultClickableComponent();
    }

    public override ClickableComponent getCurrentlySnappedComponent()
    {
        Log.Warn("getcurrentlysnapped was called on cafemenu");
        return _menuBoard.getCurrentlySnappedComponent();
    }

    public override void receiveGamePadButton(Buttons b)
    {
        base.receiveGamePadButton(b);
        switch (b)
        {
            case Buttons.RightTrigger:
                if (_pages[_currentTab].readyToClose())
                {
                    ChangeTab((_currentTab + 1) % _tabs.Count);
                }
                break;
            case Buttons.LeftTrigger:
                if (_pages[_currentTab].readyToClose())
                {
                    ChangeTab((_currentTab == 0) ? _tabs.Count - 1 : (_currentTab - 1));
                }
                break;
            default:
                _menuBoard.receiveGamePadButton(b);
                break;
        }
    }

    public override void setUpForGamePadMode()
    {
        base.setUpForGamePadMode();
        _menuBoard.setUpForGamePadMode();
        _pages[_currentTab].setUpForGamePadMode();
    }

    public override void receiveKeyPress(Keys key)
    {
        if (_pages[_currentTab].currentlySnappedComponent != null)
            _pages[_currentTab].receiveKeyPress(key);
        else
            _menuBoard.receiveKeyPress(key);
    }

    public override void draw(SpriteBatch b)
    {
        Game1.DrawBox(sideBoxBounds.X, sideBoxBounds.Y, sideBoxBounds.Width, sideBoxBounds.Height);
        
        b.End();
        b.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);

        _menuBoard.draw(b);
        _pages[_currentTab].draw(b);

        foreach (var tab in _tabs)
            tab.draw(b);

        b.End();
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
       
        HeldItem?.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 8, Game1.getOldMouseY() + 8), 1f, 1f, 1f, StackDrawType.Hide);
        
        if (!string.IsNullOrEmpty(_pages[_currentTab].HoverTitle))
        {
            drawHoverText(b, _pages[_currentTab].HoverTitle, Game1.smallFont);
        }
        else if (!string.IsNullOrEmpty(_hoverText))
        {
            drawHoverText(b, _hoverText, Game1.smallFont);
        }

        drawMouse(b, ignore_transparency: true);
    }

    internal void SnapOutInDirection(int direction)
    {
        if (direction == 1)
            _pages[_currentTab].snapToDefaultClickableComponent();
        else if (direction == 3) 
            _menuBoard.snapToDefaultClickableComponent();
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
            Game1.activeClickableMenu = new CafeMenu(Mod.Instance.Sprites);
        }

        return true;
    }
}