using Microsoft.Xna.Framework.Graphics;
using MyCafe.UI.Options;
using StardewValley.Menus;
using StardewValley;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

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
        _upArrow = new ClickableTextureComponent(
            new Rectangle(bounds.Right - 30, bounds.Y + 101, 44, 48), 
            Game1.mouseCursors, 
            new Rectangle(421, 459, 11, 12), 
            4f);
        _downArrow = new ClickableTextureComponent(
            new Rectangle(_upArrow.bounds.X, bounds.Y + bounds.Height - 48 - 2, 44, 48), 
            Game1.mouseCursors, 
            new Rectangle(421, 472, 11, 12),
            4f);
        _scrollBar = new ClickableTextureComponent(
            new Rectangle(_upArrow.bounds.X + 12, _upArrow.bounds.Y + _upArrow.bounds.Height + 4, 24, 40), 
            Game1.mouseCursors, 
            new Rectangle(435, 463, 6, 10), 
            4f);
        _scrollBarRunner = new Rectangle(
            _scrollBar.bounds.X, 
            _upArrow.bounds.Bottom + 4, 
            _scrollBar.bounds.Width, 
            (_downArrow.bounds.Top - _upArrow.bounds.Bottom) - 4);


        for (int i = 0; i < OptionSlotsCount; i++)
            OptionSlots.Add(new ClickableComponent(
                new Rectangle(
                    Bounds.X + Game1.tileSize / 4,
                    Bounds.Y + Game1.tileSize + i * (Bounds.Height / OptionSlotsCount),
                    Bounds.Width - Game1.tileSize / 2,
                    (Bounds.Height - 32) / OptionSlotsCount),
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

        OptionSlotSize = new Rectangle(0, 0, Bounds.Width - Game1.tileSize / 4,
            (Bounds.Height) / OptionSlotsCount);

        DefaultComponent = 41410;
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        for (int i = 0; i < OptionSlots.Count; ++i)
            if (OptionSlots[i].bounds.Contains(x, y) 
                && CurrentItemIndex +  i < Options.Count 
                && Options[CurrentItemIndex + i].bounds.Contains(x - OptionSlots[i].bounds.X, y - OptionSlots[i].bounds.Y))
            {
                Options[CurrentItemIndex + i].receiveLeftClick(x - OptionSlots[i].bounds.X, y - OptionSlots[i].bounds.Y);
                return;
            }
        
        if (Options.Count > OptionSlotsCount)
        {
            if (_downArrow.containsPoint(x, y) && CurrentItemIndex < Math.Max(0, Options.Count - OptionSlotsCount))
            {
                DownArrowPressed();
                Game1.playSound("shwip");
            }
            else if (_upArrow.containsPoint(x, y) && CurrentItemIndex > 0)
            {
                UpArrowPressed();
                Game1.playSound("shwip");
            }
            else if (_scrollBar.containsPoint(x, y))
            {
                _scrolling = true;
            }
            else if (!_downArrow.containsPoint(x, y))
            {
                _scrolling = true;
                leftClickHeld(x, y);
                releaseLeftClick(x, y);
            }
        }
    }

    public override void leftClickHeld(int x, int y)
    {
        if (GameMenu.forcePreventClose)
            return;

        base.leftClickHeld(x, y);
        if (_scrolling)
        {
            int oldY = _scrollBar.bounds.Y;
            _scrollBar.bounds.Y = Math.Min(Bounds.Y + Bounds.Height - 64 - 12 - _scrollBar.bounds.Height, Math.Max(y, Bounds.Y + _upArrow.bounds.Height + 20));
            float percentage = (y - _scrollBarRunner.Y) / (float) _scrollBarRunner.Height;
            CurrentItemIndex = Math.Min(Options.Count - 7, Math.Max(0, (int)(Options.Count * percentage)));
            SetScrollBarToCurrentIndex();

            if (oldY != _scrollBar.bounds.Y)
                Game1.playSound("shiny4");

            return;
        }

        if (OptionSlotHeld != -1)
        {
            Options[CurrentItemIndex + OptionSlotHeld].leftClickHeld(x - OptionSlots[OptionSlotHeld].bounds.X, y - OptionSlots[OptionSlotHeld].bounds.Y);
        }
    }

    public override void releaseLeftClick(int x, int y)
    {
        if (GameMenu.forcePreventClose) 
            return;

        base.releaseLeftClick(x, y);
        if (OptionSlotHeld != -1 && CurrentItemIndex + OptionSlotHeld < Options.Count)
        {
            Options[CurrentItemIndex + OptionSlotHeld].leftClickReleased(x - OptionSlots[OptionSlotHeld].bounds.X, y - OptionSlots[OptionSlotHeld].bounds.Y);
        }
        OptionSlotHeld = -1;
        _scrolling = false;
    }

    protected override void customSnapBehavior(int direction, int oldRegion, int oldID)
    {
        var currentSlot = getComponentWithID(oldID);
        var option = Options[CurrentItemIndex + OptionSlots.IndexOf(currentSlot)];

        Vector2 positionForSnap = option.Snap(direction);
        if (positionForSnap != Vector2.Zero)
        {
            Game1.setMousePosition((int) (currentSlot.bounds.X + positionForSnap.X), (int) (currentSlot.bounds.Y + positionForSnap.Y), ui_scale: false);
            return;
        }

        switch (direction)
        {
            case 0:
                setCurrentlySnappedComponentTo(currentSlot.myID - 1);
                break;
            case 1:
                currentlySnappedComponent = null;
                break;
            case 2:
                setCurrentlySnappedComponentTo(currentSlot.myID + 1);
                break;
            case 3:
                currentlySnappedComponent = null;
                break;
        }

        if (currentlySnappedComponent == null)
        {
            if (direction is 1 or 3)
                SnapOut(3);
            else if (direction is 0)
                setCurrentlySnappedComponentTo(12340);
        }
        else
        {
            int i = CurrentItemIndex + OptionSlots.IndexOf(currentlySnappedComponent);
            if (i >= Options.Count)
            {
                setCurrentlySnappedComponentTo(12340);
            }
            else
            {
                var o = Options[CurrentItemIndex + OptionSlots.IndexOf(currentlySnappedComponent)];

                Vector2 p = o.Snap(direction);
                if (p != Vector2.Zero)
                {
                    Log.Info("snapping mouse to option subslot again");
                    Game1.setMousePosition((int) (currentlySnappedComponent.bounds.X + p.X), (int) (currentlySnappedComponent.bounds.Y + p.Y), ui_scale: false);
                    return;
                }
            }
            
        }
    }

    public override void snapCursorToCurrentSnappedComponent()
    {
        if (currentlySnappedComponent?.region != 41410)
        {
            base.snapCursorToCurrentSnappedComponent();
        }
    }

    public override void snapToDefaultClickableComponent()
    {
        base.snapToDefaultClickableComponent();
        var currentSlot = currentlySnappedComponent;
        if (currentSlot != null && OptionSlots.Contains(currentSlot))
        {
            var option = Options[CurrentItemIndex + OptionSlots.IndexOf(currentSlot)];
            Vector2 positionForSnap = option.Snap(2);
            if (positionForSnap != Vector2.Zero)
            {
                Game1.setMousePosition((int) (currentSlot.bounds.X + positionForSnap.X), (int) (currentSlot.bounds.Y + positionForSnap.Y), ui_scale: true);
            }
        }
    }

    public override void draw(SpriteBatch b)
    {
        // Options
        for (int i = 0; i < this.OptionSlots.Count; i++)
        {
            if (CurrentItemIndex + i < Options.Count)
                Options[CurrentItemIndex + i].draw(b, OptionSlots[i].bounds.X, OptionSlots[i].bounds.Y);
        }
    }

    
    private void DownArrowPressed()
    {
        CurrentItemIndex++;
        SetScrollBarToCurrentIndex();
    }

    private void UpArrowPressed()
    {
        CurrentItemIndex--;
        SetScrollBarToCurrentIndex();
    }

    private void SetScrollBarToCurrentIndex()
    {
        if (Options.Count > 0)
        {
            _scrollBar.bounds.Y = _upArrow.bounds.Bottom + _scrollBarRunner.Height / Math.Max(1, Options.Count - OptionSlotsCount + 1) * CurrentItemIndex + 4;
            if (_scrollBar.bounds.Y > _downArrow.bounds.Y - _scrollBar.bounds.Height - 4)
            {
                _scrollBar.bounds.Y = _downArrow.bounds.Y - _scrollBar.bounds.Height - 4;
            }
        }
    }
}
