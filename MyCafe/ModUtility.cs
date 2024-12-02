using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using MonsoonSheep.Stardew.Common;
using Monsoonsheep.StardewMods.MyCafe.Data.Models.Appearances;
using Monsoonsheep.StardewMods.MyCafe.Enums;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;
using SUtility = StardewValley.Utility;
using SObject = StardewValley.Object;
using Monsoonsheep.StardewMods.MyCafe.Locations.Objects;
using xTile.Dimensions;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Monsoonsheep.StardewMods.MyCafe;

internal static class ModUtility
{
    internal static int RandomNumberOfSeatsForTable(int seatCount)
    {
        int random = Game1.random.Next(seatCount);

        return (seatCount) switch
        {
            1 => 1,
            2 or 3 => random == 0 ? seatCount : seatCount - 1,
            >=4 => random == 0 ? seatCount : Game1.random.Next(2, seatCount),
            _ => random == 0 ? seatCount : Game1.random.Next(3, seatCount)
        };
    }

    internal static bool IsChairHere(GameLocation location, Point tile)
    {
        return Mod.Cafe.Tables.SelectMany(t => t.Seats).Any(seat => seat.TilePosition.X == tile.X && seat.TilePosition.Y == tile.Y);

        // either theres a furniture there, or there's a nonpassable tile but not a door
        //return location.GetFurnitureAt(tile.ToVector2()) != null
        //       || (!location.isTilePassable(new Location(tile.X, tile.Y), Game1.viewport) &&
        //           location.isCollidingWithDoors(new Rectangle(tile.X * 64, tile.Y * 64, 64, 64)) == null);
    }
    
    internal static bool IsChair(Furniture furniture)
    {
        return furniture.furniture_type.Value is 0 or 1 or 2;
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
}
