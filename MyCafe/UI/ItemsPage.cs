using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace MyCafe.UI;
internal class ItemsPage : MenuPageBase
{
    private static Rectangle source_saveButton = new (62, 32, 31, 32);
    private static Rectangle source_loadButton = new(93, 32, 31, 32);

    // Search
    private Rectangle _searchBarTextBoxBounds;
    private readonly TextBox _searchBarTextBox;
    private readonly List<Item> _searchResultItems = new();
    public readonly ClickableComponent[,] _gridSlots;

    public readonly ClickableTextureComponent _loadButton;
    public readonly ClickableTextureComponent _saveButton;

    private readonly int gridCountX;
    private readonly int gridCountY;
    private int _currentRowIndex;

    private IEnumerable<ClickableComponent> _slots
    {
        get
        {
            for (int i = 0; i < _searchResultItems.Count; i++)
            {
                yield return _gridSlots[i / gridCountX, i % gridCountX];
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

        _loadButton = new ClickableTextureComponent(
            new Rectangle(
                Bounds.Right - source_loadButton.Width - 64, Bounds.Center.Y + source_loadButton.Height, source_loadButton.Width, source_loadButton.Height),
            Mod.Sprites,
            source_loadButton,
            2f)
        {
            myID = 45000,
            downNeighborID = 45001,
            upNeighborID = -99998,
            leftNeighborID = -99998,
            rightNeighborID = -99999
        };

        _saveButton = new ClickableTextureComponent(
            new Rectangle(
                Bounds.Right - source_loadButton.Width - 64, Bounds.Center.Y - source_loadButton.Height * 2, source_loadButton.Width, source_loadButton.Height),
            Mod.Sprites, 
            source_saveButton, 
            2f)
        {
            myID = 45001,
            downNeighborID = -99999,
            upNeighborID = 45000,
            leftNeighborID = -99998,
            rightNeighborID = -99999
        };

        int padding = 4;

        float width = (Bounds.Width - 64 - 96);
        float height = (Bounds.Height - 64 * 4);

        gridCountX = (int) (width / 64f);
        gridCountY = (int) (height / 64f);

        float gridWidth = gridCountX * 64f + gridCountX * padding;
        float gridX = Bounds.X + 32;

        float gridHeight = gridCountY * 64f + gridCountY * padding;
        float gridY = Bounds.Y + 64f * 2 + (height - gridHeight) / 2;

        _gridSlots = new ClickableComponent[gridCountY, gridCountX];
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
                component.upNeighborID = (topMost) ? -99998 : -7777;

                count++;

                _gridSlots[i, j] = component;
            }
        }

        foreach (var item in Game1.player.Items)
        {
            if (item?.Category == SObject.CookingCategory)
            {
                _searchResultItems.Add(item);
            }
        }

        defaultComponent = 42420;
        UpdateSlots();
    }

    internal void CloseTextBox()
    {
        _searchBarTextBox.Selected = false;
        Game1.keyboardDispatcher.Subscriber = null;
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y);
        if (_searchBarTextBoxBounds.Contains(x, y))
        {
            _searchBarTextBox.Text = "";
            Game1.keyboardDispatcher.Subscriber = _searchBarTextBox;
            _searchBarTextBox.SelectMe();
        }
    }

    public override bool TryHover(int x, int y)
    {
        if (!base.TryHover(x, y))
            return false;

        foreach (var c in _slots)
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
                //if (getComponentWithID(oldID - ))
                // snap to search bar
                break;
            case 1:
                break;
            case 2:
                break;
            case 3:
                if (oldID == 42420)
                    SnapOutOfMenu(3);
                break;
        }
    }

    public override void draw(SpriteBatch b)
    {
        _searchBarTextBox.Draw(b, drawShadow: false);
        _loadButton.draw(b);
        _saveButton.draw(b);

        foreach (var slot in _slots)
        {
            slot.item?.drawInMenu(b, new Vector2(slot.bounds.X, slot.bounds.Y), slot.scale, 1f, 1f, StackDrawType.Hide, Color.White, false);
        }
    }

    private void UpdateSlots()
    {
        int i = _currentRowIndex;
        foreach (var slot in _slots)
        {
            if (i >= _searchResultItems.Count)
                slot.item = null;
            else
                slot.item = _searchResultItems[i];

            i++;
        }
    }

    private ClickableComponent GetSlotAtIndex(int index)
    {
        return _gridSlots[index / gridCountX, index % gridCountX];
    }
}
