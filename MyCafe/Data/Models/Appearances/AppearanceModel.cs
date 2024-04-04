using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using System.Collections.Generic;
using System.Linq;

namespace MyCafe.Data.Models.Appearances;

public abstract class AppearanceModel
{
    public string Gender { get; set; } = "any";
    public List<int[]> ColorMasks { get; set; } = [];
    public bool ConstantColor { get; set; } = false;

    internal string Id { get; set; } = null!;
    internal string TexturePath { get; set; } = null!;
    internal IContentPack ContentPack { get; set; } = null!;

    /// <summary>
    /// Get the <see cref="StardewValley.Gender"/> of the model
    /// </summary>
    /// <returns></returns>
    internal Gender GetGender()
    {
        return ModUtility.CustomGenderToGameGender(this.Gender);
    }

    /// <summary>
    /// Returns true if the given color is in the model's ColorMasks list
    /// </summary>
    /// <remarks>From the mod FashionSense by PeacefulEnd https://github.com/Floogen/FashionSense/blob/18f0a8b29dfdd1aeb8780440b7f3226275e0dcbb/FashionSense/Framework/Models/Appearances/AppearanceModel.cs#L84</remarks>
    private bool IsMaskedColor(Color color)
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
        Gender myGender = ModUtility.CustomGenderToGameGender(this.Gender);
        return (myGender == gender) || (gender == StardewValley.Gender.Undefined) || (myGender == StardewValley.Gender.Undefined);
    }

    internal IRawTextureData GetTexture(Color color)
    {
        IRawTextureData texData = this.GetRawTexture();

        float[] multiply = [color.R / 255f, color.G / 255f, color.B / 255f];
        if (this.ConstantColor)
            return texData;

        for (int i = 0; i < texData.Data.Length; i++)
        {
            Color baseColor = texData.Data[i];
            if (baseColor == Color.Transparent || this.IsMaskedColor(baseColor))
                continue;

            texData.Data[i] = new Color(
                (int) (baseColor.R * multiply[0]),
                (int) (baseColor.G * multiply[1]),
                (int) (baseColor.B * multiply[2]),
                baseColor.A);
        }

        return texData;
    }

    /// <summary>
    /// Load the model's texture and color it with the given colors
    /// </summary>
    internal IRawTextureData GetRawTexture()
    {
        IRawTextureData texData = this.ContentPack.ModContent.Load<IRawTextureData>(this.TexturePath);
        return texData;
    }
}
