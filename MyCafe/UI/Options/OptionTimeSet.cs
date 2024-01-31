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
    private readonly Action<int> SetValue;

    private readonly int MinValue;
    private readonly int MaxValue;

    private int Hours;
    private int Minutes;
    private bool Am;

    private Rectangle MinuteUpRec;
    private Rectangle MinuteDownRec;

    public ClickableComponent UpArrow;
    public ClickableComponent DownArrow;

    private static readonly Rectangle SourceTimeArrow = new Rectangle(16, 19, 22, 13);

    public OptionTimeSet(string label, int initialValue, int minValue, int maxValue, Rectangle rec, int optionNumber, Action<int> setFunction, Texture2D sprites)
        : base(label, rec, sprites)
    {
        this.MinValue = minValue;
        this.MaxValue = maxValue;
        this.SetValue = setFunction;

        this.Minutes = initialValue % 100;
        this.Am = initialValue < 1200 || initialValue >= 2400;
        this.Hours = initialValue / 100;

        this.MinuteUpRec = new Rectangle(this.bounds.X, this.bounds.Y + 12, 32, 20);
        this.MinuteDownRec = new Rectangle(this.bounds.X, this.bounds.Y + 12 + 30, 32, 20);

        this.UpArrow = new(this.MinuteUpRec, "uparrow")
        {
            myID = optionNumber,
            downNeighborID = optionNumber + 1,
            leftNeighborID = -7777,
            rightNeighborID = -7777,
            region = optionNumber
        };
        this.DownArrow = new(this.MinuteDownRec, "downarrow")
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
        if (this.MinuteUpRec.Contains(x, y))
        {
            this.MinuteUp();
            this.SetTime();
        }
        else if (this.MinuteDownRec.Contains(x, y))
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
            Game1.dialogueFont, this.FormatHours().ToString().PadLeft(2, '0') + " : " + this.Minutes.ToString().PadLeft(2, '0') + (this.Am ? " am" : " pm"),
            new Vector2(slotX + this.bounds.X + 32, slotY + this.bounds.Y + 14),
            Color.Black);

        b.Draw(this.Sprites, new Rectangle(this.MinuteUpRec.X + slotX, this.MinuteUpRec.Y + slotY, this.MinuteUpRec.Width, this.MinuteUpRec.Height), SourceTimeArrow, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipVertically, 0.9f);
        b.Draw(this.Sprites, new Rectangle(this.MinuteDownRec.X + slotX, this.MinuteDownRec.Y + slotY, this.MinuteUpRec.Width, this.MinuteUpRec.Height), SourceTimeArrow, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.9f);
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
        this.Am = this.Hours < 12 || this.Hours >= 24;
        this.SetValue(this.Hours * 100 + this.Minutes);
    }

    private int FormatHours()
    {
        int h = this.Am ? this.Hours % 12 : this.Hours - 12;
        if (h == 0)
            h = 12;
        return h;
    }

    private void MinuteUp()
    {
        if (this.Minutes >= 50)
        {
            int h = this.Hours;
            this.HourUp();
            if (h == this.Hours || this.Hours * 100 + this.Minutes > this.MaxValue)
                return;
            this.Minutes = 0;
        }
        else
        {
            this.Minutes += 10;
        }
    }

    private void MinuteDown()
    {
        if (this.Minutes < 10)
        {
            int h = this.Hours;
            this.HourDown();
            if (h == this.Hours || this.Hours * 100 + this.Minutes < this.MinValue)
                return;
            this.Minutes = 50;
        }
        else
        {
            this.Minutes -= 10;
        }
    }

    private void HourUp()
    {
        this.Hours = Math.Min(Math.Min(25, this.MaxValue / 100), this.Hours + 1);
    }

    private void HourDown()
    {
        this.Hours = Math.Max(Math.Max(6, this.MinValue / 100), this.Hours - 1);
    }
}
