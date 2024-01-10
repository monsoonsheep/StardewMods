using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using System.Collections.Generic;
using System.Linq;

namespace MyCafe.Managers;

internal sealed class MenuManager
{
    internal static MenuManager Instance;

    internal readonly IList<Item> MenuItems = new List<Item>(new Item[27]);
    internal readonly IList<Item> RecentlyAddedMenuItems = new List<Item>(new Item[9]);

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
        if (MenuItems.Any(x => x.ItemId == itemToAdd.ItemId))
            return false;

        for (int i = 0; i < MenuItems.Count; i++)
        {
            if (MenuItems[i] == null)
            {
                MenuItems[i] = itemToAdd.getOne();
                MenuItems[i].Stack = 1;
                return true;
            }
        }

        return false;
    }

    public Item RemoveFromMenu(int slotNumber)
    {
        Item tmp = MenuItems[slotNumber];
        if (tmp == null)
            return null;

        MenuItems[slotNumber] = null;
        int firstEmpty = slotNumber;
        for (int i = slotNumber + 1; i < MenuItems.Count; i++)
        {
            if (MenuItems[i] != null)
            {
                MenuItems[firstEmpty] = MenuItems[i];
                MenuItems[i] = null;
                firstEmpty += 1;
            }
        }

        return tmp;
    }
}
