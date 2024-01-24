using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.GameData.HomeRenovations;
using StardewValley;
using StardewValley.Menus;

namespace MyCafe.UI.Options;
internal class OptionStatusSet : OptionsElement
{
    private Rectangle _setButtonBounds;
    private ClickableComponent _setButton;
    private string _setButtonText;
    private Vector2 _textCenter;
    private string _unsetText;
    private string _setText;
    private bool isSetTaskRunning;
    private bool isSet;
    private Func<Task<bool>> setFunction;
    private Func<bool> checkFunction;
    private Task<bool> runTask;
    private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

    public OptionStatusSet(string label, string buttonText, string unsetText, string setText, Func<Task<bool>> setFunction, Func<bool> checkFunction, Rectangle rec, int optionNumber) : base(label, rec.X, rec.Y, rec.Width, rec.Height)
    {
        _setText = setText;
        _unsetText = unsetText;
        _setButtonText = buttonText;
        _textCenter = Game1.dialogueFont.MeasureString(_setButtonText) / 2f;
        _setButtonBounds = new Rectangle(bounds.X + Game1.tileSize / 2, bounds.Y + Game1.tileSize, (int) _textCenter.X * 2 + Game1.tileSize, Game1.tileSize);
        _setButton = new ClickableComponent(_setButtonBounds, $"set{_setButtonText}")
        {
            myID = optionNumber,
            upNeighborID = -7777,
            downNeighborID = -7777
        };
        style = Style.OptionLabel;

        this.setFunction = setFunction;
        this.checkFunction = checkFunction;
        if (checkFunction != null && checkFunction.Invoke() == true)
        {
            isSet = true;
        }
    }

    public override void receiveLeftClick(int x, int y)
    {
        if (_setButton.containsPoint(x, y))
        {
            if (!isSet)
            {
                Log.Debug("Task starting");
                runTask?.Dispose();
                runTask = setFunction();
                runTask.ContinueWith((task) =>
                {
                    Log.Debug($"Setting the result, it's {task.Result}");
                    isSet = task.Result;
                });
            }
            else
            {
                Log.Debug("Already trying to connect");
            }
        }
    }

    public override void draw(SpriteBatch b, int slotX, int slotY, IClickableMenu context = null)
    {
        // Label
        // base.draw(b, slotX, slotY, context);
        SUtility.drawTextWithShadow(
            b,
            $"{label}: {(isSet ? _setText : _unsetText)}",
            Game1.dialogueFont,
            new Vector2(slotX + bounds.X, slotY + bounds.Y),
            (isSet ? Color.LimeGreen : Color.White) * (greyedOut ? 0.033f : 1f)
            );

        // Button
        IClickableMenu.drawTextureBox(
            b, 
            Game1.mouseCursors, 
            new Rectangle(432, 439, 9, 9), 
            slotX + _setButtonBounds.X, 
            slotY + _setButtonBounds.Y, 
            _setButtonBounds.Width, 
            _setButtonBounds.Height, 
            Color.White * (greyedOut || isSet ? 0.33f : 1f), 
            4f, drawShadow: false, 0.9f);

        // Button Text
        SUtility.drawTextWithShadow(
            b, 
            _setButtonText, 
            Game1.dialogueFont, 
            new Vector2(slotX + _setButtonBounds.Center.X, slotY + _setButtonBounds.Center.Y) - _textCenter, 
            Game1.textColor * (greyedOut || isSet ? 0.33f : 1f), 
            1f, 0.91f, -1, -1, 0f);
    }
}
