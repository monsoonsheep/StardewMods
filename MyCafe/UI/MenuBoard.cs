using MyCafe.UI.BoardItems;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
// ReSharper disable InconsistentNaming

namespace MyCafe.UI;
internal class MenuBoard : MenuPageBase
{
    private bool _editMode;

    // Menu board
    internal int slotCount = 9;
    internal static Rectangle source_Board = new(0, 64, 444, 576);
    internal static Rectangle source_Logo = new(134, 11, 94, 52);
    private readonly Rectangle target_Logo;
    private readonly List<string> _categories = new();
    private readonly List<MenuEntry> _entries = [];
    public readonly List<ClickableComponent> _slots = [];

    // Menu scrolling
    public readonly ClickableTextureComponent _upArrow;
    public readonly ClickableTextureComponent _downArrow;
    public readonly ClickableTextureComponent _scrollBar;
    private bool _scrolling;
    private readonly Rectangle _scrollBarRunner;
    private int _currentItemIndex = 0;

    public MenuBoard(CafeMenu parent, Rectangle bounds) : base("Menu", bounds, parent)
    {
        Bounds = parent.menuBoardBounds;
        target_Logo = new Rectangle(Bounds.X + (int) ((source_Board.Width - source_Logo.Width) / 2f), Bounds.Y + 40, source_Logo.Width, source_Logo.Height);

        _upArrow = new ClickableTextureComponent(new Rectangle(Bounds.X + Bounds.Width + 2, Bounds.Y + 2, 44, 48), Game1.mouseCursors, new Rectangle(421, 459, 11, 12), 4f);
        _downArrow = new ClickableTextureComponent(new Rectangle(_upArrow.bounds.X, Bounds.Y + Bounds.Height - 48 - 2, 44, 48), Game1.mouseCursors, new Rectangle(421, 472, 11, 12), 4f);
        
        _scrollBar = new ClickableTextureComponent(new Rectangle(_upArrow.bounds.X + 12, _upArrow.bounds.Y + _upArrow.bounds.Height + 4, 24, 40), Game1.mouseCursors, new Rectangle(435, 463, 6, 10), 4f);
        
        _scrollBarRunner = new Rectangle(_scrollBar.bounds.X, _upArrow.bounds.Y + _upArrow.bounds.Height + 4, _scrollBar.bounds.Width, Bounds.Height - (_upArrow.bounds.Height*2) - 16);

        for (int i = 0; i < slotCount; i++)
        {
            _slots.Add(new ClickableComponent(new Rectangle(
                Bounds.X + 24,
                Bounds.Y + 101 + (i * 43),
                Bounds.Width - (27 * 2),
                43), $"slot{i}")
            {
                region = 1001 + i,
                myID = 1001 + i,
                downNeighborID = -7777,
                upNeighborID = -7777,
                rightNeighborID = -7777,
                leftNeighborID = 7777
            });
        }
        PopulateMenuEntries();
    }

    private void PopulateMenuEntries()
    {
        _categories.Clear();
        _entries.Clear();

        int slotHeight = _slots.First().bounds.Height;
        int slotWidth = _slots.First().bounds.Width;

        MenuEntry.Bounds = new Rectangle(0, 0, slotWidth, slotHeight);

        foreach (var pair in Mod.Cafe.MenuItems)
        {
            if (!_categories.Contains(pair.Key))
            {
                _categories.Add(pair.Key);
                _entries.Add(new MenuCategoryEntry(pair.Key));
            }
            foreach (var item in pair.Value)
            {
                _entries.Add(new MenuItemEntry(item, pair.Key));
            }
        }

        _scrollBar.bounds.Height = (int) Math.Floor((double) (_scrollBarRunner.Height) / ((float) _entries.Count / slotCount));
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (_editMode)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].bounds.Contains(x, y)
                    && _currentItemIndex + i < _entries.Count
                    && _entries[_currentItemIndex + i] is MenuItemEntry entry
                    && entry.target_removeButton.Contains(x - _slots[i].bounds.X, y - _slots[i].bounds.Y))
                {
                    RemoveItem(_currentItemIndex + i);
                    return;
                }
            }
        }

        if (_entries.Count > slotCount)
        {
            if (_downArrow.containsPoint(x, y) && _currentItemIndex < Math.Max(0, _entries.Count - slotCount))
            {
                DownArrowPressed();
                Game1.playSound("shwip");
            }
            else if (_upArrow.containsPoint(x, y) && _currentItemIndex > 0)
            {
                UpArrowPressed();
                Game1.playSound("shwip");
            }
            else if (_scrollBar.containsPoint(x, y))
            {
                _scrolling = true;
            }
            else if (!_downArrow.containsPoint(x, y) && x > Bounds.X + Bounds.Width && x < Bounds.X + Bounds.Width + 128 && y > Bounds.Y && y < Bounds.Y + Bounds.Height)
            {
                _scrolling = true;
                leftClickHeld(x, y);
                releaseLeftClick(x, y);
            }
        }
        
        _currentItemIndex = Math.Max(0, Math.Min(_entries.Count - slotCount, _currentItemIndex));
    }

    public override void releaseLeftClick(int x, int y)
    {
        _scrolling = false;
    }

    public override void leftClickHeld(int x, int y)
    {
        if (GameMenu.forcePreventClose)
            return;
        if (_scrolling)
        {
            int oldY = _scrollBar.bounds.Y;
            _scrollBar.bounds.Y = Math.Min(Bounds.Y + Bounds.Height - 64 - 12 - _scrollBar.bounds.Height, Math.Max(y, Bounds.Y + _upArrow.bounds.Height + 20));
            float percentage = (float)(y - _scrollBarRunner.Y) / (float) _scrollBarRunner.Height;
            _currentItemIndex = Math.Min(_entries.Count - 7, Math.Max(0, (int)((float) _entries.Count * percentage)));
            SetScrollBarToCurrentIndex();

            if (oldY != _scrollBar.bounds.Y)
            {
                Game1.playSound("shiny4");
            }
        }
    }

    public override void receiveScrollWheelAction(int direction)
    {
        if (direction > 0 && _currentItemIndex > 0)
        {
            UpArrowPressed();
            Game1.playSound("shiny4");
        }
        else if (direction < 0 && _currentItemIndex < Math.Max(0, _entries.Count - slotCount))
        {
            DownArrowPressed();
            Game1.playSound("shiny4");
        }
        if (Game1.options.SnappyMenus)
        {
            base.snapCursorToCurrentSnappedComponent();
        }
    }

    public override  void performHoverAction(int x, int y)
    {
        // Show tooltip?
    }

    public override void snapToDefaultClickableComponent()
    {
        setCurrentlySnappedComponentTo(1001);
        //_pages[_currentTab].snapToDefaultClickableComponent();
    }

    public override void automaticSnapBehavior(int direction, int oldRegion, int oldID)
    {
        Log.Debug("auto");
        base.automaticSnapBehavior(direction, oldRegion, oldID);
    }

    public override bool IsAutomaticSnapValid(int direction, ClickableComponent a, ClickableComponent b)
    {
        Log.Debug("valid");
        return base.IsAutomaticSnapValid(direction, a, b);
    }

    public override void setCurrentlySnappedComponentTo(int id)
    {
        base.setCurrentlySnappedComponentTo(id);
        snapCursorToCurrentSnappedComponent();
    }

    protected override void customSnapBehavior(int direction, int oldRegion, int oldID)
    {
        if (direction == 2)
        {
            if (_entries.Count > slotCount 
                && oldID == 1000 + slotCount 
                && _currentItemIndex + slotCount < _entries.Count)
            {
                DownArrowPressed();
            }
            else
            {
                base.setCurrentlySnappedComponentTo(oldRegion + 1);
            }
        }
        else if (direction == 0)
        {
            if (_entries.Count > slotCount 
                && oldID == 1001 
                && _currentItemIndex > 0)
            {
                UpArrowPressed();
            }
            else
            {
                base.setCurrentlySnappedComponentTo(oldRegion - 1);
            }
        }
        else if (direction == 1)
        {
            // parent menu take control to side box
        }
    }

    public override void draw(SpriteBatch b)
    {
        // Background
        b.Draw(
            Mod.Sprites,
            Bounds,
            source_Board,
            Color.White,
            0f,
            Vector2.Zero,
            SpriteEffects.None,
            0.1f);

        // Logo
        b.Draw(
            Mod.Sprites,
            target_Logo,
            source_Logo,
            Color.White,
            0f,
            Vector2.Zero,
            SpriteEffects.None,
            1f);

        // Menu entries
        for (int i = 0; i < _slots.Count; i++)
        {
            if (_currentItemIndex + i < _entries.Count)
            {
                _entries[_currentItemIndex + i].Draw(b, _slots[i].bounds.X, _slots[i].bounds.Y, _editMode);
            }
        }

        // Scroll bar
        if (_entries.Count > slotCount)
        {
            _upArrow.draw(b);
            _downArrow.draw(b);
            _scrollBar.draw(b);
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 383, 6, 6), _scrollBarRunner.X, _scrollBarRunner.Y, _scrollBarRunner.Width, _scrollBarRunner.Height, Color.White, 4f, drawShadow: false);
        }

        if (currentlySnappedComponent != null)
        {
            drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 383, 6, 6), currentlySnappedComponent.bounds.X, currentlySnappedComponent.bounds.Y, currentlySnappedComponent.bounds.Width, currentlySnappedComponent.bounds.Height, Color.White);
        }
    }

    private void DownArrowPressed()
    {
        _currentItemIndex++;
        SetScrollBarToCurrentIndex();
    }

    private void UpArrowPressed()
    {
        _currentItemIndex--;
        SetScrollBarToCurrentIndex();
    }

    private void SetScrollBarToCurrentIndex()
    {
        if (_entries.Count > 0)
        {
            _scrollBar.bounds.Y = _scrollBarRunner.Height / Math.Max(1, _entries.Count - slotCount + 1) * _currentItemIndex + _upArrow.bounds.Bottom + 4;
            if (_scrollBar.bounds.Y > _downArrow.bounds.Y - _scrollBar.bounds.Height - 4)
            {
                _scrollBar.bounds.Y = _downArrow.bounds.Y - _scrollBar.bounds.Height - 4;
            }
        }
    }

    
    private void RemoveItem(int index)
    {
        Item item = (_entries[index] as MenuItemEntry)!.Item;

        string category = _categories.First();
        int i = index;
        while (i >= 0)
        {
            if (_entries[i] is MenuCategoryEntry entry)
            {
                category = entry.Name;
                break;
            }
            i--;
        }

        Mod.Cafe.RemoveFromMenu(category, item.ItemId);
        PopulateMenuEntries();
    }
}
