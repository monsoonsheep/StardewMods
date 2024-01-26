using MyCafe.UI.BoardItems;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
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
    internal static Rectangle source_EditButton = new(0, 32, 31, 32);
    internal static Rectangle source_EditButtonCancel = new(31, 32, 31, 32);

    private readonly Rectangle target_Logo;
    private readonly Rectangle target_board;

    private readonly List<string> _categories = [];
    private readonly List<MenuEntry> _entries = [];

    public readonly List<ClickableComponent> _slots = [];
    public readonly ClickableTextureComponent _editButton;

    // Scrolling
    public readonly ClickableTextureComponent _upArrow;
    public readonly ClickableTextureComponent _downArrow;
    public readonly ClickableTextureComponent _scrollBar;
    private bool _scrolling;
    private readonly Rectangle _scrollBarRunner;
    private int _currentItemIndex;

    public MenuBoard(CafeMenu parent, Rectangle bounds) : base("Menu", bounds, parent)
    {
        Mod.Sprites = Mod.ModHelper.ModContent.Load<Texture2D>("assets/sprites.png");
        target_board = Bounds;
        
        target_Logo = new Rectangle(target_board.X + (int) ((source_Board.Width - source_Logo.Width) / 2f), target_board.Y + 40, source_Logo.Width, source_Logo.Height);

        _upArrow = new ClickableTextureComponent(new Rectangle(target_board.Right - 30, target_board.Y + 101, 44, 48), Game1.mouseCursors, new Rectangle(421, 459, 11, 12), 4f);
        _downArrow = new ClickableTextureComponent(new Rectangle(_upArrow.bounds.X, target_board.Y + target_board.Height - 48 - 2, 44, 48), Game1.mouseCursors, new Rectangle(421, 472, 11, 12), 4f);
        _scrollBar = new ClickableTextureComponent(new Rectangle(_upArrow.bounds.X + 12, _upArrow.bounds.Y + _upArrow.bounds.Height + 4, 24, 40), Game1.mouseCursors, new Rectangle(435, 463, 6, 10), 4f);
        _scrollBarRunner = new Rectangle(_scrollBar.bounds.X, _upArrow.bounds.Bottom + 4, _scrollBar.bounds.Width, (_downArrow.bounds.Top - _upArrow.bounds.Bottom) - 4);

        _editButton = new ClickableTextureComponent(new Rectangle(
                target_board.X + source_Board.Width - source_EditButton.Width * 2 - 64,
                target_board.Y + 64,
                source_EditButton.Width, source_EditButton.Height
            ),
            Mod.Sprites,
            source_EditButton,
            2f);

        for (int i = 0; i < slotCount; i++)
        {
            _slots.Add(new ClickableComponent(new Rectangle(
                    target_board.X + 24,
                    target_board.Y + 101 + (i * 43),
                    target_board.Width - (27 * 2),
                    43), 
                $"slot{i}")
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

        Bounds.Width += _upArrow.bounds.Width;
        defaultComponent = 1001;
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

        _scrollBar.bounds.Height = (int) ((_scrollBarRunner.Height) / (float) (_entries.Count - slotCount));
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (_editMode)
        {
            //for (int i = 0; i < _slots.Count; i++)
            //{
            //    if (_slots[i].bounds.Contains(x, y)
            //        && _currentItemIndex + i < _entries.Count
            //        && _entries[_currentItemIndex + i] is MenuItemEntry entry
            //        && entry.target_removeButton.Contains(x - _slots[i].bounds.X, y - _slots[i].bounds.Y))
            //    {
            //        RemoveItem(_currentItemIndex + i);
            //        return;
            //    }
            //}
        }

        if (_editButton.containsPoint(x, y))
        {
            _editMode = !_editMode;
        }

        if (Mod.Config.EnableScrollbarInMenuBoard)
        {
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
                else if (!_downArrow.containsPoint(x, y))
                {
                    _scrolling = true;
                    leftClickHeld(x, y);
                    releaseLeftClick(x, y);
                }
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
            float percentage = (y - _scrollBarRunner.Y) / (float) _scrollBarRunner.Height;
            _currentItemIndex = Math.Min(_entries.Count - 7, Math.Max(0, (int)(_entries.Count * percentage)));
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

    public override bool TryHover(int x, int y)
    {
        return base.TryHover(x, y);
        // Show tooltip?
    }

   
    protected override void customSnapBehavior(int direction, int oldRegion, int oldID)
    {
        if (direction is 1 or 3)
        {
            snappedOut = true;
            _parentMenu.setCurrentlySnappedComponentTo(5);
            return;
            // parent menu take control to side box
        }

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
    }

    public override void draw(SpriteBatch b)
    {
        // Background
        b.Draw(
            Mod.Sprites,
            target_board,
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
                _entries[_currentItemIndex + i].Draw(b, _slots[i].bounds.X, _slots[i].bounds.Y);
            }
        }

        // Scroll bar
        if (Mod.Config.EnableScrollbarInMenuBoard && _entries.Count > slotCount)
        {
            _upArrow.draw(b);
            _downArrow.draw(b);
            // Scrollbar
            drawTextureBox(b, Game1.mouseCursors, new Rectangle(435, 463, 6, 10), _scrollBar.bounds.X, _scrollBar.bounds.Y, _scrollBar.bounds.Width, _scrollBar.bounds.Height, Color.White, 4f, false, 1f);
            // Scrollbar runner
            drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 383, 6, 6), _scrollBarRunner.X, _scrollBarRunner.Y, _scrollBarRunner.Width, _scrollBarRunner.Height, Color.White, 4f, drawShadow: false);
        }

        if (currentlySnappedComponent != null)
        {
            drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 383, 6, 6), currentlySnappedComponent.bounds.X, currentlySnappedComponent.bounds.Y - 8, currentlySnappedComponent.bounds.Width, currentlySnappedComponent.bounds.Height, Color.White, drawShadow: false);
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
            _scrollBar.bounds.Y = _upArrow.bounds.Bottom + _scrollBarRunner.Height / Math.Max(1, _entries.Count - slotCount + 1) * _currentItemIndex + 4;
            if (_scrollBar.bounds.Y > _downArrow.bounds.Y - _scrollBar.bounds.Height - 4)
            {
                _scrollBar.bounds.Y = _downArrow.bounds.Y - _scrollBar.bounds.Height - 4;
            }
        }
    }

    private void ToggleEditMode()
    {
        _editMode = !_editMode;
        _editButton.sourceRect = _editMode ? source_EditButtonCancel : source_EditButton;
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
