#region Usings

using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

#endregion

namespace VisitorFramework;
#if DEBUG
internal static class Debug
{
    public static void ButtonPress(object sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.CanPlayerMove)
            return;

        switch (e.Button)
        {
            case SButton.NumPad0:
                break;
            case SButton.NumPad1:
                WarpToBus();
                break;
            case SButton.NumPad2:
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
#endif