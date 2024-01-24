using Microsoft.Xna.Framework.Graphics;
using MyCafe.UI.Options;
using StardewValley.Menus;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace MyCafe.UI;
internal abstract class OptionsPageBase : MenuPageBase
{
    protected int OptionSlotsCount = 3;
    protected readonly List<ClickableComponent> OptionSlots = new();
    protected readonly List<OptionsElement> Options = new();
    protected readonly Rectangle OptionSlotSize;
    protected int OptionSlotHeld = -1;
    protected int CurrentItemIndex = 0;

    protected OptionsPageBase(string name, Rectangle bounds, CafeMenu parentMenu) : base(name, bounds, parentMenu)
    {
        // Config
        for (int i = 0; i < OptionSlotsCount; i++)
            OptionSlots.Add(new ClickableComponent(
                new Rectangle(
                    Bounds.X + Game1.tileSize / 4,
                    Bounds.Y + Game1.tileSize + i * (Bounds.Height / OptionSlotsCount),
                    Bounds.Width - Game1.tileSize / 2,
                    (Bounds.Height - 32) / OptionSlotsCount),
                i.ToString()));

        OptionSlotSize = new Rectangle(0, 0, Bounds.Width - Game1.tileSize / 4,
            (Bounds.Height) / OptionSlotsCount);
    }

    public override void populateClickableComponentList()
    {
        base.populateClickableComponentList();
        foreach (var f in Options.SelectMany(op => op.GetType().GetFields()))
        {
            if (f.FieldType.IsSubclassOf(typeof(ClickableComponent)) || f.FieldType == typeof(ClickableComponent))
            {
                if (f.GetValue(this) != null)
                {
                    allClickableComponents.Add((ClickableComponent)f.GetValue(this));
                }
            }
            else if (f.FieldType == typeof(List<ClickableComponent>))
            {
                List<ClickableComponent> components = (List<ClickableComponent>) f.GetValue(this);
                if (components == null)
                    continue;
                foreach (var c in components)
                {
                    if (c != null)
                        allClickableComponents.Add(c);
                }
            }
        }
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
    }

    public override void leftClickHeld(int x, int y)
    {
        if (OptionSlotHeld != -1)
        {
            Options[CurrentItemIndex + OptionSlotHeld].leftClickHeld(x - OptionSlots[OptionSlotHeld].bounds.X, y - OptionSlots[OptionSlotHeld].bounds.Y);
        }
    }

    public override void releaseLeftClick(int x, int y)
    {
        if (OptionSlotHeld != -1 && CurrentItemIndex + OptionSlotHeld < Options.Count)
        {
            Options[CurrentItemIndex + OptionSlotHeld].leftClickReleased(x - OptionSlots[OptionSlotHeld].bounds.X, y - OptionSlots[OptionSlotHeld].bounds.Y);
        }
        OptionSlotHeld = -1;
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

}
