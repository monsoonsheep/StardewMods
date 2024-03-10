using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace MyCafe.Data.Models.Appearances;

public class AppearanceModel
{
    public string Id { get; set; } = null!;
    public string TexturePath { get; set; } = null!;
    public string Gender { get; set; } = "any";
    public List<int[]> ColorMasks { get; set; } = [];
    public bool ConstantColor { get; set; } = false;

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
}
