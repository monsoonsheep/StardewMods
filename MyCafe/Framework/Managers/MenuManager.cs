using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MyCafe.Framework.UI;
using StardewModdingAPI;
using StardewValley;

namespace MyCafe.Framework.Managers
{
    internal sealed class MenuManager
    {
        internal static MenuManager Instance;

        internal readonly IList<Item> menuItems;
        internal readonly IList<Item> recentlyAddedMenuItems;

        internal IEnumerable<Item> MenuItems 
            => menuItems.Where(i => i != null);

        internal IEnumerable<Item> RecentlyAddedMenuItems 
            => recentlyAddedMenuItems.Where(i => i != null);

        internal MenuManager()
        {
            menuItems = new List<Item>(new Item[27]);
            recentlyAddedMenuItems = new List<Item>(new Item[9]);

            Instance = this;
        }

        internal bool OpenCafeMenuTileAction(GameLocation location, string[] args, Farmer player, Point tile)
        {
            if (!Context.IsMainPlayer)
                return false;

            if (Game1.activeClickableMenu == null && Context.IsPlayerFree)
            {
                Log.Debug("Opened cafe menu menu!");
                // Game1.activeClickableMenu = new CafeMenu();
            }

            return true;
        }

        public bool AddToMenu(Item itemToAdd)
        {
            if (menuItems.Any(x => x.ItemId == itemToAdd.ItemId))
                return false;

            for (int i = 0; i < menuItems.Count; i++)
            {
                if (menuItems[i] == null)
                {
                    menuItems[i] = itemToAdd.getOne();
                    menuItems[i].Stack = 1;
                    return true;
                }
            }

            return false;
        }

        public Item RemoveFromMenu(int slotNumber)
        {
            Item tmp = menuItems[slotNumber];
            if (tmp == null)
                return null;

            menuItems[slotNumber] = null;
            int firstEmpty = slotNumber;
            for (int i = slotNumber + 1; i < menuItems.Count; i++)
            {
                if (menuItems[i] != null)
                {
                    menuItems[firstEmpty] = menuItems[i];
                    menuItems[i] = null;
                    firstEmpty += 1;
                }
            }

            return tmp;
        }
    }
}
