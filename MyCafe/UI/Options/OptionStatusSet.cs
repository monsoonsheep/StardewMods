using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using SUtility = StardewValley.Utility;

namespace MyCafe.UI.Options;
internal class OptionStatusSet : OptionsElement
{
    private Rectangle _setButtonBounds;
    private readonly ClickableComponent _setButton;
    private readonly string _setButtonText;
    private readonly Vector2 _textCenter;
    private readonly string _unsetText;
    private readonly string _setText;
    private bool isSet;
    private readonly Func<Task<bool>> setFunction;
    private Func<bool> checkFunction;
    private Task<bool> runTask = null!;
    private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

    public OptionStatusSet(string label, string buttonText, string unsetText, string setText, Func<Task<bool>> setFunction, Func<bool> checkFunction, Rectangle rec, int optionNumber) : base(label, rec.X, rec.Y, rec.Width, rec.Height)
    {
        this._setText = setText;
        this._unsetText = unsetText;
        this._setButtonText = buttonText;
        this._textCenter = Game1.dialogueFont.MeasureString(this._setButtonText) / 2f;
        this._setButtonBounds = new Rectangle(this.bounds.X + Game1.tileSize / 2, this.bounds.Y + Game1.tileSize, (int)this._textCenter.X * 2 + Game1.tileSize, Game1.tileSize);
        this._setButton = new ClickableComponent(this._setButtonBounds, $"set{this._setButtonText}")
        {
            myID = optionNumber,
            upNeighborID = -99998,
            downNeighborID = -99998
        };
        this.style = Style.OptionLabel;

        this.setFunction = setFunction;
        this.checkFunction = checkFunction;
        if (checkFunction.Invoke() == true)
        {
            this.isSet = true;
        }
    }

    public override void receiveLeftClick(int x, int y)
    {
        if (this._setButton.containsPoint(x, y))
        {
            if (!this.isSet)
            {
                Log.Debug("Task starting");
                this.runTask?.Dispose();
                this.runTask = this.setFunction();
                this.runTask.ContinueWith((task) =>
                {
                    Log.Debug($"Setting the result, it's {task.Result}");
                    this.isSet = task.Result;
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
            $"{this.label}: {(this.isSet ? this._setText : this._unsetText)}",
            Game1.dialogueFont,
            new Vector2(slotX + this.bounds.X, slotY + this.bounds.Y),
            (this.isSet ? Color.LimeGreen : Color.White) * (this.greyedOut ? 0.033f : 1f)
            );

        // Button
        IClickableMenu.drawTextureBox(
            b,
            Game1.mouseCursors,
            new Rectangle(432, 439, 9, 9),
            slotX + this._setButtonBounds.X,
            slotY + this._setButtonBounds.Y, this._setButtonBounds.Width, this._setButtonBounds.Height,
            Color.White * (this.greyedOut || this.isSet ? 0.33f : 1f),
            4f, drawShadow: false, 0.9f);

        // Button Text
        SUtility.drawTextWithShadow(
            b, this._setButtonText,
            Game1.dialogueFont,
            new Vector2(slotX + this._setButtonBounds.Center.X, slotY + this._setButtonBounds.Center.Y) - this._textCenter,
            Game1.textColor * (this.greyedOut || this.isSet ? 0.33f : 1f),
            1f, 0.91f, -1, -1, 0f);
    }
}
