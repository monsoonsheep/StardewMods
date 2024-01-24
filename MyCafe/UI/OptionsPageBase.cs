using Microsoft.Xna.Framework.Graphics;
using MyCafe.UI.Options;
using StardewValley.Menus;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

    protected OptionsPageBase(string name, Rectangle bounds, CafeMenu parentMenu) : base(name, bounds, parentMenu)
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

    public override void populateClickableComponentList()
    {
        base.populateClickableComponentList();
        foreach (var f in _options.SelectMany(op => op.GetType().GetFields()))
        {
            if (f.FieldType.IsSubclassOf(typeof(ClickableComponent)) || f.FieldType == typeof(ClickableComponent))
            {
                if (f.GetValue(this) != null)
                {
                    allClickableComponents.Add((ClickableComponent)f.GetValue(this));
                }
            }
            else if (f.FieldType == typeof(List<ClickableComponent>))
            {
                List<ClickableComponent> components = (List<ClickableComponent>) f.GetValue(this);
                if (components == null)
                    continue;
                foreach (var c in components)
                {
                    if (c != null)
                        allClickableComponents.Add(c);
                }
            }
        }
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
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

    public override void leftClickHeld(int x, int y)
    {
        if (_optionSlotHeld != -1)
        {
            _options[_currentItemIndex + _optionSlotHeld].leftClickHeld(x - _optionSlots[_optionSlotHeld].bounds.X, y - _optionSlots[_optionSlotHeld].bounds.Y);
        }
    }

    public override void releaseLeftClick(int x, int y)
    {
        if (_optionSlotHeld != -1 && _currentItemIndex + _optionSlotHeld < _options.Count)
        {
            _options[_currentItemIndex + _optionSlotHeld].leftClickReleased(x - _optionSlots[_optionSlotHeld].bounds.X, y - _optionSlots[_optionSlotHeld].bounds.Y);
        }
        _optionSlotHeld = -1;
    }

    public override void draw(SpriteBatch b)
    {
        // Options
        for (int i = 0; i < this._optionSlots.Count; i++)
        {
            if (_currentItemIndex + i < _options.Count)
                _options[_currentItemIndex + i].draw(b, _optionSlots[i].bounds.X, _optionSlots[i].bounds.Y);
        }
    }

}
