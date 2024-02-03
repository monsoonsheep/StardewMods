using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.Inventories;
using MyCafe.UI.BoardItems;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Menus;
using SObject = StardewValley.Object;
// ReSharper disable InconsistentNaming

namespace MyCafe.UI.Pages;
internal class MenuBoard : MenuPageBase
{
    // Menu board
    internal int slotCount = 9;
    internal static Rectangle source_Board = new(0, 64, 444, 576);
    internal static Rectangle source_Logo = new(134, 11, 94, 52);
    internal static Rectangle source_EditButton = new(41, 19, 15, 13);
    internal static Rectangle source_EditButtonCancel = new(31, 32, 31, 32);

    private readonly Rectangle target_Logo;
    private readonly Rectangle target_board;

    private readonly List<string> _categories = [];
    private readonly List<MenuEntry> _entries = [];

    public readonly List<ClickableComponent> _slots = [];

    // Scrolling
    public readonly ClickableTextureComponent _upArrow;
    public readonly ClickableTextureComponent _downArrow;
    public readonly ClickableTextureComponent _scrollBar;
    private bool _scrolling;
    private readonly Rectangle _scrollBarRunner;
    private int _currentItemIndex;

    public MenuBoard(CafeMenu parent, Rectangle bounds) : base("Menu", bounds, parent)
    {
        this.target_board = this.Bounds;

        this.target_Logo = new Rectangle(this.target_board.X + (int)((source_Board.Width - source_Logo.Width) / 2f), this.target_board.Y + 40,
            source_Logo.Width,
            source_Logo.Height);

        this._upArrow = new ClickableTextureComponent(
            new Rectangle(this.target_board.Right - 30, this.target_board.Y + 101, 44, 48),
            Game1.mouseCursors,
            new Rectangle(421, 459, 11, 12),
            4f);
        this._downArrow = new ClickableTextureComponent(
            new Rectangle(this._upArrow.bounds.X, this.target_board.Y + this.target_board.Height - 48 - 2, 44, 48),
            Game1.mouseCursors,
            new Rectangle(421, 472, 11, 12),
            4f);
        this._scrollBar = new ClickableTextureComponent(
            new Rectangle(this._upArrow.bounds.X + 12, this._upArrow.bounds.Y + this._upArrow.bounds.Height + 4, 24, 40),
            Game1.mouseCursors,
            new Rectangle(435, 463, 6, 10),
            4f);
        this._scrollBarRunner = new Rectangle(this._scrollBar.bounds.X, this._upArrow.bounds.Bottom + 4, this._scrollBar.bounds.Width,
            this._downArrow.bounds.Top - this._upArrow.bounds.Bottom - 4);

        for (int i = 0; i < this.slotCount; i++)
        {
            this._slots.Add(new ClickableComponent(new Rectangle(this.target_board.X + 24, this.target_board.Y + 111 + i * 43, this.target_board.Width - 27 * 2,
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

        MenuEntry.Bounds = new Rectangle(0, 0, this._slots.First().bounds.Width, this._slots.First().bounds.Height);

        this.PopulateMenuEntries();

        this.Bounds.Width += this._upArrow.bounds.Width;
        this.DefaultComponent = 1001;
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (Mod.Instance.Config.EnableScrollbarInMenuBoard)
        {
            if (this._entries.Count > this.slotCount)
            {
                if (this._downArrow.containsPoint(x, y) && this._currentItemIndex < Math.Max(0, this._entries.Count - this.slotCount))
                {
                    this.DownArrowPressed();
                    Game1.playSound("shwip");
                }
                else if (this._upArrow.containsPoint(x, y) && this._currentItemIndex > 0)
                {
                    this.UpArrowPressed();
                    Game1.playSound("shwip");
                }
                else if (this._scrollBar.containsPoint(x, y))
                {
                    this._scrolling = true;
                }
                else if (!this._downArrow.containsPoint(x, y))
                {
                    this._scrolling = true;
                    this.leftClickHeld(x, y);
                    this.releaseLeftClick(x, y);
                }
            }
        }

        for (int slotIndex = 0; slotIndex < this._slots.Count; slotIndex++)
        {
            if (this._slots[slotIndex].containsPoint(x, y))
            {
                // if not held, remove or edit
                // if held, add

                int itemIndex = this._currentItemIndex + slotIndex;
                if (this.ParentMenu.HeldItem is SObject held)
                {
                    while (itemIndex >= 0 && this._entries[itemIndex] is not MenuCategoryEntry)
                        itemIndex--;
                    
                    if (this._entries[itemIndex] is MenuCategoryEntry entry)
                    {
                        if (this.AddItem(held, entry.Name, this._currentItemIndex + slotIndex - itemIndex)) this.ParentMenu.HeldItem = null;
                        break;
                    }
                }
                else
                {
                    Item? item = (this._entries[this._currentItemIndex + slotIndex] as MenuItemEntry)?.Item;
                    if (item != null)
                    {
                        this.RemoveItem(this._currentItemIndex + slotIndex);
                        this.ParentMenu.HeldItem = item;
                    }
                }
            }
        }

        this._currentItemIndex = Math.Max(0, Math.Min(this._entries.Count - this.slotCount, this._currentItemIndex));
    }

    public override void releaseLeftClick(int x, int y)
    {
        this._scrolling = false;
    }

    public override void leftClickHeld(int x, int y)
    {
        if (GameMenu.forcePreventClose)
            return;
        if (this._scrolling)
        {
            int oldY = this._scrollBar.bounds.Y;
            this._scrollBar.bounds.Y = Math.Min(this.Bounds.Y + this.Bounds.Height - 64 - 12 - this._scrollBar.bounds.Height, Math.Max(y, this.Bounds.Y + this._upArrow.bounds.Height + 20));
            float percentage = (y - this._scrollBarRunner.Y) / (float)this._scrollBarRunner.Height;
            this._currentItemIndex = Math.Min(this._entries.Count - 7, Math.Max(0, (int)(this._entries.Count * percentage)));
            this.SetScrollBarToCurrentIndex();

            if (oldY != this._scrollBar.bounds.Y)
            {
                Game1.playSound("shiny4");
            }
        }
    }

    public override void receiveScrollWheelAction(int direction)
    {
        if (direction > 0 && this._currentItemIndex > 0)
        {
            this.UpArrowPressed();
            Game1.playSound("shiny4");
        }
        else if (direction < 0 && this._currentItemIndex < Math.Max(0, this._entries.Count - this.slotCount))
        {
            this.DownArrowPressed();
            Game1.playSound("shiny4");
        }
        if (Game1.options.SnappyMenus)
        {
            base.snapCursorToCurrentSnappedComponent();
        }
    }

    public override bool TryHover(int x, int y)
    {
        for (int i = this._currentItemIndex; i < Math.Min(this._currentItemIndex + this.slotCount, this._entries.Count); i++)
        {
            if (this._entries[i] is MenuItemEntry entry)
            {
                entry.Scale = Math.Max(1f, entry.Scale - 0.025f);
                if (this._slots[i - this._currentItemIndex].containsPoint(x, y))
                {
                    entry.Scale = Math.Min(entry.Scale + 0.05f, 1.1f);
                }
            }
            else
            {

            }
        }

        return base.TryHover(x, y);
    }

    protected override void customSnapBehavior(int direction, int oldRegion, int oldID)
    {
        if (direction is 1 or 3)
        {
            this.SnapOut(1);
            return;
        }

        if (direction == 2)
        {
            if (this._entries.Count > this.slotCount
                && oldID == 1000 + this.slotCount
                && this._currentItemIndex + this.slotCount < this._entries.Count)
            {
                this.DownArrowPressed();
            }
            else if (this._currentItemIndex + (oldRegion + 1 - 1001) < this._entries.Count)
            {
                base.setCurrentlySnappedComponentTo(oldRegion + 1);
            }
        }
        else if (direction == 0)
        {
            if (this._entries.Count > this.slotCount
                && oldID == 1001
                && this._currentItemIndex > 0)
            {
                this.UpArrowPressed();
            }
            else if (this._currentItemIndex + (oldRegion - 1 - 1001) < this._entries.Count)
            {
                base.setCurrentlySnappedComponentTo(oldRegion - 1);
            }
        }
    }

    public override void draw(SpriteBatch b)
    {
        // Background
        b.Draw(Mod.Sprites, this.target_board,
            source_Board,
            Color.White,
            0f,
            Vector2.Zero,
            SpriteEffects.None,
            0.1f);

        // Logo
        b.Draw(Mod.Sprites, this.target_Logo,
            source_Logo,
            Color.White,
            0f,
            Vector2.Zero,
            SpriteEffects.None,
            1f);

        // Menu entries
        for (int i = 0; i < this._slots.Count; i++)
        {
            if (this._currentItemIndex + i < this._entries.Count)
            {
                var entry = this._entries[this._currentItemIndex + i];

                entry.Draw(b, this._slots[i].bounds.X, this._slots[i].bounds.Y);

                if (this._slots[i].bounds.Contains(Game1.getMouseX(true), Game1.getMouseY(true)) && this._currentItemIndex + i < this._entries.Count)
                {
                    if (entry is MenuCategoryEntry)
                    {
                        if (this.ParentMenu.HeldItem != null)
                        {
                            drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 383, 6, 6), this._slots[i].bounds.X, this._slots[i].bounds.Y - 8, this._slots[i].bounds.Width, this._slots[i].bounds.Height,
                                Color.White, drawShadow: false, draw_layer: 0.9f);
                        }

                        // draw edit icon
                    }
                    else
                    {
                        if (this.ParentMenu.HeldItem != null)
                        {
                            drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 383, 6, 6), this._slots[i].bounds.X, this._slots[i].bounds.Y + 28, this._slots[i].bounds.Width, 4,
                                Color.OrangeRed, drawShadow: false, draw_layer: 0.9f);
                        }
                    }
                }
            }
        }

        // Scroll bar
        if (Mod.Instance.Config.EnableScrollbarInMenuBoard && this._entries.Count > this.slotCount)
        {
            this._upArrow.draw(b);
            this._downArrow.draw(b);
            drawTextureBox(b, Game1.mouseCursors, new Rectangle(435, 463, 6, 10), this._scrollBar.bounds.X, this._scrollBar.bounds.Y, this._scrollBar.bounds.Width, this._scrollBar.bounds.Height, Color.White, 4f, false, 1f);
            drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 383, 6, 6), this._scrollBarRunner.X, this._scrollBarRunner.Y, this._scrollBarRunner.Width, this._scrollBarRunner.Height, Color.White, 4f, drawShadow: false);
        }
    }

    private void DownArrowPressed()
    {
        this._currentItemIndex++;
        this.SetScrollBarToCurrentIndex();
    }

    private void UpArrowPressed()
    {
        this._currentItemIndex--;
        this.SetScrollBarToCurrentIndex();
    }

    private void SetScrollBarToCurrentIndex()
    {
        if (this._entries.Count > 0)
        {
            this._scrollBar.bounds.Y = this._upArrow.bounds.Bottom + this._scrollBarRunner.Height / Math.Max(1, this._entries.Count - this.slotCount + 1) * this._currentItemIndex + 4;
            if (this._scrollBar.bounds.Y > this._downArrow.bounds.Y - this._scrollBar.bounds.Height - 4)
            {
                this._scrollBar.bounds.Y = this._downArrow.bounds.Y - this._scrollBar.bounds.Height - 4;
            }
        }
    }

    private void PopulateMenuEntries()
    {
        this._categories.Clear();
        this._entries.Clear();

        foreach (KeyValuePair<MenuCategory, Inventory> pair in Mod.Cafe.Menu.ItemDictionary)
        {
            if (!this._categories.Contains(pair.Key.Name))
            {
                this._categories.Add(pair.Key.Name);
                this._entries.Add(new MenuCategoryEntry(pair.Key.Name));
            }

            foreach (var item in pair.Value)
            {
                this._entries.Add(new MenuItemEntry(item));
            }
        }

       
        this._scrollBar.bounds.Height = (int)(this._scrollBarRunner.Height / (float)(this._entries.Count - this.slotCount));
    }

    private bool AddItem(SObject item, string category, int index)
    {
        if (!Mod.Cafe.Menu.AddItem(item, category, index))
        {
            Log.Warn("Can't add that");
            return false;
        }

        this.PopulateMenuEntries();
        return true;
    }

    private void RemoveItem(int entryIndex)
    {
        Item item = (this._entries[entryIndex] as MenuItemEntry)!.Item;
        Mod.Cafe.Menu.RemoveItem(item);
        this.PopulateMenuEntries();
    }
}
