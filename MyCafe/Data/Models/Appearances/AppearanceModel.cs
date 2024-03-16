using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using System.Collections.Generic;
using System.Linq;
using MyCafe.Characters;
using MyCafe.Data.Customers;
using StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

namespace MyCafe.Data.Models.Appearances;

public class AppearanceModel
{
    public IContentPack ContentPack { get; set; } = null!;
    public string Id { get; set; } = null!;
    public string TexturePath { get; set; } = null!;
    public string Gender { get; set; } = "any";
    public List<int[]> ColorMasks { get; set; } = [];
    public bool ConstantColor { get; set; } = false;

    internal Gender GetGender()
    {
        return Utility.CustomGenderToGameGender(this.Gender);
    }

    /// <summary>
    /// Returns true if the given color is in the model's ColorMasks list
    /// </summary>
    /// <remarks>From FashionSense by PeacefulEnd https://github.com/Floogen/FashionSense/blob/18f0a8b29dfdd1aeb8780440b7f3226275e0dcbb/FashionSense/Framework/Models/Appearances/AppearanceModel.cs#L84</remarks>
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

    internal bool MatchesGender(Gender gender)
    {
        Gender myGender = Utility.CustomGenderToGameGender(this.Gender);
        return (myGender == gender) || (gender == StardewValley.Gender.Undefined) || (myGender == StardewValley.Gender.Undefined);
    }
    
    internal IRawTextureData GetTexture(IList<Color>? colors)
    {
        IRawTextureData texData = this.ContentPack.ModContent.Load<IRawTextureData>(this.TexturePath);

        if (this.ConstantColor || colors?.Count != 3)
            return texData;

        Log.Trace($"Painting {this.GetType().Name} with color {colors[0]}, secondary {colors[1]}, multiplier {colors[2]}");
        this.Paint(texData.Data, colors[0], colors[1], colors[2]);
        return texData;
    }

    internal void Paint(Color[] texture, Color color, Color secondary, Color multiplier)
    {
        if (color == Color.Transparent)
            return;

        Dictionary<Color, double> luminosities = [];
        double colorLum = Utility.GetLuminosityBasicAlternative(color);
        double secondaryLum = Utility.GetLuminosityBasicAlternative(secondary);

        bool hasSecondary = secondary != Color.Transparent;
        bool hasMultiplier = multiplier != Color.Transparent;

        for (int i = 0; i < texture.Length; i++)
        {
            Color pixel = texture[i];

            if (pixel == Color.Transparent || this.IsMaskedColor(pixel))
                continue;

            if (!luminosities.TryGetValue(pixel, out double backLum))
            {
                backLum = Utility.GetLuminosityBasicAlternative(pixel);
                luminosities[pixel] = backLum;
            }

            int delta = (int) ((backLum - (hasSecondary ? secondaryLum : colorLum)) * 255);
            Color newColor = hasMultiplier ? this.CalculateNewColor(color, multiplier, delta) : this.CalculateNewColor(color, delta);
            texture[i] = newColor;
        }
    }

    private Color CalculateNewColor(Color color, Color m, int delta)
    {
        float[] multiply = [m.R / 255f, m.G / 255f, m.B / 255f];

        return new Color(
            r: (int) ((color.R + delta) * multiply[0]),
            g: (int) ((color.G + delta) * multiply[1]),
            b: (int) ((color.B + delta) * multiply[2]),
            alpha: color.A
            );
    }

    private Color CalculateNewColor(Color color, int delta)
    {
        return new Color(
            r: color.R + delta,
            g: color.G + delta,
            b: color.B + delta,
            alpha: color.A);
    }
}
