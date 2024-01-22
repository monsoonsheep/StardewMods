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


    public ItemsPage(CafeMenu parent) : base("Edit Menu", parent)
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
    }

    internal void CloseTextBox()
    {
        _searchBarTextBox.Selected = false;
        Game1.keyboardDispatcher.Subscriber = null;
    }

    internal override void LeftClick(int x, int y)
    {
        if (_searchBarTextBoxBounds.Contains(x, y))
        {
            _searchBarTextBox.Text = "";
            Game1.keyboardDispatcher.Subscriber = _searchBarTextBox;
            _searchBarTextBox.SelectMe();
        }
    }

    internal override void LeftClickHeld(int x, int y)
    {
    }

    internal override void ReleaseLeftClick(int x, int y)
    {
    }

    internal override void ScrollWheelAction(int direction)
    {
    }

    internal override void HoverAction(int x, int y)
    {
    }

    internal override void Draw(SpriteBatch b)
    {
        _searchBarTextBox.Draw(b, drawShadow: false);
    }
}
