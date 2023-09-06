using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace FarmCafe.Framework.UI
{
    internal class OptionTimeSet : OptionsElement
    {
        private new readonly string label;

        private readonly Action<int> setValue;

        private readonly int minValue;
        private readonly int maxValue;

        private int hours;
        private int minutes;
        private bool am;

        private float valuePosition;

        private Rectangle hourUpRec;
        private Rectangle hourDownRec;
        private Rectangle minuteUpRec;
        private Rectangle minuteDownRec;

        public OptionTimeSet(string label, int initialValue, int minValue, int maxValue, Action<int> setFunction) : base(label, 32, 32, 96 * Game1.pixelZoom,
            160 * Game1.pixelZoom)
        {
            this.minValue = minValue;
            this.maxValue = maxValue;
            this.setValue = setFunction;
            
            this.minutes = initialValue % 100;
            this.am = (initialValue < 1200 || initialValue >= 2400);
            this.hours = initialValue / 100;

            hourUpRec = new Rectangle(bounds.X + 5, bounds.Y, 40, 22);
            hourDownRec = new Rectangle(bounds.X + 5, bounds.Y + 52, 40, 22);
            minuteUpRec = new Rectangle(bounds.X + 72, bounds.Y, 40, 22);
            minuteDownRec = new Rectangle(bounds.X + 72, bounds.Y + 52, 40, 22);

        }

        private void setTime()
        {
            am = (hours < 12 || hours >= 24);
            setValue(hours * 100 + minutes);
        }

        private int formatHours()
        {
            int h = (am) ? (hours % 12) : (hours - 12);
            if (h == 0)
                h = 12;
            return h;
        }
        public override void receiveLeftClick(int x, int y)
        {
            base.receiveLeftClick(x, y);
            if (hourUpRec.Contains(x, y))
            {
                hourUp();
                setTime();
            }
            else if (hourDownRec.Contains(x, y))
            {
                hourDown();
                setTime();
            }
            else if (minuteUpRec.Contains(x, y))
            {
                minuteUp();
                setTime();
            }
            else if (minuteDownRec.Contains(x, y))
            {
                minuteDown();
                setTime();
            }
        }

        private void minuteUp()
        {
            if (minutes >= 50)
            {
                int h = hours;
                hourUp();
                if (h == hours || hours * 100 + minutes > maxValue)
                    return;
                minutes = 0;
            }
            else
            {
                minutes += 10;
            }
        }

        private void minuteDown()
        {
            if (minutes < 10)
            {
                int h = hours;
                hourDown();
                if (h == hours || hours * 100 + minutes < minValue)
                    return;
                minutes = 50;
            }
            else
            {
                minutes -= 10;
            }
        }

        private void hourUp()
        {
            hours = Math.Min(Math.Min(25, maxValue/100), hours + 1);
        }

        private void hourDown()
        {
            hours = Math.Max(Math.Max(6, minValue/100), hours - 1);
        }
        public override void draw(SpriteBatch b, int slotX, int slotY, IClickableMenu? context = null)
        {
            base.draw(b, slotX + 200, slotY , context);
            b.DrawString(Game1.dialogueFont, formatHours().ToString().PadLeft(2, '0') + " : " + this.minutes.ToString().PadLeft(2, '0') + ((am) ? " am" : " pm"), new Vector2(slotX + this.bounds.X, slotY + this.bounds.Y + 14), Color.Black);


            b.Draw(Game1.mouseCursors, new Rectangle(this.bounds.X + slotX + 5, this.bounds.Y + slotY, 40, 22), new Rectangle(422, 459, 10, 11), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.9f);

            b.Draw(Game1.mouseCursors, new Rectangle(this.bounds.X + slotX + 5, this.bounds.Y + slotY + 52, 40, 22), new Rectangle(422, 472, 10, 11), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.9f);
            b.Draw(Game1.mouseCursors, new Rectangle(this.bounds.X + slotX + 72, this.bounds.Y + slotY, 40, 22), new Rectangle(422, 459, 10, 11), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.9f);
            b.Draw(Game1.mouseCursors, new Rectangle(this.bounds.X + slotX + 72, this.bounds.Y + slotY + 52, 40, 22), new Rectangle(422, 472, 10, 11), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.9f);

            //new Rectangle(422, 472, 10, 11)
        }
    }
}
