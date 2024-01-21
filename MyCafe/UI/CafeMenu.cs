using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace MyCafe.UI;


public sealed class CafeMenu : IClickableMenu
{
    private int _currentTab;

    private string _hoverText = "";

    private readonly List<ClickableComponent> _tabs = new List<ClickableComponent>();
    private readonly List<IClickableMenu> _pages = new List<IClickableMenu>();

    public CafeMenu()
        : base(
            Game1.uiViewport.Width / 2 - (800 + borderWidth * 2) / 2,
            Game1.uiViewport.Height / 2 - (600 + borderWidth * 2) / 2,
            800 + borderWidth * 2,
            600 + borderWidth * 2, showUpperRightCloseButton: true)
    {
        InitializePages();
    }

    private void InitializePages()
    {
        _tabs.Clear();
        _pages.Clear();

        _tabs.Add(new ClickableComponent(new Rectangle(xPositionOnScreen + 64, yPositionOnScreen + tabYPositionRelativeToMenuY + 64, 64, 64),
            "food", "Food")
        {
            myID = 12340,
            downNeighborID = 0,
            rightNeighborID = 12341,
            tryDefaultIfNoDownNeighborExists = true,
            fullyImmutable = true
        });
        _pages.Add(new FoodPage(xPositionOnScreen + Game1.tileSize / 4, yPositionOnScreen + Game1.tileSize * 5 / 4 + Game1.pixelZoom, width, height));

        _tabs.Add(new ClickableComponent(new Rectangle(xPositionOnScreen + 128, yPositionOnScreen + tabYPositionRelativeToMenuY + 64, 64, 64),
            "settings", "Settings")
        {
            myID = 12341,
            downNeighborID = 0,
            rightNeighborID = 12342,
            tryDefaultIfNoDownNeighborExists = true,
            fullyImmutable = true
        });
        _pages.Add(new ConfigPage(xPositionOnScreen + 16, yPositionOnScreen, width, height));

        _pages[_currentTab].populateClickableComponentList();
        AddTabButtonsToCurrentMenu();
    }

    internal void ChangeTab(int whichTab, bool playSound = true)
    {
        _currentTab = whichTab;
        width = 800 + IClickableMenu.borderWidth * 2;
        base.initializeUpperRightCloseButton();

        if (playSound)
            Game1.playSound("smallSelect");

        _pages[_currentTab].populateClickableComponentList();
        AddTabButtonsToCurrentMenu();

        foreach (var t in _tabs)
            t.downNeighborID = -99999;

        if (Game1.options.SnappyMenus)
            snapToDefaultClickableComponent();
    }

    private void AddTabButtonsToCurrentMenu()
    {
        _pages[_currentTab].allClickableComponents.AddRange(_tabs);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);

        for (int i = 0; i < _tabs.Count; i++)
        {
            if (_tabs[i].containsPoint(x, y) && _currentTab != i && _pages[_currentTab].readyToClose())
            {
                ChangeTab(i);
                return;
            }
        }

        _pages[_currentTab].receiveLeftClick(x, y);
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
        InitializePages();
    }

    public override void performHoverAction(int x, int y)
    {
        base.performHoverAction(x, y);
        _hoverText = "";
        _pages[_currentTab].performHoverAction(x, y);
        foreach (ClickableComponent c in _tabs)
        {
            if (c.containsPoint(x, y))
            {
                _hoverText = c.label;
                break;
            }
        }
    }

    public override void update(GameTime time)
    {
        base.update(time);
        _pages[_currentTab].update(time);
    }

    public override void releaseLeftClick(int x, int y)
    {
        base.releaseLeftClick(x, y);
        _pages[_currentTab].releaseLeftClick(x, y);
    }

    public override void leftClickHeld(int x, int y)
    {
        base.leftClickHeld(x, y);
        _pages[_currentTab].leftClickHeld(x, y);
    }

    public override bool readyToClose()
    {
        return _pages[_currentTab].readyToClose();
    }

    public override void receiveScrollWheelAction(int direction)
    {
        _pages[_currentTab].receiveScrollWheelAction(direction);
    }

    public override void draw(SpriteBatch b)
    {
        // Big menu box
        Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, _pages[_currentTab].width, _pages[_currentTab].height, speaker: false, drawOnlyBox: true);

        //b.End();
        //b.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);

        // Draw tabs
        for (var i = 0; i < _tabs.Count; i++)
        {
            b.Draw(Mod.Sprites, new Vector2(_tabs[i].bounds.X, _tabs[i].bounds.Y + (_currentTab == i ? 8 : 0)),
                new Rectangle(i * 16, 0, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.0001f);
        }

        //b.End();
        //b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        // Draw current menu page
        _pages[_currentTab].draw(b);

        if (!string.IsNullOrEmpty(_hoverText))
            drawHoverText(b, _hoverText, Game1.smallFont);

        if (shouldDrawCloseButton())
            base.draw(b);

        if (!Game1.options.SnappyMenus && !Game1.options.hardwareCursor)
            drawMouse(b, ignore_transparency: true);
    }
}
