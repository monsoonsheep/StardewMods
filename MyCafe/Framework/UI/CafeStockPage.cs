namespace MyCafe.Framework.UI;

/*
public sealed class CafeStockPage : IClickableMenu
{
    public class RecentlyAddedItemsCafeStockInventory : CafeStockInventory
    {
        public RecentlyAddedItemsCafeStockInventory(int xPosition, int yPosition, IList<Item> items, int capacity) : base(xPosition,
            yPosition, items, capacity, 1)
        {
        }

        public new Item leftClick(int x, int y, Item _ignored, bool playSound = true)
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

        public override void draw(SpriteBatch b)
        {
            base.draw(b);
            b.DrawString(Game1.tinyFont, "Recent Items",
                new Vector2(xPositionOnScreen + horizontalGap, yPositionOnScreen - 32), Color.DimGray, 0,
                Vector2.Zero, 0.75f, SpriteEffects.None, 0.9f);
        }
    }

    public class CafeStockInventory : IClickableMenu
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

        public CafeStockInventory(int xPosition, int yPosition, IList<Item> items, int capacity, int rows) : base(xPosition, yPosition,
            64 * capacity / rows,
            64 * rows + 16)
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

        public virtual int leftClick(int x, int y, Item _ignored, bool playSound = true)
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
                            return slotNumber;
                        }
                    }
                }
            }

            return -1;
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
                if (c.containsPoint(x, y) && slotNumber < actualInventory.Count && slotNumber < actualInventory.Count &&
                    actualInventory[slotNumber] != null)
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

                    hoveredItem ??= actualInventory[slotNumber];
                }
            }

            return hoveredItem;
        }

        public override void draw(SpriteBatch b)
        {
            for (int k = 0; k < capacity; k++)
            {
                Vector2 toDraw2 = new Vector2(xPositionOnScreen + k % (capacity / rows) * 64 + horizontalGap * (k % (capacity / rows)),
                    yPositionOnScreen + k / (capacity / rows) * (64 + verticalGap) + (k / (capacity / rows) - 1) * 4);

                b.Draw(Game1.menuTexture, toDraw2, Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10), Color.White, 0f, Vector2.Zero, 1f,
                    SpriteEffects.None, 0.5f);

                if (actualInventory.Count > k && actualInventory.ElementAt(k) != null)
                {
                    actualInventory[k].drawInMenu(b, toDraw2, (inventory.Count > k) ? inventory[k].scale : 1f, 1f, 0.865f, StackDrawType.Draw, Color.White,
                        false);
                }
            }
        }
    }

    public Item heldItem;
    public Item hoveredItem;

    public int wiggleWordsTimer;
    public int hoverAmount;
    public string descriptionText = "";
    public string hoverText = "";
    public string descriptionTitle = "";

    public InventoryMenu inventory;
    private CafeStockInventory cafeStockInventory;
    private RecentlyAddedItemsCafeStockInventory recentlyAddedInventory;
    private ClickableTextureComponent recentMenuButton;
    private readonly IList<Item> menuItemsList;
    private readonly IList<Item> recentlyAddedItemsList;

    private bool showRecentMenu;

    public delegate bool addToMenuFunction(Item item);

    private readonly addToMenuFunction addFunction;

    public delegate Item removeFromMenuFunction(int slotNumber);

    private readonly removeFromMenuFunction removeFunction;


    public CafeStockPage(int x, int y, int width, int height)
        : base(x, y, width, height)
    {
        // TODO: Dependency injection somehow
        this.menuItemsList = MenuManager.Instance.menuItems;
        this.recentlyAddedItemsList = MenuManager.Instance.recentlyAddedMenuItems;
        this.addFunction = MenuManager.Instance.AddToMenu;
        this.removeFunction = MenuManager.Instance.RemoveFromMenu;

        this.showRecentMenu = false;

        InitializeComponents();
    }

    public void InitializeComponents()
    {
        this.inventory =
            new InventoryMenu(
                xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + IClickableMenu.borderWidth / 2,
                yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth + 300 - 16,
                playerInventory: true);
        this.inventory.populateClickableComponentList();

        // Cafe menu inventory menu
        this.cafeStockInventory =
            new CafeStockInventory(
                xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + IClickableMenu.borderWidth / 2,
                yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth,
                items: menuItemsList,
                capacity: 18,
                rows: 2);
        this.cafeStockInventory.populateClickableComponentList();
        cafeStockInventory.initialize();

        // History items menu
        this.recentlyAddedInventory =
            new RecentlyAddedItemsCafeStockInventory(
                xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + IClickableMenu.borderWidth / 2,
                yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth + 300 - 16,
                items: recentlyAddedItemsList,
                capacity: 9);
        this.recentlyAddedInventory.populateClickableComponentList();
        recentlyAddedInventory.initialize();

        // Button to toggle the recent items menu
        this.recentMenuButton = new ClickableTextureComponent(
            new Rectangle(
                xPositionOnScreen - borderWidth - 24,
                yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth,
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
        // Recent items show button clicked
        if (recentMenuButton != null && recentMenuButton.containsPoint(x, y))
        {
            showRecentMenu = !showRecentMenu;
            return;
        }

        int removedItemFromMenu = cafeStockInventory.leftClick(x, y, heldItem, playSound: false);
        // Clicked on an item in the menu items menu (returns removes)
        if (removedItemFromMenu != -1)
        {
            if (removeFunction != null && removeFunction(removedItemFromMenu) != null)
                return;
        }


        Item itemToAdd = (showRecentMenu is false) ? inventory.getItemAt(x, y)?.getOne() : recentlyAddedInventory.leftClick(x, y, null);
        if (itemToAdd != null && itemToAdd.canBeDropped())
        {
            itemToAdd.Stack = 1;
            if (addFunction != null && addFunction(itemToAdd))
            {
                AddToRecentItems(itemToAdd);
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

        Item itemGrabHoveredItem = inventory.hover(x, y, heldItem) ??
                                   cafeStockInventory.hover(x, y, heldItem) ?? recentlyAddedInventory.hover(x, y, heldItem);
        if (itemGrabHoveredItem != null)
            hoveredItem = itemGrabHoveredItem;
    }

    public override void draw(SpriteBatch b)
    {
        // Recent Items Menu
        if (showRecentMenu)
            recentlyAddedInventory.draw(b);
        else
        {
            inventory.draw(b, -1, -1, -1);
        }
        this.recentMenuButton.draw(b);

        // horizontal partition
        int yPositionForPartition = yPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder + 192;
        b.Draw(Game1.menuTexture, new Rectangle(xPositionOnScreen + 32, yPositionForPartition, width - 64, 64),
            Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 25), Color.White);

        // Menu items Menu
        cafeStockInventory.draw(b);

        if (hoverText != null && (hoveredItem == null || cafeStockInventory == null))
        {
            if (hoverAmount > 0)
                drawToolTip(b, hoverText, "", null, heldItem: true, -1, 0, null, -1, null, moneyAmountToShowAtBottom: hoverAmount);
            else
                drawHoverText(b, hoverText, Game1.smallFont);
        }

        if (hoveredItem != null)
            drawToolTip(b, hoveredItem.getDescription(), hoveredItem.DisplayName, hoveredItem, heldItem != null);
        else if (hoveredItem != null && cafeStockInventory != null)
            drawToolTip(b, cafeStockInventory.descriptionText, cafeStockInventory.descriptionTitle,
                hoveredItem, heldItem != null);

        heldItem?.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 8, Game1.getOldMouseY() + 8), 1f);

        Game1.mouseCursorTransparency = 1f;
        drawMouse(b);
    }

    internal void AddToRecentItems(Item item)
    {
        if (recentlyAddedItemsList.Contains(item))
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
}
*/