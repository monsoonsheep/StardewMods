using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MyCafe.CustomerFactory;
using MyCafe.UI.BoardItems;
using MyCafe.UI.Options;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
// ReSharper disable InconsistentNaming

namespace MyCafe.UI;

public sealed class CafeMenu : IClickableMenu
{
    private Item _heldItem;
    private Item _hoveredItem;

    private string _hoverText;

    private readonly MenuBoard _menuBoard;

    // Menu board
    internal Rectangle menuBoardBounds;

    // Side box
    internal Rectangle sideBoxBounds;

    // Tabs
    private readonly List<ClickableTextureComponent> _tabs = new();
    private readonly List<MenuPageBase> _pages;
    private int _currentTab;

    public CafeMenu()
        : base(
            Game1.uiViewport.Width / 2 - (800 + borderWidth * 2) / 2,
            Game1.uiViewport.Height / 2 - (600 + borderWidth * 2) / 2,
            800 + borderWidth * 2,
            Game1.uiViewport.Height,
            showUpperRightCloseButton: true)
    {
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

        _menuBoard = new MenuBoard(this, menuBoardBounds);

        _pages = [
            new ItemsPage(this, sideBoxBounds),
            new TimingPage(this, sideBoxBounds),
        ];

#if YOUTUBE || TWITCH
        _pages.Add(new ChatIntegrationPage(this, sideBoxBounds));
#endif
        PopulateTabs();
        _menuBoard.populateClickableComponentList();
        _menuBoard.setCurrentlySnappedComponentTo(1001);
    }

    private void PopulateTabs()
    {
        _tabs.Clear();
        for (var i = 0; i < _pages.Count; i++)
        {
            _tabs.Add(
                new ClickableTextureComponent(
                    new Rectangle(sideBoxBounds.X + 64 * i, sideBoxBounds.Y - Game1.tileSize - 12, 64, 64),
                    Mod.Sprites,
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
                    fullyImmutable = true
                });
        }

        SetTab(0);
    }

    private void SetTab(int index)
    {
        for (int i = 0; i < _tabs.Count; i++)
        {
            _tabs[i].bounds.Y = sideBoxBounds.Y - Game1.tileSize - 12;
            if (i == index)
                _tabs[i].bounds.Y += 12;
        }

        _currentTab = index;
        _pages[_currentTab].populateClickableComponentList();
        _pages[_currentTab].allClickableComponents.AddRange(_tabs);
    }

    public override void populateClickableComponentList()
    {
        base.populateClickableComponentList();
        foreach (var tab in _tabs)
        {
            allClickableComponents.Add(tab);
        }
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
                SetTab(i);
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

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
    }

    public override void performHoverAction(int x, int y)
    {
        _hoverText = "";
        _hoveredItem = null;
        base.performHoverAction(x, y);

        foreach (var tab in _tabs)
        {
            if (tab.containsPoint(x, y))
            {
                _hoverText = tab.hoverText;
                return;
            }
        }

        if (menuBoardBounds.Contains(x, y))
        {
            _menuBoard.performHoverAction(x, y);
        }
        else if (sideBoxBounds.Contains(x, y))
        {
            _pages[_currentTab].performHoverAction(x, y);
        }
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
        _menuBoard.snapToDefaultClickableComponent();
        //_pages[_currentTab].snapToDefaultClickableComponent();
    }

    protected override void noSnappedComponentFound(int direction, int oldRegion, int oldID)
    {
        _menuBoard.snapToDefaultClickableComponent();
    }

    public override void receiveGamePadButton(Buttons b)
    {
        base.receiveGamePadButton(b);
        switch (b)
        {
            case Buttons.RightTrigger:
                if (_pages[_currentTab].readyToClose())
                {
                    SetTab(_currentTab + 1);
                }
                break;
            case Buttons.LeftTrigger:
                if (_pages[_currentTab].readyToClose())
                {
                    SetTab(_currentTab - 1);
                }
                break;
            default:
                //_pages[_currentTab].receiveGamePadButton(b);
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
        if (Game1.options.menuButton.Contains(new InputButton(key)) && readyToClose())
        {
            Game1.exitActiveMenu();
            Game1.playSound("bigDeSelect");
        }
        _menuBoard.receiveKeyPress(key);
    }

    public override ClickableComponent getCurrentlySnappedComponent()
    {
        Log.Warn("getcurrentlysnapped was called on cafemenu");
        return _menuBoard.getCurrentlySnappedComponent();
    }

    public override void setCurrentlySnappedComponentTo(int id)
    {   
        Log.Warn("setcurrentlysnapped was called on cafemenu");
        _menuBoard.setCurrentlySnappedComponentTo(id);
    }

    public override void draw(SpriteBatch b)
    {
        Game1.DrawBox(sideBoxBounds.X, sideBoxBounds.Y, sideBoxBounds.Width, sideBoxBounds.Height);
        
        b.End();
        b.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);

        _menuBoard.draw(b);

        foreach (var tab in _tabs)
        {
            tab.draw(b);
        }

        _pages[_currentTab].draw(b);

        b.End();
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
       
        
        //if (_hoveredItem != null)
        //    drawToolTip(b, _hoveredItem.getDescription(), _hoveredItem.DisplayName, _hoveredItem, _heldItem != null);

        //_heldItem?.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 8, Game1.getOldMouseY() + 8), 1f);
        
        if (!string.IsNullOrEmpty(_pages[_currentTab].HoverTitle))
        {
            //drawToolTip(b, _hoverText, "", null, heldItem: true, -1, 0, null, -1, null, moneyAmountToShowAtBottom: _hoverAmount);
            //drawToolTip(b, _hoverText, HoverTitle, null);
            drawHoverText(b, _pages[_currentTab].HoverTitle, Game1.smallFont);
        }
        else if (!string.IsNullOrEmpty(_hoverText))
        {
            drawHoverText(b, _hoverText, Game1.smallFont);
        }

        if (shouldDrawCloseButton())
            base.draw(b);

        if (!Game1.options.SnappyMenus && !Game1.options.hardwareCursor)
            drawMouse(b, ignore_transparency: true);
    }

}
