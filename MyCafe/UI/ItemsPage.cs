using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;

namespace MyCafe.UI;
internal class ItemsPage : MenuPageBase
{
    private static Rectangle source_saveButton = new (62, 32, 31, 32);
    private static Rectangle source_loadButton = new(93, 32, 31, 32);

    // Search
    private readonly Rectangle _searchBarTextBoxBounds;
    private readonly List<Item> _searchResultItems = new();
    private readonly TextBox _searchBarTextBox;
    public readonly ClickableComponent SearchBarComponent;
    public readonly ClickableComponent[,] GridSlots;

    public readonly ClickableTextureComponent LoadButton;
    public readonly ClickableTextureComponent SaveButton;
    public readonly ClickableTextureComponent UpArrow;
    public readonly ClickableTextureComponent DownArrow;


    private readonly int gridCountX;
    private readonly int gridCountY;
    private int _currentRowIndex;

    private IEnumerable<ClickableComponent> ItemSlots
    {
        get
        {
            foreach (var slot in GridSlots)
            {
                if (slot.item == null)
                    yield break;
                yield return slot;
            }
            
        }
    }

    public ItemsPage(CafeMenu parent, Rectangle bounds) : base("Edit Menu", bounds, parent)
    {
        _searchBarTextBoxBounds = new Rectangle(
            Bounds.X + Bounds.Width / 4,
            Bounds.Y + Game1.tileSize / 2,
            Bounds.Width / 2,
            Game1.tileSize
        );

        // Search
        _searchBarTextBox = new TextBox(Game1.content.Load<Texture2D>("LooseSprites\\textBox"), null, Game1.smallFont, Game1.textColor)
        {
            textLimit = 32,
            Selected = false,
            Text = "Search",
            X = _searchBarTextBoxBounds.X,
            Y = _searchBarTextBoxBounds.Y,
            Width = _searchBarTextBoxBounds.Width,
            Height = _searchBarTextBoxBounds.Height
        };
        _searchBarTextBox.OnEnterPressed += (_) => CloseTextBox();

        SearchBarComponent = new ClickableComponent(
            _searchBarTextBoxBounds,
            "search")
        {
            myID = 50000,
            downNeighborID = 42420,
            upNeighborID = -99998,
            leftNeighborID = -7777,
            downNeighborImmutable = false,
            fullyImmutable = false,
        };

        LoadButton = new ClickableTextureComponent(
            new Rectangle(
                Bounds.Right - source_loadButton.Width - 64, 
                Bounds.Center.Y - source_loadButton.Height, 
                source_loadButton.Width, source_loadButton.Height),
            Mod.Sprites,
            source_loadButton,
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

        SaveButton = new ClickableTextureComponent(
            new Rectangle(
                Bounds.Right - source_loadButton.Width - 64, 
                Bounds.Center.Y + source_loadButton.Height * 2, 
                source_loadButton.Width, source_loadButton.Height),
            Mod.Sprites,
            source_saveButton,
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

        float gridWidth = (Bounds.Width - 64 - 96);
        float gridHeight = (Bounds.Height - 64 * 4);

        gridCountX = (int) (gridWidth / 64f);
        gridCountY = (int) (gridHeight / 64f);

        float gridX = Bounds.X + 32;
        float gridY = _searchBarTextBoxBounds.Bottom + 64;

        GridSlots = new ClickableComponent[gridCountY, gridCountX];
        int count = 0;
        for (int i = 0; i < gridCountY; i++)
        {
            for (int j = 0; j < gridCountX; j++)
            {
                ClickableComponent component = new(
                    new Rectangle((int) gridX + (j * Game1.tileSize) + (j * padding),
                        (int) gridY + (i * Game1.tileSize) + (i * padding),
                        Game1.tileSize, Game1.tileSize),
                    $"grid{i},{j}"
                );

                bool leftMost = (j == 0);
                bool rightMost = (j == gridCountX - 1);
                bool topMost = (i == 0);
                bool bottom = (i == gridCountY - 1);

                component.region = 42420;
                component.myID = 42420 + count;
                component.leftNeighborID = -7777;
                component.rightNeighborID = -7777;
                component.downNeighborID = (bottom) ? -99998 : -7777;
                component.upNeighborID = (topMost) ? 50000 : -7777;
                component.rightNeighborImmutable = true;
                component.upNeighborImmutable = true;

                count++;

                GridSlots[i, j] = component;
            }
        }

        var topRightSlot = GridSlots[0, GridSlots.GetLength(1) - 1];
        var bottomRightSlot = GridSlots[GridSlots.GetLength(0) - 1, GridSlots.GetLength(1) - 1];

        var upArrowBounds = new Rectangle(
            topRightSlot.bounds.Right,
            topRightSlot.bounds.Top,
            44, 48);

        var downArrowBounds = new Rectangle(
            bottomRightSlot.bounds.Right,
            bottomRightSlot.bounds.Bottom - 40,
            44, 48);

        UpArrow = new ClickableTextureComponent(upArrowBounds, Game1.mouseCursors, new Rectangle(421, 459, 11, 12), 4f)
        {
            myID = 46460,
            leftNeighborID = 42423,
            rightNeighborID = 45000,
            downNeighborID = 46461,
            fullyImmutable = true
        };
        DownArrow = new ClickableTextureComponent(downArrowBounds, Game1.mouseCursors, new Rectangle(421, 472, 11, 12), 4f)
        {
            myID = 46461,
            leftNeighborID = 42431,
            rightNeighborID = 45001,
            upNeighborID = 46460,
            fullyImmutable = true
        };

        defaultComponent = 42420;
        _searchResultItems.AddRange(Game1.player.Items.Where(i => i?.Category == SObject.CookingCategory));
        UpdateSlots();
    }

    public override void populateClickableComponentList()
    {
        base.populateClickableComponentList();

        foreach (var slot in GridSlots)
        {
            allClickableComponents.Add(slot);
        }
    }

    public override void receiveKeyPress(Keys key)
    {
        if (Game1.globalFade)
        {
            return;
        }
        if (Game1.options.menuButton.Contains(new InputButton(key)) && _searchBarTextBox is not { Selected: true })
        {
            Game1.playSound("smallSelect");
            if (readyToClose())
            {
                Game1.exitActiveMenu();
            }
        }
        else if (Game1.options.SnappyMenus && (!Game1.options.menuButton.Contains(new InputButton(key)) || _searchBarTextBox == null || !_searchBarTextBox.Selected))
        {
            base.receiveKeyPress(key);
        }
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y);
        _searchBarTextBox.Update();
        if (_searchBarTextBox.Selected)
        {
            _searchBarTextBox.Text = "";
        }
        else
        {
            foreach (var slot in ItemSlots)
            {
                if (slot.containsPoint(x, y))
                {
                    if (_parentMenu.HeldItem != null)
                        _parentMenu.HeldItem = null;
                    else
                        _parentMenu.HeldItem = slot.item;
                }
            }

            if (DownArrow.containsPoint(x, y) && _currentRowIndex < Math.Max(0, _searchResultItems.Count / GridSlots.GetLength(1) - GridSlots.GetLength(0)))
            {
                DownArrowPressed();
                Game1.playSound("shwip");
            }
            else if (UpArrow.containsPoint(x, y) && _currentRowIndex > 0)
            {
                UpArrowPressed();
                Game1.playSound("shwip");
            }
        }
    }

    public override bool TryHover(int x, int y)
    {
        if (!base.TryHover(x, y))
            return false;

        foreach (var c in ItemSlots)
        {
            c.scale = Math.Max(1f, c.scale - 0.025f);
            if (c.containsPoint(x, y) && c.item != null)
            {
                c.scale = Math.Min(c.scale + 0.05f, 1.1f);
                HoverTitle = c.item.DisplayName;
            }
        }

        return true;
    }

    protected override void customSnapBehavior(int direction, int oldRegion, int oldID)
    {
        switch (direction)
        {
            case 0:
                var upComponent = getComponentWithID(oldID - gridCountX);
                if (upComponent is { item: not null })
                    setCurrentlySnappedComponentTo(upComponent.myID);
                break;
            case 1:
                int countX = GridSlots.GetLength(1);
                int countY = GridSlots.GetLength(0);
                if ((oldID + 1 - 42420) % countX == 0 && _searchResultItems.Count / countX > countY)
                {
                    automaticSnapBehavior(direction, oldRegion, oldID);
                    if (currentlySnappedComponent == UpArrow && _currentRowIndex == 0)
                    {
                        setCurrentlySnappedComponentTo(DownArrow.myID);
                    }

                    if (currentlySnappedComponent == DownArrow &&
                        _currentRowIndex >= Math.Max(0, _searchResultItems.Count / countX - countY))
                    {
                        setCurrentlySnappedComponentTo(UpArrow.myID);
                    }
                }
                else
                {
                    var rightComponent = getComponentWithID(oldID + 1);
                    if (rightComponent is { item: not null })
                        setCurrentlySnappedComponentTo(rightComponent.myID);
                }
                break;
            case 2:
                var downComponent = getComponentWithID(oldID + gridCountX);
                if (downComponent is { item: not null })
                    setCurrentlySnappedComponentTo(downComponent.myID);
                break;
            case 3:
                if ((oldID - 42420) % gridCountX == 0)
                    SnapOut();
                else
                {
                    var leftComponent = getComponentWithID(oldID - 1);
                    if (leftComponent is { item: not null })
                        setCurrentlySnappedComponentTo(leftComponent.myID);
                }
                break;
        }
    }

    public override void draw(SpriteBatch b)
    {
        _searchBarTextBox.Draw(b, drawShadow: false);
        LoadButton.draw(b);
        SaveButton.draw(b);

        foreach (var slot in ItemSlots)
        {
            slot.item?.drawInMenu(b, new Vector2(slot.bounds.X, slot.bounds.Y), slot.scale, 1f, 1f, StackDrawType.Hide, Color.White, false);
        }

        if ((_searchResultItems.Count - _currentRowIndex * GridSlots.GetLength(1)) / GridSlots.GetLength(1) > GridSlots.GetLength(0))
        {
            DownArrow.draw(b);
        }

        if (_currentRowIndex > 0)
        {
            UpArrow.draw(b);
        }
    }

    private void UpdateSlots(int row = 0)
    {
        _currentRowIndex = row;

        foreach (var s in GridSlots)
            s.item = null;

        int yCount = GridSlots.GetLength(0);
        int xCount = GridSlots.GetLength(1);

        for (int n = 0; n < _searchResultItems.Count && n < GridSlots.Length; n++)
        {
            GetSlotAtIndex(n).item = _searchResultItems[_currentRowIndex * xCount + n];
        }
    }

    private ClickableComponent GetSlotAtIndex(int index)
    {
        return GridSlots[index / gridCountX, index % gridCountX];
    }

    internal void CloseTextBox()
    {
        _searchBarTextBox.Selected = false;
        Game1.keyboardDispatcher.Subscriber = null;
        string[] words = (_searchBarTextBox.Text ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (!words.Any())
            return;

        _searchResultItems.Clear();

        var type = ItemRegistry.RequireTypeDefinition("(O)");
        ObjectDataDefinition objectDefinition = ItemRegistry.GetObjectTypeDefinition();
        foreach (var id in type.GetAllIds())
        {
            var data = ItemRegistry.GetData(id);
            if (data.Category == SObject.CookingCategory)
            {
                SObject item = ItemRegistry.Create<SObject>($"(O){id}");
                if (item != null && words.All(word => item.DisplayName.Contains(word, StringComparison.OrdinalIgnoreCase))) 
                    _searchResultItems.Add(item);
            }
        }

        UpdateSlots();
    }

    private void DownArrowPressed()
    {
        _currentRowIndex++;
        UpdateSlots(_currentRowIndex);
    }

    private void UpArrowPressed()
    {
        _currentRowIndex--;
        UpdateSlots(_currentRowIndex);
    }
}
