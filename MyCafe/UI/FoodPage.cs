using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace MyCafe.UI;


public sealed class FoodPage : IClickableMenu
{
    private Item _heldItem;
    private Item _hoveredItem;

    private Texture2D sprites = Mod.Sprites;


    private const int SLOT_COUNT = 9;
    private Rectangle source_menuBoard = new(0, 64, 384, 524);
    private Rectangle source_editButton = new(0, 32, 47, 24);
    private Rectangle source_doneButton = new(47, 32, 47, 24);
    private Rectangle source_removeItemButton = new(94, 32, 31, 32);
    private Rectangle source_menuLogo = new(134, 11, 94, 52);

    private Rectangle target_menuBoard;
    private Rectangle target_menuLogo;
    private Rectangle target_editButton;
    private string _hoverText = "";

    private readonly List<string> _categories = new();

    private readonly ClickableTextureComponent editButton;
    private readonly List<MenuEntry> _menuEntries = [];

    private readonly List<Rectangle> _menuSlots = new List<Rectangle>();

    private ClickableTextureComponent _upArrow;
    private ClickableTextureComponent _downArrow;
    private ClickableTextureComponent _scrollBar;
    private bool _scrolling;
    private Rectangle _scrollBarRunner;
    private int _currentItemIndex = 0;

    private bool _editMode;

    public FoodPage(int x, int y, int width, int height)
        : base(x, y, width, height)
    {
        Mod.Sprites = Mod.ModHelper.ModContent.Load<Texture2D>("assets/sprites.png");

        target_menuBoard = new Rectangle(this.xPositionOnScreen + this.width - 384 - Game1.tileSize, yPositionOnScreen + Game1.pixelZoom + 18, 384, 524);
        // Center the logo based on the menuBoard coordinates
        target_menuLogo = new Rectangle(target_menuBoard.X + (int) ((source_menuBoard.Width - source_menuLogo.Width) / 2f), target_menuBoard.Y + 40, source_menuLogo.Width, source_menuLogo.Height);

        target_editButton = new Rectangle(target_menuBoard.X + 36, target_menuBoard.Y + 63, source_editButton.Width, source_editButton.Height);
        editButton = new ClickableTextureComponent(target_editButton, sprites, source_editButton, 1f);

        _upArrow = new ClickableTextureComponent(new Rectangle(target_menuBoard.X + target_menuBoard.Width + 2, target_menuBoard.Y + 2, 44, 48), Game1.mouseCursors, new Rectangle(421, 459, 11, 12), 4f);
        _downArrow = new ClickableTextureComponent(new Rectangle(_upArrow.bounds.X, target_menuBoard.Y + target_menuBoard.Height - 48 - 2, 44, 48), Game1.mouseCursors, new Rectangle(421, 472, 11, 12), 4f);
        _scrollBar = new ClickableTextureComponent(new Rectangle(_upArrow.bounds.X + 12, _upArrow.bounds.Y + _upArrow.bounds.Height + 4, 24, 40), Game1.mouseCursors, new Rectangle(435, 463, 6, 10), 4f);
        _scrollBarRunner = new Rectangle(_scrollBar.bounds.X, _upArrow.bounds.Y + _upArrow.bounds.Height + 4, _scrollBar.bounds.Width, target_menuBoard.Height - (_upArrow.bounds.Height*2) - 16);

        for (int i = 0; i < SLOT_COUNT; i++)
        {
            _menuSlots.Add(new Rectangle(
                target_menuBoard.X + 24,
                target_menuBoard.Y + 101 + (i * 43),
                target_menuBoard.Width - (27 * 2),
                43));
        }

        PopulateMenuEntries();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (GameMenu.forcePreventClose)
            return;

        if (_editMode)
        {
            for (int i = 0; i < _menuSlots.Count; i++)
            {
                if (_menuSlots[i].Contains(x, y)
                    && _currentItemIndex + i < _menuEntries.Count
                    && _menuEntries[_currentItemIndex + i] is MenuItemEntry entry
                    && entry.RemoveButtonBounds.Contains(x - _menuSlots[i].X, y - _menuSlots[i].Y))
                {
                    RemoveItem(_currentItemIndex + i);
                    return;
                }
            }
        }
        
        if (editButton.containsPoint(x, y))
        {
            _editMode = !_editMode;

            if (_editMode)
                editButton.sourceRect.X += editButton.sourceRect.Width;
            else
                editButton.sourceRect.X -= editButton.sourceRect.Width;
        }

        if (_downArrow.containsPoint(x, y) && _currentItemIndex < Math.Max(0, _menuEntries.Count - SLOT_COUNT))
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
        else if (!_downArrow.containsPoint(x, y) 
                 && x > xPositionOnScreen + width 
                 && x < xPositionOnScreen + width + 128 
                 && y > yPositionOnScreen 
                 && y < yPositionOnScreen + height)
        {
            _scrolling = true;
            leftClickHeld(x, y);
            releaseLeftClick(x, y);
        }
        _currentItemIndex = Math.Max(0, Math.Min(_menuEntries.Count - SLOT_COUNT, _currentItemIndex));
    }

    public override void releaseLeftClick(int x, int y)
    {
        if (!GameMenu.forcePreventClose)
        {
            base.releaseLeftClick(x, y);    
            _scrolling = false;
        }
    }

    public override void leftClickHeld(int x, int y)
    {
        if (GameMenu.forcePreventClose)
            return;
        
        base.leftClickHeld(x, y);
        if (_scrolling)
        {
            int oldY = _scrollBar.bounds.Y;
            _scrollBar.bounds.Y = Math.Min(yPositionOnScreen + height - 64 - 12 - _scrollBar.bounds.Height, Math.Max(y, yPositionOnScreen + _upArrow.bounds.Height + 20));
            float percentage = (float)(y - _scrollBarRunner.Y) / (float) _scrollBarRunner.Height;
            _currentItemIndex = Math.Min(_menuEntries.Count - 7, Math.Max(0, (int)((float) _menuEntries.Count * percentage)));
            SetScrollBarToCurrentIndex();

            if (oldY != _scrollBar.bounds.Y)
            {
                Game1.playSound("shiny4");
            }
        }
    }


    public override void performHoverAction(int x, int y)
    {
        _hoveredItem = null;
        _hoverText = "";
        base.performHoverAction(x, y);
    }

    public override void receiveScrollWheelAction(int direction)
    {
        if (!GameMenu.forcePreventClose)
        {
            base.receiveScrollWheelAction(direction);
            if (direction > 0 && _currentItemIndex > 0)
            {
                UpArrowPressed();
                Game1.playSound("shiny4");
            }
            else if (direction < 0 && _currentItemIndex < Math.Max(0, _menuEntries.Count - SLOT_COUNT))
            {
                DownArrowPressed();
                Game1.playSound("shiny4");
            }
            if (Game1.options.SnappyMenus)
            {
                base.snapCursorToCurrentSnappedComponent();
            }
        }
    }

    public override void draw(SpriteBatch b)
    {
        if (_hoverText != null)
        {
                //drawToolTip(b, _hoverText, "", null, heldItem: true, -1, 0, null, -1, null, moneyAmountToShowAtBottom: _hoverAmount);
                drawHoverText(b, _hoverText, Game1.smallFont);
        }

        b.End();
        b.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);

        b.Draw(
            sprites,
            target_menuBoard,
            source_menuBoard,
            Color.White,
            0f,
            Vector2.Zero,
            SpriteEffects.None,
            0.1f);

        b.Draw(
            sprites,
            target_menuLogo,
            source_menuLogo,
            Color.White,
            0f,
            Vector2.Zero,
            SpriteEffects.None,
            1f);


        if (_hoveredItem != null)
            drawToolTip(b, _hoveredItem.getDescription(), _hoveredItem.DisplayName, _hoveredItem, _heldItem != null);

        _heldItem?.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 8, Game1.getOldMouseY() + 8), 1f);

        for (int i = 0; i < _menuSlots.Count; i++)
        {
            if (_currentItemIndex + i < _menuEntries.Count)
            {
                _menuEntries[_currentItemIndex + i].Draw(b, _menuSlots[i].X, _menuSlots[i].Y, _editMode);
            }
        }

        editButton.draw(b);

        _upArrow.draw(b);
        _downArrow.draw(b);
        if (_menuEntries.Count > SLOT_COUNT)
        {
            _scrollBar.draw(b);
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 383, 6, 6), _scrollBarRunner.X, _scrollBarRunner.Y, _scrollBarRunner.Width, _scrollBarRunner.Height, Color.White, 4f, drawShadow: false);
        }
     
        b.End();
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        
        Game1.mouseCursorTransparency = 1f;
        drawMouse(b);
    }

    
    private void DownArrowPressed()
    {
        UnsubscribeFromSelectedTextbox();
        _currentItemIndex++;
        SetScrollBarToCurrentIndex();
    }

    private void UpArrowPressed()
    {
        
        UnsubscribeFromSelectedTextbox();
        _currentItemIndex--;
        SetScrollBarToCurrentIndex();
    }

    private void SetScrollBarToCurrentIndex()
    {
        if (_menuEntries.Count > 0)
        {
            _scrollBar.bounds.Y = _scrollBarRunner.Height / Math.Max(1, _menuEntries.Count - SLOT_COUNT + 1) * _currentItemIndex + _upArrow.bounds.Bottom + 4;
            if (_scrollBar.bounds.Y > _downArrow.bounds.Y - _scrollBar.bounds.Height - 4)
            {
                _scrollBar.bounds.Y = _downArrow.bounds.Y - _scrollBar.bounds.Height - 4;
            }
        }
    }


    private void UnsubscribeFromSelectedTextbox()
    {

    }



    private void PopulateMenuEntries()
    {
        _categories.Clear();
        _menuEntries.Clear();

        int slotHeight = _menuSlots.First().Height;
        int slotWidth = _menuSlots.First().Width;

        MenuEntry.Bounds = new Rectangle(0, 0, slotWidth, slotHeight);

        foreach (var pair in Mod.Cafe.MenuItems)
        {
            if (!_categories.Contains(pair.Key))
            {
                _categories.Add(pair.Key);
                _menuEntries.Add(new MenuCategoryEntry(pair.Key, slotWidth));
            }
            foreach (var item in pair.Value)
            {
                _menuEntries.Add(new MenuItemEntry(item, pair.Key));
            }
        }

        _scrollBar.bounds.Height = (int) Math.Floor(((float)_scrollBarRunner.Height) / ((float)_menuEntries.Count / (float)SLOT_COUNT));
    }

    private void RemoveItem(int index)
    {
        Item item = (_menuEntries[index] as MenuItemEntry)!.Item;

        string category = _categories.First();
        int i = index;
        while (i >= 0)
        {
            if (_menuEntries[i] is MenuCategoryEntry entry)
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
