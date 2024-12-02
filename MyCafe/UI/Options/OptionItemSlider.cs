using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace Monsoonsheep.StardewMods.MyCafe.UI.Options;


internal class OptionItemSlider : OptionsElementBase
{
    private readonly string Label;

    private readonly Action<int> SetValue;

    private readonly int MinValue;

    private readonly int MaxValue;

    private int Value;

    private float ValuePosition;

    private int PixelWidth => this.bounds.Width - 10 * Game1.pixelZoom;

    private readonly Func<int, string> FormatFunction;

    public OptionItemSlider(string label, int value, Action<int> setValue, int minValue, int maxValue, int width = 48, Func<int, string>? formatFunction = null)
        : base(label, new Rectangle(32, 32, width * Game1.pixelZoom, 6 * Game1.pixelZoom))
    {
        this.Label = label;
        this.Value = value;

        this.MinValue = minValue;
        this.MaxValue = maxValue;
        this.SetValue = setValue;

        this.ValuePosition = GetRangePosition(this.Value, this.MinValue, this.MaxValue);
        this.FormatFunction = formatFunction ?? ((i) => i.ToString());
    }

    internal override Vector2 Snap(int direction)
    {
        return this.bounds.Center.ToVector2();
    }

    public override void leftClickHeld(int x, int y)
    {
        if (this.greyedOut)
            return;

        base.leftClickHeld(x, y);

        this.ValuePosition = GetRangePosition(x, this.bounds.X, this.bounds.X + this.PixelWidth);
        this.Value = GetValueAtPosition(this.ValuePosition, this.MinValue, this.MaxValue);
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
        if (this.greyedOut)
            return;

        base.receiveLeftClick(x, y);
        this.leftClickHeld(x, y);
    }


    public override void leftClickReleased(int x, int y)
    {
        this.ValuePosition = GetRangePosition(this.Value, this.MinValue, this.MaxValue);
        this.SetValue(this.Value);
    }

    public override void draw(SpriteBatch b, int slotX, int slotY, IClickableMenu? context = null)
    {
        base.label = $"{this.Label}: {this.FormatFunction(this.Value)}";



        int sliderOffsetX = GetValueAtPosition(this.ValuePosition, 0, this.PixelWidth);
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, OptionsSlider.sliderBGSource, slotX + this.bounds.X, slotY + this.bounds.Y, this.bounds.Width, this.bounds.Height, Color.White, Game1.pixelZoom, false);
        b.Draw(Game1.mouseCursors, new Vector2(slotX + this.bounds.X + sliderOffsetX, slotY + this.bounds.Y), OptionsSlider.sliderButtonRect, Color.White, 0.0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 0.9f);

        base.draw(b, slotX, slotY + 12, context);
    }
}
