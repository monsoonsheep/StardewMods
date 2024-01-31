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
        this._setValue = setFunction;

        this._minutes = initialValue % 100;
        this._am = initialValue < 1200 || initialValue >= 2400;
        this._hours = initialValue / 100;

        this._minuteUpRec = new Rectangle(this.bounds.X, this.bounds.Y + 12, 32, 20);
        this._minuteDownRec = new Rectangle(this.bounds.X, this.bounds.Y + 12 + 30, 32, 20);

        this.UpArrow = new(this._minuteUpRec, "uparrow")
        {
            myID = optionNumber,
            downNeighborID = optionNumber + 1,
            leftNeighborID = -7777,
            rightNeighborID = -7777,
            region = optionNumber
        };
        this.DownArrow = new(this._minuteDownRec, "downarrow")
        {
            myID = optionNumber + 1,
            upNeighborID = optionNumber,
            leftNeighborID = -7777,
            rightNeighborID = -7777,
            downNeighborID = -99998,
            region = optionNumber
        };

        this.labelOffset = new Vector2(0, -40f);
    }

    public override void receiveLeftClick(int x, int y)
    {
        base.receiveLeftClick(x, y);
        if (this._minuteUpRec.Contains(x, y))
        {
            this.MinuteUp();
            this.SetTime();
        }
        else if (this._minuteDownRec.Contains(x, y))
        {
            this.MinuteDown();
            this.SetTime();
        }
    }

    public override void draw(SpriteBatch b, int slotX, int slotY, IClickableMenu? context = null)
    {
        SUtility.drawTextWithShadow(
            b,
            this.label,
            Game1.dialogueFont,
            new Vector2((slotX + this.bounds.X + this.labelOffset.X), (slotY + this.bounds.Y + this.labelOffset.Y)), Game1.textColor);

        b.DrawString(
            Game1.dialogueFont, this.FormatHours().ToString().PadLeft(2, '0') + " : " + this._minutes.ToString().PadLeft(2, '0') + (this._am ? " am" : " pm"),
            new Vector2(slotX + this.bounds.X + 32, slotY + this.bounds.Y + 14),
            Color.Black);

        b.Draw(this.Sprites, new Rectangle(this._minuteUpRec.X + slotX, this._minuteUpRec.Y + slotY, this._minuteUpRec.Width, this._minuteUpRec.Height), source_timeArrow, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipVertically, 0.9f);
        b.Draw(this.Sprites, new Rectangle(this._minuteDownRec.X + slotX, this._minuteDownRec.Y + slotY, this._minuteUpRec.Width, this._minuteUpRec.Height), source_timeArrow, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.9f);
    }

    internal override Vector2 Snap(int direction)
    {
        if (direction == 0)
        {
            if (this.CurrentlySnapped == 0)
            {
                this.CurrentlySnapped = this.DownArrow.myID;
                return this.DownArrow.bounds.Center.ToVector2();
            }

            if (this.CurrentlySnapped == this.DownArrow.myID)
            {
                this.CurrentlySnapped = this.UpArrow.myID;
                return this.UpArrow.bounds.Center.ToVector2();
            }
        }
        else if (direction == 2)
        {
            if (this.CurrentlySnapped == 0)
            {
                this.CurrentlySnapped = this.UpArrow.myID;
                return this.UpArrow.bounds.Center.ToVector2();
            }

            if (this.CurrentlySnapped == this.UpArrow.myID)
            {
                this.CurrentlySnapped = this.DownArrow.myID;
                return this.DownArrow.bounds.Center.ToVector2();
            }
        }

        this.CurrentlySnapped = 0;
        return Vector2.Zero;
    }

    private void SetTime()
    {
        this._am = this._hours < 12 || this._hours >= 24;
        this._setValue(this._hours * 100 + this._minutes);
    }

    private int FormatHours()
    {
        int h = this._am ? this._hours % 12 : this._hours - 12;
        if (h == 0)
            h = 12;
        return h;
    }

    private void MinuteUp()
    {
        if (this._minutes >= 50)
        {
            int h = this._hours;
            this.HourUp();
            if (h == this._hours || this._hours * 100 + this._minutes > this._maxValue)
                return;
            this._minutes = 0;
        }
        else
        {
            this._minutes += 10;
        }
    }

    private void MinuteDown()
    {
        if (this._minutes < 10)
        {
            int h = this._hours;
            this.HourDown();
            if (h == this._hours || this._hours * 100 + this._minutes < this._minValue)
                return;
            this._minutes = 50;
        }
        else
        {
            this._minutes -= 10;
        }
    }

    private void HourUp()
    {
        this._hours = Math.Min(Math.Min(25, this._maxValue / 100), this._hours + 1);
    }

    private void HourDown()
    {
        this._hours = Math.Max(Math.Max(6, this._minValue / 100), this._hours - 1);
    }
}
