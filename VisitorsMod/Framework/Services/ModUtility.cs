using Microsoft.Xna.Framework;
using StardewMods.VisitorsMod.Framework.Data.Models.Appearances;

namespace StardewMods.VisitorsMod.Framework.Services;

internal static class ModUtility
{
    internal static Gender GetRandomGender(bool binary = false)
    {
        if (binary)
            return Game1.random.Next(2) == 0 ? Gender.Male : Gender.Female;

        return Game1.random.Next(3) switch
        {
            0 => Gender.Female,
            1 => Gender.Male,
            _ => Gender.Undefined
        };
    }

    internal static string GameGenderToCustomGender(Gender gender)
    {
        return gender switch
        {
            Gender.Male => "male",
            Gender.Female => "female",
            _ => "any"
        };
    }

    internal static Gender CustomGenderToGameGender(string gender)
    {
        return gender.ToLower() switch
        {
            "male" => Gender.Male,
            "female" => Gender.Female,
            _ => Gender.Undefined
        };
    }

    internal static float GetLuminosityBasic(Color color)
    {
        return color.R / 255f * 0.3f + color.G / 255f * 0.59f + color.B / 255f * 0.11f;
    }

    internal static float GetLuminosityBasicAlternative(Color color)
    {
        return Math.Max(Math.Min(0.2126f * (color.R / 255f) + 0.7152f * (color.G / 255f) + 0.0722f * (color.B / 255f), 1), 0);
    }

    internal static float GetLuminosityLinear(Color color)
    {
        float[] channels = [color.R / 255f, color.G / 255f, color.B / 255f];

        for (int i = 0; i < channels.Length; i++)
            channels[i] = channels[i] <= 0.04045f ? channels[i] / 12.92f : (float)Math.Pow((channels[i] + 0.055f) / 1.055f, 2.4f);

        return 0.2126f * channels[0] + 0.7152f * channels[1] + 0.0722f * channels[2];
    }
}
