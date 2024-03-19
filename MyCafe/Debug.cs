using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MyCafe.Enums;
using MyCafe.Locations.Objects;
using MyCafe.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace MyCafe;

internal static class Debug
{
    public static void ButtonPress(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.CanPlayerMove)
            return;

        switch (e.Button)
        {
            case SButton.NumPad0:
                break;
            case SButton.NumPad1:
                WarpToBus();
                break;
            case SButton.NumPad2:
                SpawnCustomers(GroupType.Random);
                break;
            case SButton.NumPad3:
                Mod.Cafe.RemoveAllCustomers();
                break;
            case SButton.NumPad4:
                SpawnCustomers(GroupType.Villager);
                break;
            case SButton.NumPad5:
                Log.Trace("Breaking");
                break;
            case SButton.NumPad6:
                break;
            case SButton.NumPad7:
                SetMenuItems();
                break;
            case SButton.NumPad8:
                break;
            case SButton.NumPad9:
                break;
            case SButton.U:
                OpenCafeMenu();
                break;
            default:
                return;
        }
    }

    internal static void SpawnCustomers(GroupType type)
    {
        Table? table = Mod.Cafe.GetFreeTable();
        if (table != null)
            if (type == GroupType.Villager)
                Mod.Cafe.VillagerCustomers.Spawn(table);
            else
                Mod.Cafe.RandomCustomers.Spawn(table);
    }

    internal static void OpenCafeMenu()
    {
        Game1.activeClickableMenu = new CafeMenu();
    }

    internal static void OpenCarpenterMenu()
    {
        Game1.activeClickableMenu = new CarpenterMenu("Robin");
    }

    internal static Item SetTestItemForOrder(NPC customer)
    {
        return ItemRegistry.Create<Object>("(O)128");
    }

    internal static void SetMenuItems()
    {
        Mod.Cafe.Menu.AddCategory("Soups");
        Mod.Cafe.Menu.AddCategory("Dessert");

        List<Item> soups = [ItemRegistry.Create("(O)218"), ItemRegistry.Create("(O)199"), ItemRegistry.Create("(O)727"), ItemRegistry.Create("(O)730")];
        List<Item> dessert = [ItemRegistry.Create("(O)211"), ItemRegistry.Create("(O)222"), ItemRegistry.Create("(O)232"), ItemRegistry.Create("(O)234")];

        foreach (Item s in soups)
        {
            Mod.Cafe.Menu.AddItem(s, "Soups");
        }
        foreach (Item d in dessert)
        {
            Mod.Cafe.Menu.AddItem(d, "Dessert");
        }
    }

    internal static bool Wait10Seconds()
    {
        Task.Delay(3000);
        Log.Info("Connected!");
        return true;
    }

    internal static void WarpToBus()
    {
        Game1.warpFarmer("BusStop", 12, 15, false);
    }
}
