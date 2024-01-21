using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using SUtility = StardewValley.Utility;

namespace MyCafe.UI;


public sealed class ConfigPage : IClickableMenu
{
    private readonly List<ClickableComponent> _optionSlots = new();
    private readonly List<OptionsElement> _options = new();

    private int _optionsSlotHeld = -1;
    private int _currentItemIndex;

    private const int ITEMS_PER_PAGE = 5;

    public ConfigPage(int x, int y, int width, int height) : base(x, y, width, height)
    {
        for (int i = 0; i < ITEMS_PER_PAGE; i++)
            this._optionSlots.Add(new ClickableComponent(
                new Rectangle(
                    this.xPositionOnScreen + Game1.tileSize / 4,
                    this.yPositionOnScreen + Game1.tileSize * 5 / 4 + Game1.pixelZoom + i * ((this.height - Game1.tileSize * 2) / ITEMS_PER_PAGE),
                    this.width - Game1.tileSize / 2,
                    (this.height - Game1.tileSize * 2) / ITEMS_PER_PAGE + Game1.pixelZoom),
                i.ToString()));

        this._options.Add(new OptionTimeSet(I18n.Menu_OpeningTime(), Mod.Cafe.OpeningTime.Value, 0700, 1800, (v) => Mod.Cafe.OpeningTime.Set(v)));
        this._options.Add(new OptionTimeSet(I18n.Menu_ClosingTime(), Mod.Cafe.ClosingTime.Value, 1100, 2500, (v) => Mod.Cafe.ClosingTime.Set(v)));
    }


    internal string FormatTime(int time)
    {
        return Game1.getTimeOfDayString(SUtility.ConvertMinutesToTime(time*10)) + ", " + time.ToString();
    }

    internal void SetTimeFromMinutes(int time, out int target)
    {
        target = SUtility.ConvertMinutesToTime(time) * 10;
    }


    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        _currentItemIndex = Math.Max(0, Math.Min(_options.Count - ITEMS_PER_PAGE, _currentItemIndex));

        UnsubscribeFromSelectedTextbox();
        for (var index = 0; index < _optionSlots.Count; ++index)
            if (_optionSlots[index].bounds.Contains(x, y) 
                && _currentItemIndex + index < _options.Count 
                && _options[_currentItemIndex + index].bounds.Contains(x - _optionSlots[index].bounds.X, y - _optionSlots[index].bounds.Y))
            {
                _options[_currentItemIndex + index].receiveLeftClick(x - _optionSlots[index].bounds.X, y - _optionSlots[index].bounds.Y);
                _optionsSlotHeld = index;
                break;
            }
    }

    public override void releaseLeftClick(int x, int y)
    {
        if (GameMenu.forcePreventClose)
            return;
        base.releaseLeftClick(x, y);
        if (_optionsSlotHeld != -1 && _optionsSlotHeld + _currentItemIndex < _options.Count)
            _options[_currentItemIndex + _optionsSlotHeld].leftClickReleased(x - _optionSlots[_optionsSlotHeld].bounds.X,
                y - _optionSlots[_optionsSlotHeld].bounds.Y);
        _optionsSlotHeld = -1;
    }

    public override void leftClickHeld(int x, int y)
    {
        if (GameMenu.forcePreventClose)
            return;
        base.leftClickHeld(x, y);
        //if (scrolling)
        //{
        //    var y1 = scrollBar.bounds.Y;
        //    scrollBar.bounds.Y = Math.Min(yPositionOnScreen + height - 64 - 12 - scrollBar.bounds.Height,
        //        Math.Max(y, yPositionOnScreen + upArrow.bounds.Height + 20));
        //    currentItemIndex = Math.Min(options.Count - 7,
        //        Math.Max(0, (int)(options.Count * (double)((y - scrollBarRunner.Y) / (float)scrollBarRunner.Height))));
        //    setScrollBarToCurrentIndex();
        //    var y2 = scrollBar.bounds.Y;
        //    if (y1 == y2)
        //        return;
        //    Game1.playSound("shiny4");
        //}
        //else
        {
            if (_optionsSlotHeld == -1 || _optionsSlotHeld + _currentItemIndex >= _options.Count)
                return;
            _options[_currentItemIndex + _optionsSlotHeld].leftClickHeld(x - _optionSlots[_optionsSlotHeld].bounds.X,
                y - _optionSlots[_optionsSlotHeld].bounds.Y);
        }
    }


    internal bool IsDropdownActive()
    {
        return _optionsSlotHeld != -1 && _optionsSlotHeld + _currentItemIndex < _options.Count &&
               _options[_currentItemIndex + _optionsSlotHeld] is OptionsDropDown;
    }


    internal void UnsubscribeFromSelectedTextbox()
    {
        if (Game1.keyboardDispatcher.Subscriber == null)
            return;
        foreach (var option in _options)
            if (option is OptionsTextEntry && Game1.keyboardDispatcher.Subscriber == (option as OptionsTextEntry).textBox)
            {
                Game1.keyboardDispatcher.Subscriber = null;
                break;
            }
    }


    public override void draw(SpriteBatch spriteBatch)
    {
        base.draw(spriteBatch);

        //Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, false, true);
        for (int index = 0; index < this._optionSlots.Count; ++index)
        {
            if (this._currentItemIndex >= 0 && this._currentItemIndex + index < this._options.Count)
                this._options[this._currentItemIndex + index].draw(spriteBatch, this._optionSlots[index].bounds.X, this._optionSlots[index].bounds.Y);
        }
    }
}
