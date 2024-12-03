using Microsoft.Xna.Framework;

namespace StardewMods.VisitorsMod.Framework.Data.Models.Appearances;

public class AppearancePaint
{
    public int[] Main = [255, 255, 255];
    public int[] Secondary = [255, 255, 255];
    public int[][] Multipliers = [];

    internal Color[] GetRandomPermutation()
    {
        Color[] colors = new Color[3];

        colors[0] = new Color(this.Main[0], this.Main[1], this.Main[2]);
        colors[1] = new Color(this.Secondary[0], this.Secondary[1], this.Secondary[2]);

        int[]? multiplier = this.Multipliers.PickRandom();
        colors[2] = multiplier != null ? new Color(multiplier[0], multiplier[1], multiplier[2]) : Color.White;

        return colors;
    }
}
