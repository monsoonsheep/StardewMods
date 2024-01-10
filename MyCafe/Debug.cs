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
                Mod.Cafe.Customers.SpawnBusCustomers();
                break;
            case SButton.NumPad4:
                Mod.Cafe.ClosingTime.Set(2400);
                break; 
            case SButton.NumPad5:
                Mod.Cafe.ClosingTime.Set(2500);
                break;
            case SButton.NumPad6:
                Log.LogWithHudMessage($"Cafe time: {Mod.Cafe.ClosingTime.Value}");
                break;
            default:
                return;
        }
    }

    public static void WarpToBus()
    {
        Game1.warpFarmer("BusStop", 12, 15, false);
    }
}