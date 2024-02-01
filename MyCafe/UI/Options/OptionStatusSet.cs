using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using SUtility = StardewValley.Utility;

namespace MyCafe.UI.Options;
internal class OptionStatusSet : OptionsElementBase
{
    private Rectangle SetButtonBounds;
    private readonly ClickableComponent SetButton;
    private readonly string SetButtonText;
    private readonly Vector2 TextCenter;
    private readonly string UnsetText;
    private readonly string SetText;
    private bool IsSet;
    private readonly Func<Task<bool>> SetFunction;
    private Func<bool> CheckFunction;
    private Task<bool> RunTask = null!;
    private readonly CancellationTokenSource Cancellation = new CancellationTokenSource();

    public OptionStatusSet(string label, string buttonText, string unsetText, string setText, Func<Task<bool>> setFunction, Func<bool> checkFunction, Rectangle rec, int optionNumber) : base(label, new Rectangle(rec.X, rec.Y, rec.Width, rec.Height))
    {
        this.SetText = setText;
        this.UnsetText = unsetText;
        this.SetButtonText = buttonText;
        this.TextCenter = Game1.dialogueFont.MeasureString(this.SetButtonText) / 2f;
        this.SetButtonBounds = new Rectangle(this.bounds.X + Game1.tileSize / 2, this.bounds.Y + Game1.tileSize, (int)this.TextCenter.X * 2 + Game1.tileSize, Game1.tileSize);
        this.SetButton = new ClickableComponent(this.SetButtonBounds, $"set{this.SetButtonText}")
        {
            myID = optionNumber,
            upNeighborID = -99998,
            downNeighborID = -99998
        };
        this.style = Style.OptionLabel;

        this.SetFunction = setFunction;
        this.CheckFunction = checkFunction;
        if (checkFunction.Invoke() == true)
        {
            this.IsSet = true;
        }
    }

    internal override Vector2 Snap(int direction)
    {
        return this.bounds.Center.ToVector2();
    }

    public override void receiveLeftClick(int x, int y)
    {
        if (this.SetButton.containsPoint(x, y))
        {
            if (!this.IsSet)
            {
                Log.Debug("Task starting");
                this.RunTask?.Dispose();
                this.RunTask = this.SetFunction();
                this.RunTask.ContinueWith((task) =>
                {
                    Log.Debug($"Setting the result, it's {task.Result}");
                    this.IsSet = task.Result;
                });
            }
            else
            {
                Log.Debug("Already trying to connect");
            }
        }
    }

    public override void draw(SpriteBatch b, int slotX, int slotY, IClickableMenu? context = null)
    {
        // Label
        // base.draw(b, slotX, slotY, context);
        SUtility.drawTextWithShadow(
            b,
            $"{this.label}: {(this.IsSet ? this.SetText : this.UnsetText)}",
            Game1.dialogueFont,
            new Vector2(slotX + this.bounds.X, slotY + this.bounds.Y),
            (this.IsSet ? Color.LimeGreen : Color.White) * (this.greyedOut ? 0.033f : 1f)
            );

        // Button
        IClickableMenu.drawTextureBox(
            b,
            Game1.mouseCursors,
            new Rectangle(432, 439, 9, 9),
            slotX + this.SetButtonBounds.X,
            slotY + this.SetButtonBounds.Y, this.SetButtonBounds.Width, this.SetButtonBounds.Height,
            Color.White * (this.greyedOut || this.IsSet ? 0.33f : 1f),
            4f, drawShadow: false, 0.9f);

        // Button Text
        SUtility.drawTextWithShadow(
            b, this.SetButtonText,
            Game1.dialogueFont,
            new Vector2(slotX + this.SetButtonBounds.Center.X, slotY + this.SetButtonBounds.Center.Y) - this.TextCenter,
            Game1.textColor * (this.greyedOut || this.IsSet ? 0.33f : 1f),
            1f, 0.91f, -1, -1, 0f);
    }

}
