using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using System.Collections.Generic;
using System.Linq;

namespace MyCafe.Data.Models.Appearances;

public class AppearanceModel
{
    public IContentPack ContentPack { get; set; } = null!;
    public string Id { get; set; } = null!;
    public string TexturePath { get; set; } = null!;
    public string Gender { get; set; } = "any";
    public List<int[]> ColorMasks { get; set; } = [];
    public bool ConstantColor { get; set; } = false;

    private const int LUMINOSITY_THRESHOLD_FOR_SECONDARY_COLOR = 74;

    /// <summary>
    /// Get the <see cref="StardewValley.Gender"/> of the model
    /// </summary>
    /// <returns></returns>
    internal Gender GetGender()
    {
        return Utility.CustomGenderToGameGender(this.Gender);
    }

    /// <summary>
    /// Returns true if the given color is in the model's ColorMasks list
    /// </summary>
    /// <remarks>From the mod FashionSense by PeacefulEnd https://github.com/Floogen/FashionSense/blob/18f0a8b29dfdd1aeb8780440b7f3226275e0dcbb/FashionSense/Framework/Models/Appearances/AppearanceModel.cs#L84</remarks>
    internal bool IsMaskedColor(Color color)
    {
        foreach (Color maskedColor in this.ColorMasks.Select(c => new Color(c[0], c[1], c[2], c.Length > 3 ? c[3] : 255)))
        {
            if (maskedColor == color)
            {
                return true;
            }

            if (maskedColor.A is not (byte.MinValue or byte.MaxValue))
            {
                // Premultiply the color for the mask, as SMAPI premultiplies the alpha
                Color adjustedColor = new Color(maskedColor.R * maskedColor.A / 255, maskedColor.G * maskedColor.A / 255, maskedColor.B * maskedColor.A / 255, maskedColor.A);
                if (adjustedColor == color)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Returns true if the given gender should be used with this model
    /// </summary>
    internal bool MatchesGender(Gender gender)
    {
        Gender myGender = Utility.CustomGenderToGameGender(this.Gender);
        return (myGender == gender) || (gender == StardewValley.Gender.Undefined) || (myGender == StardewValley.Gender.Undefined);
    }

    /// <summary>
    /// Load the model's texture and color it with the given colors
    /// </summary>
    /// <param name="colors">An array of 3 colors: The main color, the secondary darker color, and the multiplier, in that order</param>
    internal IRawTextureData GetTextureWithPaint(IList<Color>? colors)
    {
        IRawTextureData texData = this.ContentPack.ModContent.Load<IRawTextureData>(this.TexturePath);

        if (this.ConstantColor || colors?.Count != 3 || colors[0] == Color.Transparent)
            return texData;

        Log.Trace($"Painting {this.GetType().Name} with color {colors[0]}, secondary {colors[1]}, multiplier {colors[2]}");

        Color color = colors[0];
        Color secondary = colors[1];
        Color multiplier = colors[2];

        Dictionary<Color, float> luminosities = [];
        float colorLum = Utility.GetLuminosityBasicAlternative(color);
        float secondaryLum = Utility.GetLuminosityBasicAlternative(secondary);

        bool hasSecondary = secondary != Color.Transparent;
        if (multiplier == Color.Transparent)
            multiplier = Color.White;

        float[] multiply = [multiplier.R / 255f, multiplier.G / 255f, multiplier.B / 255f];

        for (int i = 0; i < texData.Data.Length; i++)
        {
            Color pixel = texData.Data[i];

            if (pixel == Color.Transparent || this.IsMaskedColor(pixel))
                continue;

            if (!luminosities.TryGetValue(pixel, out float backLum))
            {
                backLum = Utility.GetLuminosityBasicAlternative(pixel);
                luminosities[pixel] = backLum;
            }

            int delta = (int) ((backLum - ((hasSecondary && backLum < LUMINOSITY_THRESHOLD_FOR_SECONDARY_COLOR) ? secondaryLum : colorLum)) * 255);
            Color newColor = calculateNewColor(color, multiply, delta);
            texData.Data[i] = newColor;
        }

        return texData;

        static Color calculateNewColor(Color c, float[] m, int delta)
        {
            return new Color(
                r: (int) ((c.R + delta) * m[0]),
                g: (int) ((c.G + delta) * m[1]),
                b: (int) ((c.B + delta) * m[2]),
                alpha: c.A
            );
        }
    }

    internal IRawTextureData GetTextureWithMultiplyColors(Color color)
    {
        IRawTextureData texData = this.ContentPack.ModContent.Load<IRawTextureData>(this.TexturePath);

        float[] multiply = [color.R / 255f, color.G / 255f, color.B / 255f];
        if (this.ConstantColor)
            return texData;

        for (int i = 0; i < texData.Data.Length; i++)
        {
            Color c = texData.Data[i];
            if (c == Color.Transparent)
                continue;

            texData.Data[i] = new Color(
                (int) (c.R * multiply[0]),
                (int) (c.G * multiply[1]),
                (int) (c.B * multiply[2]),
                c.A);
        }

        return texData;
    }

    /// <summary>
    /// Load the model's texture and color it with the given colors
    /// </summary>
    internal IRawTextureData GetTextureNoColor()
    {
        IRawTextureData texData = this.ContentPack.ModContent.Load<IRawTextureData>(this.TexturePath);
        return texData;
    }
}
