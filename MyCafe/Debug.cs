using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MyCafe.Characters.Spawning;
using MyCafe.Locations.Objects;
using MyCafe.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

#if YOUTUBE || TWITCH
using System.IO;
using MyCafe.LiveChatIntegration;
#endif

namespace MyCafe;

internal class Debug
{
    public static void ButtonPress(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.CanPlayerMove)
            return;

        switch (e.Button)
        {
            case SButton.NumPad0:
                Game1.activeClickableMenu = new CarpenterMenu("Robin");
                break;
            case SButton.NumPad1:
                WarpToBus();
                break;
            case SButton.NumPad2:
                if (Context.IsMainPlayer)
                    Mod.Cafe.Customers.SpawnCustomers();
                break;
            case SButton.NumPad3:
                Mod.Cafe.Customers.RemoveAllCustomers();
                //Mod.Cafe.PopulateTables();
                break;
            case SButton.NumPad4:
                foreach (var table in Mod.Cafe.Tables)
                {
                    foreach (Seat seat in table.Seats)
                    {
                        if (seat.ReservingCustomer is { ItemToOrder.Value: not null } customer)
                        {
                            Vector2 pos = customer.getLocalPosition(Game1.viewport);
                            pos.Y -= 32 + customer.Sprite.SpriteHeight * 3;
                            Log.Debug($"pos {pos}, Position: {customer.Position}, viewport: {Game1.viewport}");
                        }
                    }
                }
                break;
            case SButton.NumPad5:
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
                Game1.activeClickableMenu = new CafeMenu(Mod.Sprites);
                break;
            default:
                return;
        }
    }


#if YOUTUBE || TWITCH
    public static void RefreshChat()
    {
        Mod.Cafe.Customers.ChatCustomers = new ChatCustomerSpawner();
        Mod.Cafe.Customers.ChatCustomers.Initialize(Mod.Instance.Helper);
    }

    public static void Test_UserJoinChat()
    {
        string[] a = File.ReadAllText(Mod.Instance.Helper.DirectoryPath + "\\names.txt").Split('\n');
        string name = a[Game1.random.Next(a.Length)].TrimEnd('\r').TrimStart();
        (Mod.Cafe.Customers.ChatCustomers as ChatCustomerSpawner)?.OnChatMessageReceived(Mod.Cafe.Customers.ChatCustomers, new ChatMessageReceivedEventArgs()
        {
            Username = name,
            Message = "!join"
        });
    }
#endif

    public static void SetMenuItems()
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

        //Mod.Cafe.Items.Value.NetItems.Value.Add(ItemRegistry.Create("(O)219"));
        //Mod.Cafe.Items.Value.NetItems.Value.Add(ItemRegistry.Create("(O)129"));
        //Mod.Cafe.MenuItems.Clear();
        //Mod.Cafe.MenuItems["Soups"] = [ItemRegistry.Create("(O)218"), ItemRegistry.Create("(O)199"), ItemRegistry.Create("(O)727"), ItemRegistry.Create("(O)730")];
        //Mod.Cafe.MenuItems["Dessert"] = [ItemRegistry.Create("(O)211"), ItemRegistry.Create("(O)222"), ItemRegistry.Create("(O)232"), ItemRegistry.Create("(O)234")];
        //Mod.Cafe.MenuItems["Beverages"] = [
        //    ItemRegistry.GetObjectTypeDefinition().CreateFlavoredJuice(ItemRegistry.Create<Object>("(O)613")),
        //    ItemRegistry.GetObjectTypeDefinition().CreateFlavoredJuice(ItemRegistry.Create<Object>("(O)635")),
        //    ItemRegistry.GetObjectTypeDefinition().CreateFlavoredJuice(ItemRegistry.Create<Object>("(O)637"))
        //];
    }

    public static bool Wait10Seconds()
    {
        Task.Delay(3000);
        Log.Info("Connected!");
        return true;
    }

    public static void WarpToBus()
    {
        Game1.warpFarmer("BusStop", 12, 15, false);
    }
}
