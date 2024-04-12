using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using MonsoonSheep.Stardew.Common;
using MyCafe.Data.Models.Appearances;
using MyCafe.Enums;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;
using SUtility = StardewValley.Utility;
using SObject = StardewValley.Object;
using MyCafe.Locations.Objects;

namespace MyCafe;

internal static class ModUtility
{
    internal static SObject? GetSignboard()
    {
        SObject? found = null;

        SUtility.ForEachLocation(delegate(GameLocation loc)
        {
            foreach (SObject obj in loc.Objects.Values)
            {
                if (obj.QualifiedItemId.Equals($"(BC){ModKeys.CAFE_SIGNBOARD_OBJECT_ID}"))
                {
                    found = obj;
                    return false;
                }
            }

            return true;
        });

        return found;
    }

    internal static IEnumerable<Furniture> GetValidFurnitureInCafeLocations()
    {
        if (Mod.Cafe.Signboard?.Location is { } signboardLocation)
        {
            foreach (Furniture furniture in signboardLocation.furniture.Where(t => t.IsTable()))
            {
                if (signboardLocation.IsOutdoors && !Mod.Cafe.IsFurnitureWithinRangeOfSignboard(furniture))
                    continue;

                yield return furniture;
            }
        }
    }

    internal static bool IsChair(Furniture furniture)
    {
        return furniture.furniture_type.Value is 0 or 1 or 2;
    }

    internal static List<Item> ParseMenuItems(string[] ids)
    {
        List<Item> items = [];
        foreach (string id in ids)
        {
            Item? item = ItemRegistry.Create(id);
            if (item != null)
            {
                items.Add(item);
            }
        }

        return items;
    }

    internal static Gender GetRandomGender(bool binary = false)
    {
        if (binary)
            return (Game1.random.Next(2) == 0) ? Gender.Male : Gender.Female;
        
        return (Game1.random.Next(3)) switch
        {
            0 => Gender.Female,
            1 => Gender.Male,
            _ => Gender.Undefined
        };
    }

    internal static string GameGenderToCustomGender(Gender gender)
    {
        return (gender) switch
        {
            Gender.Male => "male",
            Gender.Female => "female",
            _ => "any"
        };
    }

    internal static Gender CustomGenderToGameGender(string gender)
    {
        return (gender.ToLower()) switch
        {
            "male" => Gender.Male,
            "female" => Gender.Female,
            _ => Gender.Undefined
        };
    }

    internal static float GetLuminosityBasic(Color color)
    {
        return (color.R / 255f) * 0.3f + (color.G / 255f) * 0.59f + (color.B / 255f) * 0.11f;
    }

    internal static float GetLuminosityBasicAlternative(Color color)
    {
        return Math.Max(Math.Min(0.2126f * (color.R / 255f) + 0.7152f * (color.G / 255f) + 0.0722f * (color.B / 255f), 1), 0);
    }

    internal static float GetLuminosityLinear(Color color)
    {
        float[] channels = [color.R / 255f, color.G / 255f, color.B / 255f];

        for (int i = 0; i < channels.Length; i++)
            channels[i] = channels[i] <= 0.04045f ? channels[i] / 12.92f : (float) Math.Pow((channels[i] + 0.055f) / 1.055f, 2.4f);
        
        return (0.2126f * channels[0]) + (0.7152f * channels[1]) + (0.0722f * channels[2]);
    }

    internal static string GetFileNameForAppearanceType<T>()
    {
        string filename = typeof(T).Name switch
        {
            nameof(HairModel) => "hair",
            nameof(ShirtModel) => "shirt",
            nameof(PantsModel) => "pants",
            nameof(OutfitModel) => "outfit",
            nameof(ShoesModel) => "shoes",
            nameof(AccessoryModel) => "accessory",
            _ => throw new ArgumentOutOfRangeException()
        };
        return filename;
    }

    internal static string GetFolderNameForAppearance<T>()
    {
        string folderName = typeof(T).Name switch
        {
            nameof(HairModel) => "Hairstyles",
            nameof(ShirtModel) => "Shirts",
            nameof(PantsModel) => "Pants",
            nameof(OutfitModel) => "Outfits",
            nameof(ShoesModel) => "Shoes",
            nameof(AccessoryModel) => "Accessories",
            _ => throw new ArgumentOutOfRangeException()
        };

        return folderName;
    }

    /// <summary>
    /// Alpha-blend the given raw texture data arrays over each other in order
    /// </summary>
    internal static Color[] CompositeTextures(List<Color[]> textures)
    {
        Color[] finalData = new Color[textures[0].Length];

        for (int i = 0; i < textures[0].Length; i++)
        {
            Color below = textures[0][i];

            int r = below.R;
            int g = below.G;
            int b = below.B;
            int a = below.A;

            foreach (Color[] layer in textures)
            {
                Color above = layer[i];

                float alphaBelow = 1 - (above.A / 255f);

                r = (int) (above.R + (r * alphaBelow));
                g = (int) (above.G + (g * alphaBelow));
                b = (int) (above.B + (b * alphaBelow));
                a = Math.Max(a, above.A);
            }

            finalData[i] = new Color(r, g, b, a);
        }

        return finalData;
    }

    internal static Rectangle GetEmojiSource(EmojiSprite type)
    {
        return type switch
        {
            EmojiSprite.Smiley => new Rectangle(0, 0, 9, 9),
            EmojiSprite.Disappointed => new Rectangle(82, 0, 9, 9),
            EmojiSprite.Heart => new Rectangle(9, 27, 9, 9),
            EmojiSprite.Time => new Rectangle(0, 27, 9, 9),
            EmojiSprite.Money => new Rectangle(118, 18, 8, 9),
            _ => new Rectangle(0, 0, 0, 0)
        };
    }

    internal static void DoEmojiSprite(Vector2 position, EmojiSprite type)
    {
        Rectangle source = ModUtility.GetEmojiSource(type);
        Game1.Multiplayer.broadcastSprites(
            Game1.player.currentLocation,
            new TemporaryAnimatedSprite("LooseSprites\\\\emojis",
                source,
                2000f,
                1,
                0,
                position + new Vector2(-13f, -64f),
                flicker: false,
                flipped: false,
                1f, 0f, Color.White, 4f, 0f, 0f, 0f)
            {
                motion = new Vector2(0f, -0.5f),
                alphaFade = 0.01f
            });
    }

    internal static void CleanUpCustomers()
    {
        SUtility.ForEachLocation((loc) =>
        {
            for (int i = loc.characters.Count - 1; i >= 0; i--)
            {
                NPC npc = loc.characters[i];
                if (npc.Name.StartsWith(ModKeys.CUSTOMER_NPC_NAME_PREFIX))
                {
                    loc.characters.RemoveAt(i);
                }
            }

            return true;
        });
    }

    internal static KeyValuePair<string, string> GetCustomDialogueAssetOrGeneric(string name, string key)
    {
        Dictionary<string, string> dialogueAsset = Game1.content.Load<Dictionary<string, string>>($"{ModKeys.MODASSET_CUSTOM_DIALOGUE}/{name}");
        if (!dialogueAsset.ContainsKey(key))
            dialogueAsset = Game1.content.Load<Dictionary<string, string>>($"{ModKeys.MODASSET_CUSTOM_DIALOGUE}/Generic");

        return dialogueAsset.Where(pair => pair.Key.StartsWith(key)).ToList().PickRandom();
    }

    internal static KeyValuePair<string, string>? GetCustomDialogueAsset(string name, string key)
    {
        try
        {
            return Game1.content.Load<Dictionary<string, string>>($"{ModKeys.MODASSET_CUSTOM_DIALOGUE}/{name}").Where(pair => pair.Key.StartsWith(key)).ToList().PickRandom();
        }
        catch
        {
            return null;
        }
    }
}
