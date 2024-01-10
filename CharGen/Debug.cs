using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Linq;


namespace CharGen;
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
