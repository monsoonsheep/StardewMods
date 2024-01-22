using Microsoft.Xna.Framework.Graphics;
using MyCafe.UI.Options;
using StardewValley.Menus;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace MyCafe.UI;
internal abstract class OptionsPageBase : MenuPageBase
{
    protected int _optionSlotsCount = 3;
    protected readonly List<ClickableComponent> _optionSlots = new();
    protected readonly List<OptionsElement> _options = new();
    protected readonly Rectangle _optionSlotSize;
    protected int _optionSlotHeld = -1;
    protected int _currentItemIndex = 0;

    protected OptionsPageBase(string name, CafeMenu parentMenu) : base(name, parentMenu)
    {
        // Config
        for (int i = 0; i < _optionSlotsCount; i++)
            _optionSlots.Add(new ClickableComponent(
                new Rectangle(
                    Bounds.X + Game1.tileSize / 4,
                    Bounds.Y + Game1.tileSize / 2 + i * (Bounds.Height / _optionSlotsCount),
                    Bounds.Width - Game1.tileSize / 2,
                    (Bounds.Height - 32) / _optionSlotsCount),
                i.ToString()));

        _optionSlotSize = new Rectangle(0, 0, Bounds.Width - Game1.tileSize / 4,
            (Bounds.Height) / _optionSlotsCount);
    }

    internal override void LeftClick(int x, int y)
    {
        for (int i = 0; i < _optionSlots.Count; ++i)
            if (_optionSlots[i].bounds.Contains(x, y) 
                && _currentItemIndex +  i < _options.Count 
                && _options[_currentItemIndex + i].bounds.Contains(x - _optionSlots[i].bounds.X, y - _optionSlots[i].bounds.Y))
            {
                _options[_currentItemIndex + i].receiveLeftClick(x - _optionSlots[i].bounds.X, y - _optionSlots[i].bounds.Y);
                return;
            }
    }

    internal override void LeftClickHeld(int x, int y)
    {
        if (_optionSlotHeld != -1)
        {
            _options[_currentItemIndex + _optionSlotHeld].leftClickHeld(x - _optionSlots[_optionSlotHeld].bounds.X, y - _optionSlots[_optionSlotHeld].bounds.Y);
        }
    }

    internal override void ReleaseLeftClick(int x, int y)
    {
        if (_optionSlotHeld != -1 && _currentItemIndex + _optionSlotHeld < _options.Count)
        {
            _options[_currentItemIndex + _optionSlotHeld].leftClickReleased(x - _optionSlots[_optionSlotHeld].bounds.X, y - _optionSlots[_optionSlotHeld].bounds.Y);
        }
        _optionSlotHeld = -1;
    }

    internal override void ScrollWheelAction(int direction)
    {

    }

    internal override void HoverAction(int x, int y)
    {
        
    }

    internal override void Draw(SpriteBatch b)
    {
        // Options
        for (int i = 0; i < this._optionSlots.Count; i++)
        {
            if (_currentItemIndex + i < _options.Count)
                _options[_currentItemIndex + i].draw(b, _optionSlots[i].bounds.X, _optionSlots[i].bounds.Y);
        }
    }

}
