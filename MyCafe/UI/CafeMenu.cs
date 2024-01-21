using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.UI.MenuBoard;
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

    private bool _editMode;

    private readonly Rectangle sideBoxBounds;

    // Menu board
    private const int MENU_SLOT_COUNT = 9;
    private readonly Rectangle source_menuBoard = new(0, 64, 384, 524);
    private readonly Rectangle source_editButton = new(0, 32, 47, 24);
    private readonly Rectangle source_menuLogo = new(134, 11, 94, 52);
    private readonly Rectangle target_menuBoard;
    private readonly Rectangle target_menuLogo;
    private readonly List<string> _categories = new();
    private readonly ClickableTextureComponent editButton;
    private readonly List<MenuEntry> _menuEntries = [];
    private readonly List<Rectangle> _menuSlots = new List<Rectangle>();

    // Menu scrolling
    private readonly ClickableTextureComponent _upArrow;
    private readonly ClickableTextureComponent _downArrow;
    private readonly ClickableTextureComponent _scrollBar;
    private bool _scrolling;
    private readonly Rectangle _scrollBarRunner;
    private int _menuCurrentIndex = 0;

    // Search
    private readonly TextBox _searchBarTextBox;

    // Config options
    private const int OPTION_SLOT_COUNT = 5;
    private readonly List<ClickableComponent> _optionSlots = new();
    private readonly List<OptionsElement> _options = new();
    private int _optionsCurrentIndex = 0;


    public CafeMenu()
        : base(
            Game1.uiViewport.Width / 2 - (800 + borderWidth * 2) / 2,
            Game1.uiViewport.Height / 2 - (600 + borderWidth * 2) / 2,
            800 + borderWidth * 2,
            600 + borderWidth * 2, 
            showUpperRightCloseButton: true)
    {
        Mod.Sprites = Mod.ModHelper.ModContent.Load<Texture2D>("assets/sprites.png");

        target_menuBoard = new Rectangle(this.xPositionOnScreen, yPositionOnScreen + Game1.pixelZoom + Game1.tileSize/2, 384, 524);
        target_menuLogo = new Rectangle(target_menuBoard.X + (int) ((source_menuBoard.Width - source_menuLogo.Width) / 2f), target_menuBoard.Y + 40, source_menuLogo.Width, source_menuLogo.Height);

        var targetEditButton = new Rectangle(target_menuBoard.X + 36, target_menuBoard.Y + 63, source_editButton.Width, source_editButton.Height);
        editButton = new ClickableTextureComponent(targetEditButton, Mod.Sprites, source_editButton, 1f);

        _upArrow = new ClickableTextureComponent(new Rectangle(target_menuBoard.X + target_menuBoard.Width + 2, target_menuBoard.Y + 2, 44, 48), Game1.mouseCursors, new Rectangle(421, 459, 11, 12), 4f);
        _downArrow = new ClickableTextureComponent(new Rectangle(_upArrow.bounds.X, target_menuBoard.Y + target_menuBoard.Height - 48 - 2, 44, 48), Game1.mouseCursors, new Rectangle(421, 472, 11, 12), 4f);
        _scrollBar = new ClickableTextureComponent(new Rectangle(_upArrow.bounds.X + 12, _upArrow.bounds.Y + _upArrow.bounds.Height + 4, 24, 40), Game1.mouseCursors, new Rectangle(435, 463, 6, 10), 4f);
        _scrollBarRunner = new Rectangle(_scrollBar.bounds.X, _upArrow.bounds.Y + _upArrow.bounds.Height + 4, _scrollBar.bounds.Width, target_menuBoard.Height - (_upArrow.bounds.Height*2) - 16);

        for (int i = 0; i < MENU_SLOT_COUNT; i++)
        {
            _menuSlots.Add(new Rectangle(
                target_menuBoard.X + 24,
                target_menuBoard.Y + 101 + (i * 43),
                target_menuBoard.Width - (27 * 2),
                43));
        }
        PopulateMenuEntries();

        sideBoxBounds = new Rectangle(
            xPositionOnScreen + target_menuBoard.Width + Game1.tileSize / 2,
            yPositionOnScreen,
            target_menuBoard.Width,
            target_menuBoard.Height);

        // Search
        _searchBarTextBox = new TextBox(Game1.content.Load<Texture2D>("LooseSprites\\textBox"), null, Game1.smallFont, Game1.textColor)
        {
            textLimit = 32,
            Selected = false,
            Text = "Search",
        };
        _searchBarTextBox.OnEnterPressed += (_) => CloseTextBox();

        // Config
        for (int i = 0; i < OPTION_SLOT_COUNT; i++)
            _optionSlots.Add(new ClickableComponent(
                new Rectangle(
                    sideBoxBounds.X + Game1.tileSize / 4,
                    sideBoxBounds.Y + Game1.tileSize * 5 / 4 + Game1.pixelZoom + i * ((this.height - Game1.tileSize * 2) / OPTION_SLOT_COUNT),
                    this.width - Game1.tileSize / 2,
                    (this.height - Game1.tileSize * 2) / OPTION_SLOT_COUNT + Game1.pixelZoom),
                i.ToString()));

        _options.Add(new OptionTimeSet(I18n.Menu_OpeningTime(), Mod.Cafe.OpeningTime.Value, 0700, 1800, (v) => Mod.Cafe.OpeningTime.Set(v)));
        _options.Add(new OptionTimeSet(I18n.Menu_ClosingTime(), Mod.Cafe.ClosingTime.Value, 1100, 2500, (v) => Mod.Cafe.ClosingTime.Set(v)));
    }


    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        if (GameMenu.forcePreventClose)
            return;

        if (_editMode)
        {
            for (int i = 0; i < _menuSlots.Count; i++)
            {
                if (_menuSlots[i].Contains(x, y)
                    && _menuCurrentIndex + i < _menuEntries.Count
                    && _menuEntries[_menuCurrentIndex + i] is MenuItemEntry entry
                    && entry.target_removeButton.Contains(x - _menuSlots[i].X, y - _menuSlots[i].Y))
                {
                    RemoveItem(_menuCurrentIndex + i);
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

        if (_downArrow.containsPoint(x, y) && _menuCurrentIndex < Math.Max(0, _menuEntries.Count - MENU_SLOT_COUNT))
        {
            DownArrowPressed();
            Game1.playSound("shwip");
        }
        else if (_upArrow.containsPoint(x, y) && _menuCurrentIndex > 0)
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
        _menuCurrentIndex = Math.Max(0, Math.Min(_menuEntries.Count - MENU_SLOT_COUNT, _menuCurrentIndex));
        _optionsCurrentIndex = Math.Max(0, Math.Min(_options.Count - OPTION_SLOT_COUNT, _optionsCurrentIndex));

        for (var index = 0; index < _optionSlots.Count; ++index)
            if (_optionSlots[index].bounds.Contains(x, y) 
                && _optionsCurrentIndex + index < _options.Count 
                && _options[_optionsCurrentIndex + index].bounds.Contains(x - _optionSlots[index].bounds.X, y - _optionSlots[index].bounds.Y))
            {
                _options[_optionsCurrentIndex + index].receiveLeftClick(x - _optionSlots[index].bounds.X, y - _optionSlots[index].bounds.Y);
                break;
            }
    }

    public override void releaseLeftClick(int x, int y)
    {
        base.releaseLeftClick(x, y);
        if (!GameMenu.forcePreventClose)
        {
            base.releaseLeftClick(x, y);    
            _scrolling = false;
        }
    }

    public override void leftClickHeld(int x, int y)
    {
        base.leftClickHeld(x, y);

        if (GameMenu.forcePreventClose)
            return;
        
        base.leftClickHeld(x, y);
        if (_scrolling)
        {
            int oldY = _scrollBar.bounds.Y;
            _scrollBar.bounds.Y = Math.Min(yPositionOnScreen + height - 64 - 12 - _scrollBar.bounds.Height, Math.Max(y, yPositionOnScreen + _upArrow.bounds.Height + 20));
            float percentage = (float)(y - _scrollBarRunner.Y) / (float) _scrollBarRunner.Height;
            _menuCurrentIndex = Math.Min(_menuEntries.Count - 7, Math.Max(0, (int)((float) _menuEntries.Count * percentage)));
            SetScrollBarToCurrentIndex();

            if (oldY != _scrollBar.bounds.Y)
            {
                Game1.playSound("shiny4");
            }
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
    }

    public override void receiveScrollWheelAction(int direction)
    {
        if (!GameMenu.forcePreventClose)
        {
            base.receiveScrollWheelAction(direction);
            if (direction > 0 && _menuCurrentIndex > 0)
            {
                UpArrowPressed();
                Game1.playSound("shiny4");
            }
            else if (direction < 0 && _menuCurrentIndex < Math.Max(0, _menuEntries.Count - MENU_SLOT_COUNT))
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

    public override void update(GameTime time)
    {
        base.update(time);
    }

    public override bool readyToClose()
    {
        return base.readyToClose();
    }

    public override void draw(SpriteBatch b)
    {
        // Big menu box
        Game1.drawDialogueBox(
            sideBoxBounds.X,
            sideBoxBounds.Y,
            sideBoxBounds.Width,
            sideBoxBounds.Height,
            speaker: false, drawOnlyBox: true);

        if (_hoverText != null)
        {
            //drawToolTip(b, _hoverText, "", null, heldItem: true, -1, 0, null, -1, null, moneyAmountToShowAtBottom: _hoverAmount);
            drawHoverText(b, _hoverText, Game1.smallFont);
        }

        b.End();
        b.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);

        if (_hoveredItem != null)
            drawToolTip(b, _hoveredItem.getDescription(), _hoveredItem.DisplayName, _hoveredItem, _heldItem != null);

        b.Draw(
            Mod.Sprites,
            target_menuBoard,
            source_menuBoard,
            Color.White,
            0f,
            Vector2.Zero,
            SpriteEffects.None,
            0.1f);

        b.Draw(
            Mod.Sprites,
            target_menuLogo,
            source_menuLogo,
            Color.White,
            0f,
            Vector2.Zero,
            SpriteEffects.None,
            1f);


        _heldItem?.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 8, Game1.getOldMouseY() + 8), 1f);

        for (int i = 0; i < _menuSlots.Count; i++)
        {
            if (_menuCurrentIndex + i < _menuEntries.Count)
            {
                _menuEntries[_menuCurrentIndex + i].Draw(b, _menuSlots[i].X, _menuSlots[i].Y, _editMode);
            }
        }

        editButton.draw(b);

        _upArrow.draw(b);
        _downArrow.draw(b);
        if (_menuEntries.Count > MENU_SLOT_COUNT)
        {
            _scrollBar.draw(b);
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 383, 6, 6), _scrollBarRunner.X, _scrollBarRunner.Y, _scrollBarRunner.Width, _scrollBarRunner.Height, Color.White, 4f, drawShadow: false);
        }

        // Options
        
        for (int index = 0; index < this._optionSlots.Count; ++index)
        {
            if (this._menuCurrentIndex >= 0 && this._menuCurrentIndex + index < this._options.Count)
                this._options[this._menuCurrentIndex + index].draw(b, this._optionSlots[index].bounds.X, this._optionSlots[index].bounds.Y);
        }
     
        b.End();
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        
        Game1.mouseCursorTransparency = 1f;
        drawMouse(b);

        if (!string.IsNullOrEmpty(_hoverText))
            drawHoverText(b, _hoverText, Game1.smallFont);

        if (shouldDrawCloseButton())
            base.draw(b);

        if (!Game1.options.SnappyMenus && !Game1.options.hardwareCursor)
            drawMouse(b, ignore_transparency: true);
    }


     
    private void DownArrowPressed()
    {
        UnsubscribeFromSelectedTextbox();
        _menuCurrentIndex++;
        SetScrollBarToCurrentIndex();
    }

    private void UpArrowPressed()
    {
        
        UnsubscribeFromSelectedTextbox();
        _menuCurrentIndex--;
        SetScrollBarToCurrentIndex();
    }

    private void SetScrollBarToCurrentIndex()
    {
        if (_menuEntries.Count > 0)
        {
            _scrollBar.bounds.Y = _scrollBarRunner.Height / Math.Max(1, _menuEntries.Count - MENU_SLOT_COUNT + 1) * _menuCurrentIndex + _upArrow.bounds.Bottom + 4;
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
                _menuEntries.Add(new MenuCategoryEntry(pair.Key));
            }
            foreach (var item in pair.Value)
            {
                _menuEntries.Add(new MenuItemEntry(item, pair.Key));
            }
        }

        _scrollBar.bounds.Height = (int) Math.Floor(((float)_scrollBarRunner.Height) / ((float)_menuEntries.Count / (float)MENU_SLOT_COUNT));
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
    
    private void CloseTextBox()
    {
        _searchBarTextBox.Selected = false;
        Game1.keyboardDispatcher.Subscriber = null;
    }
}
