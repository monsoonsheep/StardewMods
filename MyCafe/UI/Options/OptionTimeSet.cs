using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace MyCafe.UI.Options;

internal class OptionTimeSet : OptionsElementBase
{
    private new readonly string label;

    internal static int numberOfComponents = 2;
    private readonly Action<int> setValue;

    private readonly int minValue;
    private readonly int maxValue;

    private int hours;
    private int minutes;
    private bool am;

    private Vector2 textPos;
    private Rectangle minuteUpRec;
    private Rectangle minuteDownRec;

    public ClickableComponent UpArrow;
    public ClickableComponent DownArrow;

    private Rectangle source_TimeArrow = new Rectangle(16, 19, 22, 13);

    public OptionTimeSet(string label, int initialValue, int minValue, int maxValue, Rectangle rec, int optionNumber, Action<int> setFunction) 
        : base(label, rec)
    {
        this.minValue = minValue;
        this.maxValue = maxValue;
        setValue = setFunction;

        minutes = initialValue % 100;
        am = initialValue < 1200 || initialValue >= 2400;
        hours = initialValue / 100;

        minuteUpRec = new Rectangle(bounds.X, bounds.Y + 12, 32, 20);
        minuteDownRec = new Rectangle(bounds.X, bounds.Y + 12 + 30, 32, 20);

        UpArrow = new(minuteUpRec, "uparrow")
        {
            myID = optionNumber,
            downNeighborID = optionNumber + 1,
            leftNeighborID = -7777,
            rightNeighborID = -7777,
            region = optionNumber
        };
        DownArrow = new(minuteDownRec, "downarrow")
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
        if (minuteUpRec.Contains(x, y))
        {
            MinuteUp();
            SetTime();
        }
        else if (minuteDownRec.Contains(x, y))
        {
            MinuteDown();
            SetTime();
        }
    }

    public override void draw(SpriteBatch b, int slotX, int slotY, IClickableMenu context = null)
    {
        SUtility.drawTextWithShadow(
            b, 
            base.label, 
            Game1.dialogueFont,
            new Vector2((slotX + bounds.X + labelOffset.X), (slotY + bounds.Y + labelOffset.Y)), Game1.textColor);

        b.DrawString(
            Game1.dialogueFont,
            FormatHours().ToString().PadLeft(2, '0') + " : " + minutes.ToString().PadLeft(2, '0') + (am ? " am" : " pm"),
            new Vector2(slotX + bounds.X + 32, slotY + bounds.Y + 14),
            Color.Black);

        b.Draw(Mod.Sprites, new Rectangle(minuteUpRec.X + slotX, minuteUpRec.Y + slotY, minuteUpRec.Width, minuteUpRec.Height), source_TimeArrow, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipVertically, 0.9f);
        b.Draw(Mod.Sprites, new Rectangle(minuteDownRec.X + slotX, minuteDownRec.Y + slotY, minuteUpRec.Width, minuteUpRec.Height), source_TimeArrow, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.9f);
    }

    internal override Vector2 Snap(int direction)
    {
        if (direction == 0)
        {
            if (currentlySnapped == 0)
            {
                currentlySnapped = DownArrow.myID;
                return DownArrow.bounds.Center.ToVector2();
            }

            if (currentlySnapped == DownArrow.myID)
            {
                currentlySnapped = UpArrow.myID;
                return UpArrow.bounds.Center.ToVector2();
            }
        }
        else if (direction == 2)
        {
            if (currentlySnapped == 0)
            {
                currentlySnapped = UpArrow.myID;
                return UpArrow.bounds.Center.ToVector2();
            }

            if (currentlySnapped == UpArrow.myID)
            {
                currentlySnapped = DownArrow.myID;
                return DownArrow.bounds.Center.ToVector2();
            }
        }

        currentlySnapped = 0;
        return Vector2.Zero;
    }

    private void SetTime()
    {
        am = hours < 12 || hours >= 24;
        setValue(hours * 100 + minutes);
    }

    private int FormatHours()
    {
        int h = am ? hours % 12 : hours - 12;
        if (h == 0)
            h = 12;
        return h;
    }

    private void MinuteUp()
    {
        if (minutes >= 50)
        {
            int h = hours;
            HourUp();
            if (h == hours || hours * 100 + minutes > maxValue)
                return;
            minutes = 0;
        }
        else
        {
            minutes += 10;
        }
    }

    private void MinuteDown()
    {
        if (minutes < 10)
        {
            int h = hours;
            HourDown();
            if (h == hours || hours * 100 + minutes < minValue)
                return;
            minutes = 50;
        }
        else
        {
            minutes -= 10;
        }
    }

    private void HourUp()
    {
        hours = Math.Min(Math.Min(25, maxValue / 100), hours + 1);
    }

    private void HourDown()
    {
        hours = Math.Max(Math.Max(6, minValue / 100), hours - 1);
    }
}
