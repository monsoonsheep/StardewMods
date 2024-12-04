using Microsoft.Xna.Framework;
using StardewMods.VisitorsMod.Framework.Data;
using StardewMods.VisitorsMod.Framework.Data.Models.Appearances;

namespace StardewMods.VisitorsMod.Framework.Services.Visitors.RandomVisitors;
internal class ColorsManager
{
    // Colors
    internal IRawTextureData BodyBase = null!;
    internal IRawTextureData Eyes = null!;
    internal List<int[]> SkinTones = [];
    internal List<int[]> EyeColors = [];
    internal List<AppearancePaint> HairColors = [];
    internal List<AppearancePaint> ShirtColors = [];
    internal List<AppearancePaint> PantsColors = [];
    internal List<AppearancePaint> OutfitColors = [];

    public ColorsManager()
    {
        this.BodyBase = Mod.Helper.ModContent.Load<IRawTextureData>(Path.Combine("assets", "CharGen", "base.png"));
        this.Eyes = Mod.Helper.ModContent.Load<IRawTextureData>(Path.Combine("assets", "CharGen", "eyes.png"));
        this.SkinTones = Mod.Helper.ModContent.Load<List<int[]>>(Path.Combine("assets", "CharGen", "skintones.json"));
        this.EyeColors = Mod.Helper.ModContent.Load<List<int[]>>(Path.Combine("assets", "CharGen", "eyecolors.json"));
        this.HairColors = Mod.Helper.ModContent.Load<List<AppearancePaint>>(Path.Combine("assets", "CharGen", "haircolors.json"));
        this.ShirtColors = Mod.Helper.ModContent.Load<List<AppearancePaint>>(Path.Combine("assets", "CharGen", "shirtcolors.json"));
        this.PantsColors = Mod.Helper.ModContent.Load<List<AppearancePaint>>(Path.Combine("assets", "CharGen", "pantscolors.json"));
    }

    /// <summary>
    /// Paint the body base texture with the skin tone set in this object and return it as raw texture data
    /// </summary>
    internal IRawTextureData PaintBody(Color skinTone)
    {
        Color[] data = new Color[this.BodyBase.Width * this.BodyBase.Height];

        float[] multiplier = [
            skinTone.R / 255f,
            skinTone.G / 255f,
            skinTone.B / 255f
        ];

        for (int i = 0; i < this.BodyBase.Data.Length; i++)
        {
            Color baseColor = this.BodyBase.Data[i];
            if (baseColor == Color.Transparent)
                continue;

            data[i] = new Color(
                (int)(baseColor.R * multiplier[0]),
                (int)(baseColor.G * multiplier[1]),
                (int)(baseColor.B * multiplier[2]),
                baseColor.A);
        }

        RawTextureData tex = new RawTextureData(data, this.BodyBase.Width, this.BodyBase.Height);
        return tex;
    }

    /// <summary>
    /// Get the eyes texture data with the given color applied
    /// </summary>
    internal IRawTextureData PaintEyes(Color color)
    {
        Color[] data = new Color[this.Eyes.Data.Length];

        float[] eyeColor = [
            color.R / 255f,
            color.G / 255f,
            color.B / 255f
        ];

        for (int i = 0; i < this.Eyes.Data.Length; i++)
        {
            Color baseColor = this.Eyes.Data[i];
            if (baseColor == Color.Transparent)
                continue;

            // Leave the bright white parts of the eye (don't color them)
            if (baseColor.R >= 200)
            {
                data[i] = baseColor;
            }
            else
            {
                data[i] = new Color(
                    (int)(baseColor.R * eyeColor[0]),
                    (int)(baseColor.G * eyeColor[1]),
                    (int)(baseColor.B * eyeColor[2]),
                    baseColor.A);
            }
        }

        RawTextureData tex = new RawTextureData(data, this.Eyes.Width, this.Eyes.Height);
        return tex;
    }

}
