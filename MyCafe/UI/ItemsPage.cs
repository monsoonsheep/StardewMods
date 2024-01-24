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
    // Search
    private Rectangle _searchBarTextBoxBounds;
    private readonly TextBox _searchBarTextBox;
    private readonly List<Item> _searchResultItems = new();
    private readonly List<ClickableComponent> _gridItems = new();
    private readonly int _itemCountInGrid;

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

        int padding = 12;

        float width = (Bounds.Width - 64f);
        float height = (Bounds.Height - 64f * 4);

        int gridCountX = (int) (width / 64f);
        int gridCountY = (int) (height / 64f);

        float gridWidth = gridCountX * 64f + gridCountX * padding;
        float gridX = Bounds.X + (float) (Bounds.Width - gridWidth) / 2f;

        float gridHeight = gridCountY * 64f + gridCountY * padding;
        float gridY = Bounds.Y + 64f * 2 + (height - gridHeight) / 2;

        int count = 0;
        for (int i = 0; i < gridCountY; i++)
        {
            for (int j = 0; j < gridCountX; j++)
            {
                var component = new ClickableComponent(
                    new Rectangle((int) gridX + (j * Game1.tileSize) + (j * padding),
                        (int) gridY + (i * Game1.tileSize) + (i * padding),
                        Game1.tileSize, Game1.tileSize),
                    $"grid{i},{j}"
                );

                bool leftMost = (j == 0);
                bool rightMost = (j == gridCountX - 1);
                bool topMost = (i == 0);
                bool bottom = (i == gridCountY - 1);

                component.myID = 42420 + count;
                component.leftNeighborID = (leftMost && topMost) ? -99998 : 42420 + count - 1;
                component.rightNeighborID = (rightMost && bottom) ? -99998 : 42420 + count + 1;
                component.downNeighborID = (bottom) ? -99998 : 42420 + count + gridCountX;
                component.upNeighborID = (topMost) ? -99998 : 42420 + count - gridCountY;

                count++;

                _gridItems.Add(component);
            }
        }

        _itemCountInGrid = 0;
        foreach (var item in Game1.player.Items)
        {
            if (item?.Category == StardewValley.Object.CookingCategory)
            {
                _gridItems[_itemCountInGrid++].item = item;
            }
        }
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

    public override void performHoverAction(int x, int y)
    {
        base.performHoverAction(x, y);
        HoverText = "";
        HoverTitle = "";

        for (int i = 0; i < _itemCountInGrid; i++)
        {
            var component = _gridItems[i];

            component.scale = Math.Max(1f, component.scale - 0.025f);

            if (_gridItems[i].containsPoint(x, y))
            {
                component.scale = Math.Min(component.scale + 0.05f, 1.1f);

                HoverTitle = _gridItems[i].item.DisplayName;
                HoverText = _gridItems[i].item.getDescription();
            }
        }
    }

    public override void draw(SpriteBatch b)
    {
        _searchBarTextBox.Draw(b, drawShadow: false);

        for (int i = 0; i < _itemCountInGrid; i++)
        {
            _gridItems[i].item.drawInMenu(b, new Vector2(_gridItems[i].bounds.X, _gridItems[i].bounds.Y), _gridItems[i].scale, 1f, 1f, StackDrawType.Hide, Color.White, false);
        }
    }
}
