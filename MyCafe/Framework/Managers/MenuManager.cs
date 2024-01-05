using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using System.Collections.Generic;
using System.Linq;

namespace MyCafe.Framework.Managers;

internal sealed class MenuManager
{
    internal static MenuManager Instance;

    internal readonly IList<Item> menuItems = new List<Item>(new Item[27]);
    internal readonly IList<Item> recentlyAddedMenuItems = new List<Item>(new Item[9]);

    internal MenuManager() => Instance = this;

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
