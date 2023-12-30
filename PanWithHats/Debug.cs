using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;


namespace PanWithHats
{
    internal class Debug
    {
        public static void ButtonPress(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsMainPlayer || !Context.CanPlayerMove)
                return;

            switch (e.Button)
            {
                case SButton.NumPad0:
                    Point p = Game1.player.getTileLocationPoint() + new Point(0, 2);
                    Game1.currentLocation.orePanPoint.Set(p);
                    break; 
                case SButton.NumPad1:
                    Game1.player.CurrentTool = new Pan();
                    break;
                case SButton.NumPad2:
                    Game1.player.Items[Game1.player.CurrentToolIndex] = ModEntry.HatPlayerWasHolding;
                    break;
                default:
                    return;
            }
        }
    }
}
