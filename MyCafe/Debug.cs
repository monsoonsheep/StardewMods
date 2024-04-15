#if DEBUG

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MyCafe.Data.Customers;
using MyCafe.Enums;
using MyCafe.Locations.Objects;
using MyCafe.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Menus;
using StardewValley.Objects;

#pragma warning disable IDE0060

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
                Mod.Cafe.TrySpawnCustomers(GroupType.Random);

                break;
            case SButton.NumPad3:
                Mod.Cafe.RemoveAllCustomers();

                break;
            case SButton.NumPad4:
                Mod.Cafe.TrySpawnCustomers(GroupType.Villager);

                break;
            case SButton.NumPad5:
                Log.Trace("Breaking");

                break;
            case SButton.NumPad6:
                GivePlayerTheSignboard();

                break;
            case SButton.NumPad7:
                //Mod.Cafe.RequestNpcCustomer("Abigail");

                break;
            case SButton.NumPad8:
                Game1.activeClickableMenu = new CarpenterMenu("Robin");

                break;
            case SButton.NumPad9:
                ModUtility.DoEmojiSprite(Game1.player.Tile, EmojiSprite.Money);

                break;
            case SButton.U:
                OpenCafeMenu();

                break;
        }
    }

    internal static void GivePlayerTheSignboard()
    {
        Object signboard = ItemRegistry.Create<Object>($"(BC){ModKeys.CAFE_SIGNBOARD_OBJECT_ID}");
        Game1.player.addItemToInventory(signboard);
    }

    internal static void InvalidateIntroductionEvent()
    {
        //GameLocation eventLocation = Game1.locations.First(l => l.isBuildingConstructed(ModKeys.CAFE_SIGNBOARD_BUILDING_ID));
        //Mod.Instance.Helper.GameContent.InvalidateCache($"Data/Events/{eventLocation.Name}");
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

    internal static void WarpToBus()
    {
        Game1.warpFarmer("BusStop", 12, 15, false);
    }

    internal static void PrintAllInfo()
    {
        Log.Debug($"{Mod.Cafe.Tables.Count} tables");
    }

    internal static bool IsDebug()
    {
        #if DEBUG
        return true;
        #else
        return false;
        #endif
    }
}

#endif
