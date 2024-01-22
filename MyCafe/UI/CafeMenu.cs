using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    private string _hoverText = "";

    private Item _heldItem;
    private Item _hoveredItem;

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

        _menuBoard = new MenuBoard(this);

        _pages = [
            new ItemsPage(this),
            new TimingPage(this),
#if YOUTUBE || TWITCH
            new ChatIntegrationPage(this)
#endif
        ];

        PopulateTabs();
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
                    hoverText = _pages[i].Name
                });
        }

        _tabs[_currentTab].bounds.Y += 12;
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
            _menuBoard.LeftClick(x, y);
        }
        else if (sideBoxBounds.Contains(x, y))
        {
            _pages[_currentTab].LeftClick(x, y);
        }
    }

    public override void releaseLeftClick(int x, int y)
    {
        if (!GameMenu.forcePreventClose)
            return;

        base.releaseLeftClick(x, y);

        if (menuBoardBounds.Contains(x, y))
        {
            _menuBoard.ReleaseLeftClick(x, y);
        }
        else if (sideBoxBounds.Contains(x, y))
        {
            _pages[_currentTab].ReleaseLeftClick(x, y);
        }

    }

    public override void leftClickHeld(int x, int y)
    {
        base.leftClickHeld(x, y);
        if (menuBoardBounds.Contains(x, y))
        {
            _menuBoard.LeftClickHeld(x, y);
        }
        else if (sideBoxBounds.Contains(x, y))
        {
            _pages[_currentTab].LeftClickHeld(x, y);
        }
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
    }

    public override void performHoverAction(int x, int y)
    {
        _hoveredItem = null;
        _hoverText = "";
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
            _menuBoard.HoverAction(x, y);
        }
        else if (sideBoxBounds.Contains(x, y))
        {
            _pages[_currentTab].HoverAction(x, y);
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
            _menuBoard.ScrollWheelAction(direction);
        }
        else if (sideBoxBounds.Contains(x, y))
        {
            _pages[_currentTab].ScrollWheelAction(direction);
        }
    }

    public override void draw(SpriteBatch b)
    {
        Game1.DrawBox(sideBoxBounds.X, sideBoxBounds.Y, sideBoxBounds.Width, sideBoxBounds.Height);
        
        b.End();
        b.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);

        _menuBoard.Draw(b);

        foreach (var tab in _tabs)
        {
            tab.draw(b);
        }

        _pages[_currentTab].Draw(b);

        b.End();
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
       
        
        //if (_hoveredItem != null)
        //    drawToolTip(b, _hoveredItem.getDescription(), _hoveredItem.DisplayName, _hoveredItem, _heldItem != null);

        //_heldItem?.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 8, Game1.getOldMouseY() + 8), 1f);
        
        if (!string.IsNullOrEmpty(_hoverText))
        {
            //drawToolTip(b, _hoverText, "", null, heldItem: true, -1, 0, null, -1, null, moneyAmountToShowAtBottom: _hoverAmount);
            drawHoverText(b, _hoverText, Game1.smallFont);
        }

        if (shouldDrawCloseButton())
            base.draw(b);

        if (!Game1.options.SnappyMenus && !Game1.options.hardwareCursor)
            drawMouse(b, ignore_transparency: true);
    }

}
