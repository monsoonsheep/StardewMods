using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.Framework.Managers;
using StardewValley;
using StardewValley.Menus;

// ReSharper disable InconsistentNaming

namespace MyCafe.Framework.UI
{
    public sealed class CafeMenu : IClickableMenu
    {
        public const int stockTab = 0;
        public const int settingsTab = 1;
        public int currentTab;

        public string hoverText = "";
        public string descriptionText = "";

        public List<ClickableComponent> tabs = new List<ClickableComponent>();
        public List<IClickableMenu> pages = new List<IClickableMenu>();


        public CafeMenu()
            : base(
                Game1.uiViewport.Width / 2 - (800 + borderWidth * 2) / 2,
                Game1.uiViewport.Height / 2 - (600 + borderWidth * 2) / 2,
                800 + borderWidth * 2,
                600 + borderWidth * 2, showUpperRightCloseButton: true)
        {
            initializePages();
        }

        public void AddTabsToClickableComponents(IClickableMenu menu)
        {
            menu.allClickableComponents.AddRange(tabs);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);
            
            for (int i = 0; i < tabs.Count; i++)
            {
                if (tabs[i].containsPoint(x, y) && currentTab != i && pages[currentTab].readyToClose())
                {
                    changeTab(i);
                    return;
                }
            }

            pages[currentTab].receiveLeftClick(x, y);
        }

        private void initializePages()
        {
            tabs.Clear();
            pages.Clear();

            tabs.Add(new ClickableComponent(new Rectangle(xPositionOnScreen + 64, yPositionOnScreen + tabYPositionRelativeToMenuY + 64, 64, 64),
                "stock", "Menu")
            {
                myID = 12340,
                downNeighborID = 0,
                rightNeighborID = 12341,
                tryDefaultIfNoDownNeighborExists = true,
                fullyImmutable = true
            });
            pages.Add(new CafeStockPage(xPositionOnScreen + 16, yPositionOnScreen, width, height));

            tabs.Add(new ClickableComponent(new Rectangle(xPositionOnScreen + 128, yPositionOnScreen + tabYPositionRelativeToMenuY + 64, 64, 64),
                "config", "Settings")
            {
                myID = 12341,
                downNeighborID = 0,
                rightNeighborID = 12342,
                tryDefaultIfNoDownNeighborExists = true,
                fullyImmutable = true
            });
            pages.Add(new CafeConfigPage(xPositionOnScreen + 16, yPositionOnScreen, width, height));

            pages[currentTab].populateClickableComponentList();
            AddTabsToClickableComponents(pages[currentTab]);
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);
            initializePages();
        }

        public override void performHoverAction(int x, int y)
        {
            base.performHoverAction(x, y);
            hoverText = "";
            pages[currentTab].performHoverAction(x, y);
            foreach (ClickableComponent c in tabs)
            {
                if (c.containsPoint(x, y))
                {
                    hoverText = c.label;
                    break;
                }
            }
        }

        public int getTabNumberFromName(string name)
        {
            int whichTab = -1;
            switch (name)
            {
                case "stock":
                    whichTab = 0;
                    break;
                case "config":
                    whichTab = 1;
                    break;
            }

            return whichTab;
        }

        public override void update(GameTime time)
        {
            base.update(time);
            pages[currentTab].update(time);
        }

        public override void releaseLeftClick(int x, int y)
        {
            base.releaseLeftClick(x, y);
            pages[currentTab].releaseLeftClick(x, y);
        }

        public override void leftClickHeld(int x, int y)
        {
            base.leftClickHeld(x, y);
            pages[currentTab].leftClickHeld(x, y);
        }

        public override bool readyToClose()
        {
            return pages[currentTab].readyToClose();
        }

        public void changeTab(int whichTab, bool playSound = true)
        {
            currentTab = whichTab;
            width = 800 + IClickableMenu.borderWidth * 2;
            initializeUpperRightCloseButton();

            if (playSound)
            {
                Game1.playSound("smallSelect");
            }

            pages[currentTab].populateClickableComponentList();
            AddTabsToClickableComponents(pages[currentTab]);
            setTabNeighborsForCurrentPage();
            if (Game1.options.SnappyMenus)
            {
                snapToDefaultClickableComponent();
            }
        }

        public void setTabNeighborsForCurrentPage()
        {
            foreach (var t in tabs)
            {
                t.downNeighborID = -99999;
            }
        }

        public override void draw(SpriteBatch b)
        {
            Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, pages[currentTab].width, pages[currentTab].height, speaker: false, drawOnlyBox: true);
            b.End();

            b.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
            foreach (ClickableComponent c in tabs)
            {
                int sheetIndex = c.name switch
                {
                    "stock" => 0,
                    "config" => 0,
                    _ => 0
                };
                b.Draw(AssetManager.Instance.Sprites, new Vector2(c.bounds.X, c.bounds.Y + ((currentTab == getTabNumberFromName(c.name)) ? 8 : 0)),
                    new Rectangle(sheetIndex * 16, 0, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.0001f);
            }

            b.End();
            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);

            pages[currentTab].draw(b);

            if (!hoverText.Equals(""))
            {
                drawHoverText(b, hoverText, Game1.smallFont);
            }

            if (pages[currentTab].shouldDrawCloseButton())
                base.draw(b);

            if (!Game1.options.SnappyMenus && !Game1.options.hardwareCursor)
                drawMouse(b, ignore_transparency: true);
        }
    }
}