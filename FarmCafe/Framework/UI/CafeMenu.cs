using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FarmCafe.Framework.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;
using StardewValley.Objects;
using static StardewValley.Menus.CharacterCustomization;
// ReSharper disable InconsistentNaming

namespace FarmCafe.Framework.UI
{
    public class ItemEqualityComparer : IEqualityComparer<Item>
    {
        public bool Equals(Item x, Item y)
        {
            return x != null && y != null && x.ParentSheetIndex == y.ParentSheetIndex;
        }

        public int GetHashCode(Item obj) => (obj != null) ? obj.ParentSheetIndex * 900 : -1;
    }

    public class RecentlyAddedItemsMenu : MenuMenu
    {
        public RecentlyAddedItemsMenu(int xPosition, int yPosition, IList<Item> items, int capacity) : base(xPosition,
            yPosition, items, capacity, 1)
        {
            
        }

        public override void draw(SpriteBatch b)
        {

            Game1.drawDialogueBox(
                xPositionOnScreen - borderWidth - spaceToClearSideBorder,
                yPositionOnScreen - borderWidth - spaceToClearTopBorder,
                width + borderWidth * 2 + spaceToClearSideBorder * 2,
                height + spaceToClearTopBorder + borderWidth * 2,
                speaker: false,
                drawOnlyBox: true);

            base.draw(b);
            b.DrawString(Game1.tinyFont, "Recent Items",
                new Vector2(xPositionOnScreen + horizontalGap, yPositionOnScreen - 32), Color.DimGray, 0,
                Vector2.Zero, 0.75f, SpriteEffects.None, 0.9f);
        }

        public override Item leftClick(int x, int y, Item _ignored, bool playSound = true)
        {
            foreach (ClickableComponent c in inventory)
            {
                if (c.containsPoint(x, y))
                {
                    int slotNumber = Convert.ToInt32(c.name);
                    if (slotNumber < actualInventory.Count)
                    {
                        if (actualInventory[slotNumber] != null)
                        {
                            return actualInventory[slotNumber];
                        }
                    }
                }
            }

            return null;
        }

    }

    public class MenuMenu : IClickableMenu
    {
        public List<ClickableComponent> inventory = new List<ClickableComponent>();
        public IList<Item> actualInventory;

        public int capacity;
        public int rows;
        public int horizontalGap = 0;
        public int verticalGap = 0;

        public string hoverText = "";
        public string hoverTitle = "";
        public string descriptionTitle = "";
        public string descriptionText = "";

        public MenuMenu(int xPosition, int yPosition, IList<Item> items, int capacity, int rows) : base(xPosition, yPosition, 64 * capacity / rows, 64 * rows + 16)
        {
            this.capacity = capacity;
            this.rows = rows;
            this.actualInventory = items;

            for (int i = 0; i < this.capacity; i++)
            {
                inventory.Add(new ClickableComponent(
                    new Rectangle(
                        xPosition + i % (this.capacity / rows) * 64 + horizontalGap * (i % (this.capacity / rows)), 
                        yPositionOnScreen + i / (this.capacity / rows) * (64 + verticalGap) + (i / (this.capacity / rows) - 1) * 4 - 0, 
                        64, 
                        64), 
                    string.Concat(i))
                {
                    myID = i,
                    leftNeighborID = ((i % (this.capacity / rows) != 0) ? (i - 1) : 107),
                    rightNeighborID = (((i + 1) % (this.capacity / rows) != 0) ? (i + 1) : 106),
                    downNeighborID = ((i >= this.actualInventory.Count - this.capacity / rows) ? 102 : (i + this.capacity / rows)),
                    upNeighborID = ((i < this.capacity / rows) ? (12340 + i) : (i - this.capacity / rows)),
                    region = 9000,
                    upNeighborImmutable = true,
                    downNeighborImmutable = true,
                    leftNeighborImmutable = true,
                    rightNeighborImmutable = true
                });
            }

        }

        public virtual void initialize()
        {
            foreach (var component in inventory)
            {
                if (component != null)
                {
                    component.myID += 53910;
                    component.upNeighborID += 53910;
                    component.rightNeighborID += 53910;
                    component.downNeighborID = -7777;
                    component.leftNeighborID += 53910;
                    component.fullyImmutable = true;
                }
            }
        }

        public virtual Item leftClick(int x, int y, Item _ignored, bool playSound = true)
        {
            foreach (ClickableComponent c in inventory)
            {
                if (c.containsPoint(x, y))
                {
                    int slotNumber = Convert.ToInt32(c.name);
                    if (slotNumber < actualInventory.Count)
                    {
                        if (actualInventory[slotNumber] != null)
                        {
                            return RemoveFromMenu(slotNumber);
                        }
                    }
                }
            }

            return null;
        }

        
        public Item getItemAt(int x, int y)
        {
            foreach (ClickableComponent c in inventory)
            {
                if (c.containsPoint(x, y))
                {
                    return getItemFromClickableComponent(c);
                }
            }
            return null;
        }

        public Item getItemFromClickableComponent(ClickableComponent c)
        {
            if (c != null)
            {
                int slotNumber = Convert.ToInt32(c.name);
                if (slotNumber < actualInventory.Count)
                {
                    return actualInventory[slotNumber];
                }
            }
            return null;
        }

        public virtual bool AddToMenu(Item itemToAdd)
        {
            if (actualInventory.Contains(itemToAdd, new ItemEqualityComparer()))
                return false;
            
            for (int i = 0; i < actualInventory.Count; i++)
            {
                if (actualInventory[i] == null)
                {
                    actualInventory[i] = itemToAdd.getOne();
                    actualInventory[i].Stack = 1;
                    return true;
                }
            }

            return false;
        }

        public virtual Item RemoveFromMenu(int slotNumber)
        {
            Item tmp = actualInventory[slotNumber];
            if (tmp == null)
                return null;
            
            actualInventory[slotNumber] = null;
            int firstEmpty = slotNumber;
            for (int i = slotNumber + 1; i < actualInventory.Count; i++)
            {
                if (actualInventory[i] != null)
                {
                    actualInventory[firstEmpty] = actualInventory[i];
                    actualInventory[i] = null;
                    firstEmpty += 1;
                }
            }

            return tmp;
        }

        public virtual Item hover(int x, int y, Item heldItem)
        {
            descriptionText = "";
            descriptionTitle = "";
            hoverText = "";
            hoverTitle = "";
            Item hoveredItem = null;
            foreach (ClickableComponent c in inventory)
            {
                int slotNumber = Convert.ToInt32(c.name);
                c.scale = Math.Max(1f, c.scale - 0.025f);
                if (c.containsPoint(x, y) && slotNumber < actualInventory.Count && slotNumber < actualInventory.Count && actualInventory[slotNumber] != null)
                {
                    descriptionTitle = actualInventory[slotNumber].DisplayName;
                    descriptionText = Environment.NewLine + actualInventory[slotNumber].getDescription();
                    c.scale = Math.Min(c.scale + 0.05f, 1.1f);
                    string s = actualInventory[slotNumber].getHoverBoxText(heldItem);
                    if (s != null)
                    {
                        hoverText = s;
                        hoverTitle = actualInventory[slotNumber].DisplayName;
                    }
                    else
                    {
                        hoverText = actualInventory[slotNumber].getDescription();
                        hoverTitle = actualInventory[slotNumber].DisplayName;
                    }
                    if (hoveredItem == null)
                    {
                        hoveredItem = actualInventory[slotNumber];
                    }
                }
            }
            return hoveredItem;
        }

        public override void draw(SpriteBatch b)
		{
            // Upper menu box
            Game1.drawDialogueBox(
                xPositionOnScreen - borderWidth - spaceToClearSideBorder,
                yPositionOnScreen - borderWidth - spaceToClearTopBorder,
                width + borderWidth * 2 + spaceToClearSideBorder * 2,
                height + spaceToClearTopBorder + borderWidth * 2,
                speaker: false,
                drawOnlyBox: true);

            Color tint = Color.White;
			Texture2D texture = Game1.menuTexture;
            
            for (int k = 0; k < capacity; k++)
            {
                Vector2 toDraw2 = new Vector2(xPositionOnScreen + k % (capacity / rows) * 64 + horizontalGap * (k % (capacity / rows)), yPositionOnScreen + k / (capacity / rows) * (64 + verticalGap) + (k / (capacity / rows) - 1) * 4);
                b.Draw(texture, toDraw2, Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10), tint, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.5f);
                if ((true) && k >= (int)Game1.player.maxItems)
                { // true is showgrayedoutslots
                    b.Draw(texture, toDraw2, Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 57), tint * 0.5f, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.5f);
                }
                if (actualInventory.Count > k && actualInventory.ElementAt(k) != null)
                {
                    actualInventory[k].drawInMenu(b, toDraw2, (inventory.Count > k) ? inventory[k].scale : 1f, 1f, 0.865f, StackDrawType.Draw, Color.White, false);
                }
            }
        }
    }

    public sealed class CafeMenu : MenuWithInventory
    {
        private MenuMenu menuItemsMenu;
        private RecentlyAddedItemsMenu recentyAddedMenu;
        private ClickableTextureComponent recentMenuButton;
        private readonly IList<Item> menuItemsList;
        private readonly IList<Item> recentlyAddedItemsList;

        private bool showRecentMenu;

        public CafeMenu(IList<Item> menu, IList<Item> recentItems)
			: base(highlighterMethod: null, okButton: true)
        {
            this.menuItemsList = menu;
            this.recentlyAddedItemsList = recentItems;

            InitializeComponents();

            this.showRecentMenu = false;

            populateClickableComponentList();
        }

        public void InitializeComponents()
        {
            // Cafe menu inventory menu
            this.menuItemsMenu = 
                new MenuMenu(
                    xPositionOnScreen + 32,
                    yPositionOnScreen - 16,
                    items: menuItemsList,
                    capacity: 18,
                    rows: 2);
            this.menuItemsMenu.populateClickableComponentList();
            menuItemsMenu.initialize();

            // History items menu
            this.recentyAddedMenu =
                new RecentlyAddedItemsMenu(
                    xPositionOnScreen + spaceToClearSideBorder + borderWidth / 2, 
                    yPositionOnScreen + spaceToClearTopBorder + borderWidth + 192 - 16,
                    //xPositionOnScreen + 32,
                    //yPositionOnScreen + 172,
                    items: recentlyAddedItemsList, 
                    capacity: 9);
            this.recentyAddedMenu.populateClickableComponentList();
            recentyAddedMenu.initialize();


            // Button to toggle the recent items menu
            this.recentMenuButton = new ClickableTextureComponent(
                new Rectangle(
                    xPositionOnScreen - borderWidth - 24,
                    yPositionOnScreen,
                    9 * 4,
                    9 * 4),
                Game1.mouseCursors,
                new Rectangle(410, 501, 9, 9), 
                4f);
            //{ // Is this needed?
            //    myID = 5948,
            //    downNeighborID = 4857,
            //    leftNeighborID = 12,
            //    upNeighborID = 106
            //};

        }

		public override void receiveLeftClick(int x, int y, bool playSound = true)
		{
            // Ok Button clicked
            if (okButton != null && okButton.containsPoint(x, y) && readyToClose())
			{
				exitThisMenu();
                Game1.playSound("bigDeSelect");
			}
            // Recent items show button clicked
            if (recentMenuButton != null && recentMenuButton.containsPoint(x, y))
            {
                showRecentMenu = !showRecentMenu;
                return;
            }

            // Clicked on an item in the menu items menu (returns removed item)
            if (menuItemsMenu.leftClick(x, y, heldItem, playSound: false) != null)
                return;

            Item itemToAdd = base.inventory.getItemAt(x, y)?.getOne() ?? recentyAddedMenu.leftClick(x, y, null);
            if (itemToAdd != null && itemToAdd.canBeDropped() && itemToAdd.salePrice() > 0)
            {
                itemToAdd.Stack = 1;
                if (menuItemsMenu.AddToMenu(itemToAdd))
                {
                    AddToRecentItems(itemToAdd);
                }
            }
        }

        internal void AddToRecentItems(Item item)
        {
            if (recentlyAddedItemsList.Contains(item, new ItemEqualityComparer()))
                return;

            // Shift everything left
            if (recentlyAddedItemsList.All(i => i != null))
            {
                for (int i = 0; i < recentlyAddedItemsList.Count; i++)
                {
                    if (i != recentlyAddedItemsList.Count - 1)
                        recentlyAddedItemsList[i] = recentlyAddedItemsList[i + 1];
                    else
                        recentlyAddedItemsList[i] = null;
                }
            }

            for (int i = 0; i < recentlyAddedItemsList.Count; i++)
            {
                if (recentlyAddedItemsList[i] == null)
                {
                    recentlyAddedItemsList[i] = item;
                    return;
                }
            }
        }

		public override void receiveRightClick(int x, int y, bool playSound = true)
		{
			//heldItem = inventory.rightClick(x, y, heldItem, playSound);
		}

		public override void performHoverAction(int x, int y)
		{
			hoveredItem = null;
			hoverText = "";
			base.performHoverAction(x, y);
            
            if (recentMenuButton.containsPoint(x, y))
                recentMenuButton.scale = Math.Min(4.4f, recentMenuButton.scale + 0.15f);
            else
                recentMenuButton.scale = Math.Max(4f, recentMenuButton.scale - 0.15f);

            Item itemGrabHoveredItem = menuItemsMenu.hover(x, y, heldItem) ?? recentyAddedMenu.hover(x, y, heldItem);
            if (itemGrabHoveredItem != null)
                hoveredItem = itemGrabHoveredItem;
            
        }

		public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
		{
			base.gameWindowSizeChanged(oldBounds, newBounds);
			
			if (okButton != null)
                okButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width + 4, yPositionOnScreen + height - 192 - borderWidth, 64, 64), Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46), 1f);

            InitializeComponents();
        }

		public override void draw(SpriteBatch b)
		{
            // Recent Items Menu
            if (showRecentMenu)
                recentyAddedMenu.draw(b);
            else
                base.draw(b, drawUpperPortion: false, drawDescriptionArea: false);

            this.recentMenuButton.draw(b);
            
            //b.Draw(Game1.mouseCursors, new Vector2(xPositionOnScreen - 64, yPositionOnScreen + height / 2 + 64 + 16), new Rectangle(16, 368, 12, 16), Color.White, 4.712389f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
            //b.Draw(Game1.mouseCursors, new Vector2(xPositionOnScreen - 64, yPositionOnScreen + height / 2 + 64 - 16), new Rectangle(21, 368, 11, 16), Color.White, 4.712389f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
            //b.Draw(Game1.mouseCursors, new Vector2(xPositionOnScreen - 40, yPositionOnScreen + height / 2 + 64 - 44), new Rectangle(4, 372, 8, 11), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);

            //b.Draw(Game1.mouseCursors, new Vector2(xPositionOnScreen - 72, yPositionOnScreen + 64 + 16), new Rectangle(16, 368, 12, 16), Color.White, 4.712389f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
            //b.Draw(Game1.mouseCursors, new Vector2(xPositionOnScreen - 72, yPositionOnScreen + 64 - 16), new Rectangle(21, 368, 11, 16), Color.White, 4.712389f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
            //b.Draw(Game1.mouseCursors, new Vector2(xPositionOnScreen - 52, yPositionOnScreen + 64 - 44), new Rectangle(127, 412, 10, 11), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);

            // Menu items Menu
            menuItemsMenu.draw(b);

            if (hoverText != null && (hoveredItem == null || menuItemsMenu == null))
            {
                if (hoverAmount > 0)
                    drawToolTip(b, hoverText, "", null, heldItem: true, -1, 0, -1, -1, null, hoverAmount);
                else
                    drawHoverText(b, hoverText, Game1.smallFont);
            }

            if (hoveredItem != null)
                drawToolTip(b, hoveredItem.getDescription(), hoveredItem.DisplayName, hoveredItem, heldItem != null);
            else if (hoveredItem != null && menuItemsMenu != null)
                drawToolTip(b, menuItemsMenu.descriptionText, menuItemsMenu.descriptionTitle, hoveredItem, heldItem != null);

            heldItem?.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 8, Game1.getOldMouseY() + 8), 1f);
            
            Game1.mouseCursorTransparency = 1f;
            drawMouse(b);
        }
    }
}