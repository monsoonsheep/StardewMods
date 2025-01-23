using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley.GameData.HomeRenovations;
using static StardewValley.Menus.CharacterCustomization;
using static StardewValley.Minigames.TargetGame;

namespace StardewMods.CustomFarmerAnimations.Framework
{
    public abstract class EditOperation
    {
        public abstract void Execute(IAssetDataForImage imageData);

        internal static Rectangle? ParseRectangle(string commaSeparatedRect)
        {
            try
            {
                int[] split = commaSeparatedRect.Split(',').Select(i => int.Parse(i)).ToArray();
                return new Rectangle(
                    split[0],
                    split[1],
                    split.Length > 2 ? split[2] : -1,
                    split.Length > 3 ? split[3] : -1);
            }
            catch (Exception e) {
                Log.Error("Error parsing move operation");
                return null;
            }
        }
    }
}
