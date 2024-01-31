using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.UI.Options;
using StardewValley;
using StardewValley.Menus;

namespace MyCafe.UI;
internal abstract class OptionsPageBase : MenuPageBase
{
    protected int OptionSlotsCount = 3;
    public readonly List<ClickableComponent> OptionSlots = new();
    protected readonly List<OptionsElementBase> Options = new();
    protected readonly Rectangle OptionSlotSize;
    protected int OptionSlotHeld = -1;

    public readonly ClickableTextureComponent _upArrow;
    public readonly ClickableTextureComponent _downArrow;
    public readonly ClickableTextureComponent _scrollBar;
    private bool _scrolling;
    private readonly Rectangle _scrollBarRunner;
    protected int CurrentItemIndex = 0;

    protected OptionsPageBase(string name, Rectangle bounds, CafeMenu parentMenu, Texture2D sprites) : base(name, bounds, parentMenu, sprites)
    {
        this._upArrow = new ClickableTextureComponent(
            new Rectangle(bounds.Right - 30, bounds.Y + 101, 44, 48),
            Game1.mouseCursors,
            new Rectangle(421, 459, 11, 12),
            4f);
        this._downArrow = new ClickableTextureComponent(
            new Rectangle(this._upArrow.bounds.X, bounds.Y + bounds.Height - 48 - 2, 44, 48),
            Game1.mouseCursors,
            new Rectangle(421, 472, 11, 12),
            4f);
        this._scrollBar = new ClickableTextureComponent(
            new Rectangle(this._upArrow.bounds.X + 12, this._upArrow.bounds.Y + this._upArrow.bounds.Height + 4, 24, 40),
            Game1.mouseCursors,
            new Rectangle(435, 463, 6, 10),
            4f);
        this._scrollBarRunner = new Rectangle(this._scrollBar.bounds.X, this._upArrow.bounds.Bottom + 4, this._scrollBar.bounds.Width,
            (this._downArrow.bounds.Top - this._upArrow.bounds.Bottom) - 4);


        for (int i = 0; i < this.OptionSlotsCount; i++)
            this.OptionSlots.Add(new ClickableComponent(
                new Rectangle(this.Bounds.X + Game1.tileSize / 4, this.Bounds.Y + Game1.tileSize + i * (this.Bounds.Height / this.OptionSlotsCount), this.Bounds.Width - Game1.tileSize / 2,
                    (this.Bounds.Height - 32) / this.OptionSlotsCount),
                i.ToString())
            {
                region = 41410,
                myID = 41410 + i,
                downNeighborID = -7777,
                upNeighborID = -7777,
                leftNeighborID = -7777,
                rightNeighborID = -7777,
                upNeighborImmutable = true,
                downNeighborImmutable = true,
                fullyImmutable = true
            });
        // Config

        this.OptionSlotSize = new Rectangle(0, 0, this.Bounds.Width - Game1.tileSize / 4,
            (this.Bounds.Height) / this.OptionSlotsCount);

        this.DefaultComponent = 41410;
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        for (int i = 0; i < this.OptionSlots.Count; ++i)
            if (this.OptionSlots[i].bounds.Contains(x, y)
                && this.CurrentItemIndex + i < this.Options.Count
                && this.Options[this.CurrentItemIndex + i].bounds.Contains(x - this.OptionSlots[i].bounds.X, y - this.OptionSlots[i].bounds.Y))
            {
                this.Options[this.CurrentItemIndex + i].receiveLeftClick(x - this.OptionSlots[i].bounds.X, y - this.OptionSlots[i].bounds.Y);
                return;
            }

        if (this.Options.Count > this.OptionSlotsCount)
        {
            if (this._downArrow.containsPoint(x, y) && this.CurrentItemIndex < Math.Max(0, this.Options.Count - this.OptionSlotsCount))
            {
                this.DownArrowPressed();
                Game1.playSound("shwip");
            }
            else if (this._upArrow.containsPoint(x, y) && this.CurrentItemIndex > 0)
            {
                this.UpArrowPressed();
                Game1.playSound("shwip");
            }
            else if (this._scrollBar.containsPoint(x, y))
            {
                this._scrolling = true;
            }
            else if (!this._downArrow.containsPoint(x, y))
            {
                this._scrolling = true;
                this.leftClickHeld(x, y);
                this.releaseLeftClick(x, y);
            }
        }
    }

    public override void leftClickHeld(int x, int y)
    {
        if (GameMenu.forcePreventClose)
            return;

        base.leftClickHeld(x, y);
        if (this._scrolling)
        {
            int oldY = this._scrollBar.bounds.Y;
            this._scrollBar.bounds.Y = Math.Min(this.Bounds.Y + this.Bounds.Height - 64 - 12 - this._scrollBar.bounds.Height, Math.Max(y, this.Bounds.Y + this._upArrow.bounds.Height + 20));
            float percentage = (y - this._scrollBarRunner.Y) / (float)this._scrollBarRunner.Height;
            this.CurrentItemIndex = Math.Min(this.Options.Count - 7, Math.Max(0, (int)(this.Options.Count * percentage)));
            this.SetScrollBarToCurrentIndex();

            if (oldY != this._scrollBar.bounds.Y)
                Game1.playSound("shiny4");

            return;
        }

        if (this.OptionSlotHeld != -1)
        {
            this.Options[this.CurrentItemIndex + this.OptionSlotHeld].leftClickHeld(x - this.OptionSlots[this.OptionSlotHeld].bounds.X, y - this.OptionSlots[this.OptionSlotHeld].bounds.Y);
        }
    }

    public override void releaseLeftClick(int x, int y)
    {
        if (GameMenu.forcePreventClose)
            return;

        base.releaseLeftClick(x, y);
        if (this.OptionSlotHeld != -1 && this.CurrentItemIndex + this.OptionSlotHeld < this.Options.Count)
        {
            this.Options[this.CurrentItemIndex + this.OptionSlotHeld].leftClickReleased(x - this.OptionSlots[this.OptionSlotHeld].bounds.X, y - this.OptionSlots[this.OptionSlotHeld].bounds.Y);
        }

        this.OptionSlotHeld = -1;
        this._scrolling = false;
    }

    protected override void customSnapBehavior(int direction, int oldRegion, int oldID)
    {
        var currentSlot = this.getComponentWithID(oldID);
        var option = this.Options[this.CurrentItemIndex + this.OptionSlots.IndexOf(currentSlot)];

        Vector2 positionForSnap = option.Snap(direction);
        if (positionForSnap != Vector2.Zero)
        {
            Game1.setMousePosition((int)(currentSlot.bounds.X + positionForSnap.X), (int)(currentSlot.bounds.Y + positionForSnap.Y), ui_scale: false);
            return;
        }

        switch (direction)
        {
            case 0:
                this.setCurrentlySnappedComponentTo(currentSlot.myID - 1);
                break;
            case 1:
                this.currentlySnappedComponent = null;
                break;
            case 2:
                this.setCurrentlySnappedComponentTo(currentSlot.myID + 1);
                break;
            case 3:
                this.currentlySnappedComponent = null;
                break;
        }

        if (this.currentlySnappedComponent == null)
        {
            if (direction is 1 or 3)
                this.SnapOut(3);
            else if (direction is 0) this.setCurrentlySnappedComponentTo(12340);
        }
        else
        {
            int i = this.CurrentItemIndex + this.OptionSlots.IndexOf(this.currentlySnappedComponent);
            if (i >= this.Options.Count)
            {
                this.setCurrentlySnappedComponentTo(12340);
            }
            else
            {
                var o = this.Options[this.CurrentItemIndex + this.OptionSlots.IndexOf(this.currentlySnappedComponent)];

                Vector2 p = o.Snap(direction);
                if (p != Vector2.Zero)
                {
                    Log.Info("snapping mouse to option subslot again");
                    Game1.setMousePosition((int)(this.currentlySnappedComponent.bounds.X + p.X), (int)(this.currentlySnappedComponent.bounds.Y + p.Y), ui_scale: false);
                    return;
                }
            }

        }
    }

    public override void snapCursorToCurrentSnappedComponent()
    {
        if (this.currentlySnappedComponent?.region != 41410)
        {
            base.snapCursorToCurrentSnappedComponent();
        }
    }

    public override void snapToDefaultClickableComponent()
    {
        base.snapToDefaultClickableComponent();
        var currentSlot = this.currentlySnappedComponent;
        if (currentSlot != null && this.OptionSlots.Contains(currentSlot))
        {
            var option = this.Options[this.CurrentItemIndex + this.OptionSlots.IndexOf(currentSlot)];
            Vector2 positionForSnap = option.Snap(2);
            if (positionForSnap != Vector2.Zero)
            {
                Game1.setMousePosition((int)(currentSlot.bounds.X + positionForSnap.X), (int)(currentSlot.bounds.Y + positionForSnap.Y), ui_scale: true);
            }
        }
    }

    public override void draw(SpriteBatch b)
    {
        // Options
        for (int i = 0; i < this.OptionSlots.Count; i++)
        {
            if (this.CurrentItemIndex + i < this.Options.Count) this.Options[this.CurrentItemIndex + i].draw(b, this.OptionSlots[i].bounds.X, this.OptionSlots[i].bounds.Y);
        }
    }


    private void DownArrowPressed()
    {
        this.CurrentItemIndex++;
        this.SetScrollBarToCurrentIndex();
    }

    private void UpArrowPressed()
    {
        this.CurrentItemIndex--;
        this.SetScrollBarToCurrentIndex();
    }

    private void SetScrollBarToCurrentIndex()
    {
        if (this.Options.Count > 0)
        {
            this._scrollBar.bounds.Y = this._upArrow.bounds.Bottom + this._scrollBarRunner.Height / Math.Max(1, this.Options.Count - this.OptionSlotsCount + 1) * this.CurrentItemIndex + 4;
            if (this._scrollBar.bounds.Y > this._downArrow.bounds.Y - this._scrollBar.bounds.Height - 4)
            {
                this._scrollBar.bounds.Y = this._downArrow.bounds.Y - this._scrollBar.bounds.Height - 4;
            }
        }
    }
}
