using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using SObject = StardewValley.Object;
namespace MyCafe.UI.Pages;
internal class ItemsPage : MenuPageBase
{
    private static Rectangle SourceSaveButton = new(62, 32, 31, 32);
    private static Rectangle SourceLoadButton = new(93, 32, 31, 32);

    // Search
    private readonly Rectangle SearchBarTextBoxBounds;
    private readonly List<Item> SearchResultItems = new();
    private readonly TextBox SearchBarTextBox;
    public readonly ClickableComponent SearchBarComponent;
    public readonly ClickableComponent[,] GridSlots;

    public readonly ClickableTextureComponent LoadButton;
    public readonly ClickableTextureComponent SaveButton;
    public readonly ClickableTextureComponent UpArrow;
    public readonly ClickableTextureComponent DownArrow;


    private readonly int GridCountX;
    private readonly int GridCountY;
    private int CurrentRowIndex;

    private IEnumerable<ClickableComponent> ItemSlots
    {
        get
        {
            foreach (var slot in this.GridSlots)
            {
                if (slot.item == null)
                    yield break;
                yield return slot;
            }

        }
    }

    public ItemsPage(CafeMenu parent, Rectangle bounds, Texture2D sprites) : base("Edit Menu", bounds, parent, sprites)
    {
        this.SearchBarTextBoxBounds = new Rectangle(this.Bounds.X + this.Bounds.Width / 4, this.Bounds.Y + Game1.tileSize / 2, this.Bounds.Width / 2,
            Game1.tileSize
        );

        // Search
        this.SearchBarTextBox = new TextBox(Game1.content.Load<Texture2D>("LooseSprites\\textBox"), null, Game1.smallFont, Game1.textColor)
        {
            textLimit = 32,
            Selected = false,
            Text = "Search",
            X = this.SearchBarTextBoxBounds.X,
            Y = this.SearchBarTextBoxBounds.Y,
            Width = this.SearchBarTextBoxBounds.Width,
            Height = this.SearchBarTextBoxBounds.Height
        };
        this.SearchBarTextBox.OnEnterPressed += (_) => this.CloseTextBox();

        this.SearchBarComponent = new ClickableComponent(this.SearchBarTextBoxBounds,
            "search")
        {
            myID = 50000,
            downNeighborID = 42420,
            upNeighborID = -99998,
            leftNeighborID = -7777,
            downNeighborImmutable = false,
            fullyImmutable = false,
        };

        this.LoadButton = new ClickableTextureComponent(
            new Rectangle(this.Bounds.Right - SourceLoadButton.Width - 64, this.Bounds.Center.Y - SourceLoadButton.Height,
                SourceLoadButton.Width, SourceLoadButton.Height), this.Sprites,
            SourceLoadButton,
            2f)
        {
            myID = 45000,
            downNeighborID = 45001,
            upNeighborID = -99998,
            leftNeighborID = -99999,
            rightNeighborID = -99998,
            leftNeighborImmutable = false,
            fullyImmutable = false
        };

        this.SaveButton = new ClickableTextureComponent(
            new Rectangle(this.Bounds.Right - SourceLoadButton.Width - 64, this.Bounds.Center.Y + SourceLoadButton.Height * 2,
                SourceLoadButton.Width, SourceLoadButton.Height), this.Sprites,
            SourceSaveButton,
            2f)
        {
            myID = 45001,
            downNeighborID = -99999,
            upNeighborID = 45000,
            leftNeighborID = -99999,
            rightNeighborID = -99998,
            leftNeighborImmutable = false,
            fullyImmutable = false
        };


        int padding = 4;

        float gridWidth = this.Bounds.Width - 64 - 96;
        float gridHeight = this.Bounds.Height - 64 * 4;

        this.GridCountX = (int)(gridWidth / 64f);
        this.GridCountY = (int)(gridHeight / 64f);

        float gridX = this.Bounds.X + 32;
        float gridY = this.SearchBarTextBoxBounds.Bottom + 64;

        this.GridSlots = new ClickableComponent[this.GridCountY, this.GridCountX];
        int count = 0;
        for (int i = 0; i < this.GridCountY; i++)
        {
            for (int j = 0; j < this.GridCountX; j++)
            {
                ClickableComponent component = new(
                    new Rectangle((int)gridX + j * Game1.tileSize + j * padding,
                        (int)gridY + i * Game1.tileSize + i * padding,
                        Game1.tileSize, Game1.tileSize),
                    $"grid{i},{j}"
                );

                bool leftMost = j == 0;
                bool rightMost = j == this.GridCountX - 1;
                bool topMost = i == 0;
                bool bottom = i == this.GridCountY - 1;

                component.region = 42420;
                component.myID = 42420 + count;
                component.leftNeighborID = -7777;
                component.rightNeighborID = -7777;
                component.downNeighborID = bottom ? -99998 : -7777;
                component.upNeighborID = topMost ? 50000 : -7777;
                component.rightNeighborImmutable = true;
                component.upNeighborImmutable = true;

                count++;

                this.GridSlots[i, j] = component;
            }
        }

        var topRightSlot = this.GridSlots[0, this.GridSlots.GetLength(1) - 1];
        var bottomRightSlot = this.GridSlots[this.GridSlots.GetLength(0) - 1, this.GridSlots.GetLength(1) - 1];

        var upArrowBounds = new Rectangle(
            topRightSlot.bounds.Right,
            topRightSlot.bounds.Top,
            44, 48);

        var downArrowBounds = new Rectangle(
            bottomRightSlot.bounds.Right,
            bottomRightSlot.bounds.Bottom - 40,
            44, 48);

        this.UpArrow = new ClickableTextureComponent(upArrowBounds, Game1.mouseCursors, new Rectangle(421, 459, 11, 12), 4f)
        {
            myID = 46460,
            leftNeighborID = 42423,
            rightNeighborID = 45000,
            downNeighborID = 46461,
            fullyImmutable = true
        };
        this.DownArrow = new ClickableTextureComponent(downArrowBounds, Game1.mouseCursors, new Rectangle(421, 472, 11, 12), 4f)
        {
            myID = 46461,
            leftNeighborID = 42431,
            rightNeighborID = 45001,
            upNeighborID = 46460,
            fullyImmutable = true
        };

        this.DefaultComponent = 42420;
        this.SearchResultItems.AddRange(Game1.player.Items.Where(i => i?.Category == SObject.CookingCategory));
        this.UpdateSlots();
    }

    public override void populateClickableComponentList()
    {
        base.populateClickableComponentList();

        foreach (var slot in this.GridSlots)
        {
            this.allClickableComponents.Add(slot);
        }
    }

    public override void receiveKeyPress(Keys key)
    {
        if (Game1.globalFade)
        {
            return;
        }
        if (Game1.options.menuButton.Contains(new InputButton(key)) && this.SearchBarTextBox is not { Selected: true })
        {
            Game1.playSound("smallSelect");
            if (this.readyToClose())
            {
                Game1.exitActiveMenu();
            }
        }
        else if (Game1.options.SnappyMenus && (!Game1.options.menuButton.Contains(new InputButton(key)) || this.SearchBarTextBox == null || !this.SearchBarTextBox.Selected))
        {
            base.receiveKeyPress(key);
        }
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y);
        this.SearchBarTextBox.Update();
        if (this.SearchBarTextBox.Selected)
        {
            this.SearchBarTextBox.Text = "";
        }
        else
        {
            foreach (var slot in this.ItemSlots)
            {
                if (slot.containsPoint(x, y))
                {
                    if (this.ParentMenu.HeldItem != null)
                        this.ParentMenu.HeldItem = null;
                    else
                        this.ParentMenu.HeldItem = slot.item;
                }
            }

            if (this.DownArrow.containsPoint(x, y) && this.CurrentRowIndex < Math.Max(0, this.SearchResultItems.Count / this.GridSlots.GetLength(1) - this.GridSlots.GetLength(0)))
            {
                this.DownArrowPressed();
                Game1.playSound("shwip");
            }
            else if (this.UpArrow.containsPoint(x, y) && this.CurrentRowIndex > 0)
            {
                this.UpArrowPressed();
                Game1.playSound("shwip");
            }
        }
    }

    public override bool TryHover(int x, int y)
    {
        if (!base.TryHover(x, y))
            return false;

        foreach (var c in this.ItemSlots)
        {
            c.scale = Math.Max(1f, c.scale - 0.025f);
            if (c.containsPoint(x, y) && c.item != null)
            {
                c.scale = Math.Min(c.scale + 0.05f, 1.1f);
                this.HoverTitle = c.item.DisplayName;
            }
        }

        return true;
    }

    protected override void customSnapBehavior(int direction, int oldRegion, int oldId)
    {
        switch (direction)
        {
            case 0:
                var upComponent = this.getComponentWithID(oldId - this.GridCountX);
                if (upComponent is { item: not null }) this.setCurrentlySnappedComponentTo(upComponent.myID);
                break;
            case 1:
                int countX = this.GridSlots.GetLength(1);
                int countY = this.GridSlots.GetLength(0);
                if ((oldId + 1 - 42420) % countX == 0 && this.SearchResultItems.Count / countX > countY)
                {
                    this.automaticSnapBehavior(direction, oldRegion, oldId);
                    if (this.currentlySnappedComponent == this.UpArrow && this.CurrentRowIndex == 0)
                    {
                        this.setCurrentlySnappedComponentTo(this.DownArrow.myID);
                    }

                    if (this.currentlySnappedComponent == this.DownArrow && this.CurrentRowIndex >= Math.Max(0, this.SearchResultItems.Count / countX - countY))
                    {
                        this.setCurrentlySnappedComponentTo(this.UpArrow.myID);
                    }
                }
                else
                {
                    var rightComponent = this.getComponentWithID(oldId + 1);
                    if (rightComponent is { item: not null }) this.setCurrentlySnappedComponentTo(rightComponent.myID);
                }
                break;
            case 2:
                var downComponent = this.getComponentWithID(oldId + this.GridCountX);
                if (downComponent is { item: not null }) this.setCurrentlySnappedComponentTo(downComponent.myID);
                break;
            case 3:
                if ((oldId - 42420) % this.GridCountX == 0)
                    this.SnapOut();
                else
                {
                    var leftComponent = this.getComponentWithID(oldId - 1);
                    if (leftComponent is { item: not null }) this.setCurrentlySnappedComponentTo(leftComponent.myID);
                }
                break;
        }
    }

    public override void draw(SpriteBatch b)
    {
        this.SearchBarTextBox.Draw(b, drawShadow: false);
        this.LoadButton.draw(b);
        this.SaveButton.draw(b);

        foreach (var slot in this.ItemSlots)
        {
            slot.item?.drawInMenu(b, new Vector2(slot.bounds.X, slot.bounds.Y), slot.scale, 1f, 1f, StackDrawType.Hide, Color.White, false);
        }

        if ((this.SearchResultItems.Count - this.CurrentRowIndex * this.GridSlots.GetLength(1)) / this.GridSlots.GetLength(1) > this.GridSlots.GetLength(0))
        {
            this.DownArrow.draw(b);
        }

        if (this.CurrentRowIndex > 0)
        {
            this.UpArrow.draw(b);
        }
    }

    private void UpdateSlots(int row = 0)
    {
        this.CurrentRowIndex = row;

        foreach (var s in this.GridSlots)
            s.item = null;

        int yCount = this.GridSlots.GetLength(0);
        int xCount = this.GridSlots.GetLength(1);

        for (int n = 0; n < this.SearchResultItems.Count && n < this.GridSlots.Length; n++)
        {
            this.GetSlotAtIndex(n).item = this.SearchResultItems[this.CurrentRowIndex * xCount + n];
        }
    }

    private ClickableComponent GetSlotAtIndex(int index)
    {
        return this.GridSlots[index / this.GridCountX, index % this.GridCountX];
    }

    internal void CloseTextBox()
    {
        this.SearchBarTextBox.Selected = false;
        Game1.keyboardDispatcher.Subscriber = null;
        string[] words = (this.SearchBarTextBox.Text ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (!words.Any())
            return;

        this.SearchResultItems.Clear();

        var type = ItemRegistry.RequireTypeDefinition("(O)");
        ObjectDataDefinition objectDefinition = ItemRegistry.GetObjectTypeDefinition();
        foreach (string? id in type.GetAllIds())
        {
            var data = ItemRegistry.GetData(id);
            if (data.Category == SObject.CookingCategory)
            {
                SObject item = ItemRegistry.Create<SObject>($"(O){id}");
                if (item != null && words.All(word => item.DisplayName.Contains(word, StringComparison.OrdinalIgnoreCase))) this.SearchResultItems.Add(item);
            }
        }

        this.UpdateSlots();
    }

    private void DownArrowPressed()
    {
        this.CurrentRowIndex++;
        this.UpdateSlots(this.CurrentRowIndex);
    }

    private void UpArrowPressed()
    {
        this.CurrentRowIndex--;
        this.UpdateSlots(this.CurrentRowIndex);
    }
}
