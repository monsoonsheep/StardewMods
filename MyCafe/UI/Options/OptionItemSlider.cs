using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace MyCafe.UI.Options;


internal class OptionItemSlider : OptionsElement
{
    private new readonly string label;

    private readonly Action<int> setValue;

    private readonly int minValue;

    private readonly int maxValue;

    private int value;

    private float valuePosition;

    private int PixelWidth => bounds.Width - 10 * Game1.pixelZoom;

    private Func<int, string> formatFunction;

    public OptionItemSlider(string label, int value, Action<int> setValue, int minValue, int maxValue, int width = 48, Func<int, string> formatFunction = null)
        : base(label, 32, 32, width * Game1.pixelZoom, 6 * Game1.pixelZoom)
    {
        this.label = label;
        this.value = value;

        this.minValue = minValue;
        this.maxValue = maxValue;
        this.setValue = setValue;

        valuePosition = GetRangePosition(this.value, this.minValue, this.maxValue);
        this.formatFunction = formatFunction ?? ((i) => i.ToString());
    }


    public override void leftClickHeld(int x, int y)
    {
        if (greyedOut)
            return;

        base.leftClickHeld(x, y);

        valuePosition = GetRangePosition(x, bounds.X, bounds.X + PixelWidth);
        value = GetValueAtPosition(valuePosition, minValue, maxValue);
    }

    public static float GetRangePosition(int value, int minValue, int maxValue)
    {
        float position = (value - minValue) / (float)(maxValue - minValue);
        return MathHelper.Clamp(position, 0, 1);
    }

    public static int GetValueAtPosition(float position, int minValue, int maxValue)
    {
        float value = position * (maxValue - minValue) + minValue;
        return (int)MathHelper.Clamp(value, minValue, maxValue);
    }

    public override void receiveLeftClick(int x, int y)
    {
        if (greyedOut)
            return;

        base.receiveLeftClick(x, y);
        leftClickHeld(x, y);
    }


    public override void leftClickReleased(int x, int y)
    {
        valuePosition = GetRangePosition(value, minValue, maxValue);
        setValue(value);
    }

    public override void draw(SpriteBatch b, int slotX, int slotY, IClickableMenu context = null)
    {
        base.label = $"{label}: {formatFunction(value)}";



        int sliderOffsetX = GetValueAtPosition(valuePosition, 0, PixelWidth);
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, OptionsSlider.sliderBGSource, slotX + bounds.X, slotY + bounds.Y, bounds.Width, bounds.Height, Color.White, Game1.pixelZoom, false);
        b.Draw(Game1.mouseCursors, new Vector2(slotX + bounds.X + sliderOffsetX, slotY + bounds.Y), OptionsSlider.sliderButtonRect, Color.White, 0.0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 0.9f);

        base.draw(b, slotX, slotY + 12, context);
    }
}
