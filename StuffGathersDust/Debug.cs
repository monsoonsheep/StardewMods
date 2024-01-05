using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;


namespace StuffGathersDust;

internal class Debug
{
    public static void ButtonPress(object sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.CanPlayerMove)
            return;

        switch (e.Button)
        {
            case SButton.NumPad0:
                break;
            default:
                return;
        }
    }
}