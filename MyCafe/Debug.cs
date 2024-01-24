﻿using System.IO;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.CustomerFactory;
using MyCafe.LiveChatIntegration;
using MyCafe.Locations;
using MyCafe.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace MyCafe;

internal class Debug
{
    public static void ButtonPress(object sender, ButtonPressedEventArgs e)
    {
        if (!Context.CanPlayerMove)
            return;

        switch (e.Button)
        {
            case SButton.NumPad0:
                Log.Debug($"We're in {Game1.player.currentLocation.Name} ({Game1.player.currentLocation.NameOrUniqueName}) ");
                Log.Debug(System.Type.GetType("CafeLocation, MyCafe")?.FullName);
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
                            Log.Debug($"pos {pos.ToString()}, Position: {customer.Position.ToString()}, viewport: {Game1.viewport.ToString()}");
                        }
                    }
                }
                break;
            case SButton.NumPad5:
                Mod.Cafe.OpeningTime.Set(700);
                Mod.Cafe.ClosingTime.Set(2500);
                break;
            case SButton.NumPad6:
                Log.LogWithHudMessage($"Cafe time: {Mod.Cafe.ClosingTime.Value}");
                break;
            case SButton.NumPad7:
                SetMenuItems();
                break;
            case SButton.NumPad8:
                Mod.Cafe.MenuItems["Test"].Add(ItemRegistry.Create("(O)201"));
                break;
            case SButton.NumPad9:
                break;
            case SButton.U:
                Mod.Sprites = Mod.ModHelper.ModContent.Load<Texture2D>("assets/sprites.png");
                Game1.activeClickableMenu = new CafeMenu();
                break;
            default:
                return;
        }
    }

    
#if YOUTUBE || TWITCH
    public static void RefreshChat()
    {
        Mod.Cafe.Customers.ChatCustomers = new ChatCustomerSpawner();
        Mod.Cafe.Customers.ChatCustomers.Initialize(Mod.ModHelper);
    }

    public static void Test_UserJoinChat()
    {
        var a = File.ReadAllText(Mod.ModHelper.DirectoryPath + "\\names.txt").Split('\n');
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
        Mod.Cafe.MenuItems.Clear();
        Mod.Cafe.MenuItems["Soups"] = [ItemRegistry.Create("(O)218"), ItemRegistry.Create("(O)199"), ItemRegistry.Create("(O)727"), ItemRegistry.Create("(O)730")];
        Mod.Cafe.MenuItems["Dessert"] = [ItemRegistry.Create("(O)211"), ItemRegistry.Create("(O)222"), ItemRegistry.Create("(O)232"), ItemRegistry.Create("(O)234")];
        Mod.Cafe.MenuItems["Beverages"] = [
            ItemRegistry.GetObjectTypeDefinition().CreateFlavoredJuice(ItemRegistry.Create<Object>("(O)613")), 
            ItemRegistry.GetObjectTypeDefinition().CreateFlavoredJuice(ItemRegistry.Create<Object>("(O)635")), 
            ItemRegistry.GetObjectTypeDefinition().CreateFlavoredJuice(ItemRegistry.Create<Object>("(O)637"))
        ];
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