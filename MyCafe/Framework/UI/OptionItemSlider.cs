namespace VisitorFramework.Framework.UI;

/*
internal class OptionItemSlider : OptionsElement
{
    private new readonly string label;

    private readonly Action<int> setValue;

    private readonly int minValue;

    private readonly int maxValue;

    private int value;

    private float valuePosition;

    private int PixelWidth => this.bounds.Width - 10 * Game1.pixelZoom;

    private Func<int, string> formatFunction;

    public OptionItemSlider(string label, int value, Action<int> setValue, int minValue, int maxValue,  int width = 48, Func<int, string> formatFunction = null)
        : base(label, 32, 32, width * Game1.pixelZoom, 6 * Game1.pixelZoom)
    {
        this.label = label;
        this.value = value;

        this.minValue = minValue;
        this.maxValue = maxValue;
        this.setValue = setValue;

        this.valuePosition = GetRangePosition(this.value, this.minValue, this.maxValue);
        this.formatFunction = formatFunction ?? ((i) => i.ToString());
    }


    public override void leftClickHeld(int x, int y)
    {
        if (this.greyedOut)
            return;

        base.leftClickHeld(x, y);

        this.valuePosition = GetRangePosition(x, this.bounds.X, this.bounds.X + this.PixelWidth);
        this.value = GetValueAtPosition(this.valuePosition, this.minValue, this.maxValue);
    }

    public static float GetRangePosition(int value, int minValue, int maxValue)
    {
        float position = (value - minValue) / (float)(maxValue - minValue);
        return MathHelper.Clamp(position, 0, 1);
    }

    public static int GetValueAtPosition(float position, int minValue, int maxValue)
    {
        float value = position * (maxValue - minValue) + minValue;
        return (int) MathHelper.Clamp(value, minValue, maxValue);
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
        this.valuePosition = GetRangePosition(this.value, this.minValue, this.maxValue);
        this.setValue(this.value);
    }

    public override void draw(SpriteBatch b, int slotX, int slotY, IClickableMenu? context = null)
    {
        base.label = $"{this.label}: {formatFunction(this.value)}";



        int sliderOffsetX = GetValueAtPosition(this.valuePosition, 0, this.PixelWidth);
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, OptionsSlider.sliderBGSource, slotX + this.bounds.X, slotY + this.bounds.Y, this.bounds.Width, this.bounds.Height, Color.White, Game1.pixelZoom, false);
        b.Draw(Game1.mouseCursors, new Vector2(slotX + this.bounds.X + sliderOffsetX, slotY + this.bounds.Y), OptionsSlider.sliderButtonRect, Color.White, 0.0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 0.9f);

        base.draw(b, slotX, slotY + 12, context);
    }
}
*/