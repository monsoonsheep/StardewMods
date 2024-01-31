using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using SUtility = StardewValley.Utility;

namespace MyCafe.UI.Options;

internal class OptionTimeSet : OptionsElementBase
{
    internal static int NumberOfComponents = 2;
    private readonly Action<int> _setValue;

    private readonly int _minValue;
    private readonly int _maxValue;

    private int _hours;
    private int _minutes;
    private bool _am;

    private Rectangle _minuteUpRec;
    private Rectangle _minuteDownRec;

    public ClickableComponent UpArrow;
    public ClickableComponent DownArrow;

    private static readonly Rectangle source_timeArrow = new Rectangle(16, 19, 22, 13);

    public OptionTimeSet(string label, int initialValue, int minValue, int maxValue, Rectangle rec, int optionNumber, Action<int> setFunction, Texture2D sprites) 
        : base(label, rec, sprites)
    {
        this._minValue = minValue;
        this._maxValue = maxValue;
        _setValue = setFunction;

        _minutes = initialValue % 100;
        _am = initialValue < 1200 || initialValue >= 2400;
        _hours = initialValue / 100;

        _minuteUpRec = new Rectangle(bounds.X, bounds.Y + 12, 32, 20);
        _minuteDownRec = new Rectangle(bounds.X, bounds.Y + 12 + 30, 32, 20);

        UpArrow = new(_minuteUpRec, "uparrow")
        {
            myID = optionNumber,
            downNeighborID = optionNumber + 1,
            leftNeighborID = -7777,
            rightNeighborID = -7777,
            region = optionNumber
        };
        DownArrow = new(_minuteDownRec, "downarrow")
        {
            myID = optionNumber + 1,
            upNeighborID = optionNumber,
            leftNeighborID = -7777,
            rightNeighborID = -7777,
            downNeighborID = -99998,
            region = optionNumber
        };

        labelOffset = new Vector2(0, -40f);
    }

    public override void receiveLeftClick(int x, int y)
    {
        base.receiveLeftClick(x, y);
        if (_minuteUpRec.Contains(x, y))
        {
            MinuteUp();
            SetTime();
        }
        else if (_minuteDownRec.Contains(x, y))
        {
            MinuteDown();
            SetTime();
        }
    }

    public override void draw(SpriteBatch b, int slotX, int slotY, IClickableMenu? context = null)
    {
        SUtility.drawTextWithShadow(
            b, 
            base.label, 
            Game1.dialogueFont,
            new Vector2((slotX + bounds.X + labelOffset.X), (slotY + bounds.Y + labelOffset.Y)), Game1.textColor);

        b.DrawString(
            Game1.dialogueFont,
            FormatHours().ToString().PadLeft(2, '0') + " : " + _minutes.ToString().PadLeft(2, '0') + (_am ? " am" : " pm"),
            new Vector2(slotX + bounds.X + 32, slotY + bounds.Y + 14),
            Color.Black);

        b.Draw(Sprites, new Rectangle(_minuteUpRec.X + slotX, _minuteUpRec.Y + slotY, _minuteUpRec.Width, _minuteUpRec.Height), source_timeArrow, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipVertically, 0.9f);
        b.Draw(Sprites, new Rectangle(_minuteDownRec.X + slotX, _minuteDownRec.Y + slotY, _minuteUpRec.Width, _minuteUpRec.Height), source_timeArrow, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.9f);
    }

    internal override Vector2 Snap(int direction)
    {
        if (direction == 0)
        {
            if (CurrentlySnapped == 0)
            {
                CurrentlySnapped = DownArrow.myID;
                return DownArrow.bounds.Center.ToVector2();
            }

            if (CurrentlySnapped == DownArrow.myID)
            {
                CurrentlySnapped = UpArrow.myID;
                return UpArrow.bounds.Center.ToVector2();
            }
        }
        else if (direction == 2)
        {
            if (CurrentlySnapped == 0)
            {
                CurrentlySnapped = UpArrow.myID;
                return UpArrow.bounds.Center.ToVector2();
            }

            if (CurrentlySnapped == UpArrow.myID)
            {
                CurrentlySnapped = DownArrow.myID;
                return DownArrow.bounds.Center.ToVector2();
            }
        }

        CurrentlySnapped = 0;
        return Vector2.Zero;
    }

    private void SetTime()
    {
        _am = _hours < 12 || _hours >= 24;
        _setValue(_hours * 100 + _minutes);
    }

    private int FormatHours()
    {
        int h = _am ? _hours % 12 : _hours - 12;
        if (h == 0)
            h = 12;
        return h;
    }

    private void MinuteUp()
    {
        if (_minutes >= 50)
        {
            int h = _hours;
            HourUp();
            if (h == _hours || _hours * 100 + _minutes > _maxValue)
                return;
            _minutes = 0;
        }
        else
        {
            _minutes += 10;
        }
    }

    private void MinuteDown()
    {
        if (_minutes < 10)
        {
            int h = _hours;
            HourDown();
            if (h == _hours || _hours * 100 + _minutes < _minValue)
                return;
            _minutes = 50;
        }
        else
        {
            _minutes -= 10;
        }
    }

    private void HourUp()
    {
        _hours = Math.Min(Math.Min(25, _maxValue / 100), _hours + 1);
    }

    private void HourDown()
    {
        _hours = Math.Max(Math.Max(6, _minValue / 100), _hours - 1);
    }
}
