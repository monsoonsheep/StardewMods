using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;
using SUtility = StardewValley.Utility;
using SObject = StardewValley.Object;
using xTile.Dimensions;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using StardewMods.FoodJoints.Framework.Enums;

namespace StardewMods.FoodJoints.Framework;

internal static class ModUtility
{
    internal static int RandomNumberOfSeatsForTable(int seatCount)
    {
        int random = Game1.random.Next(seatCount);

        return seatCount switch
        {
            1 => 1,
            2 or 3 => random == 0 ? seatCount : seatCount - 1,
            >= 4 => random == 0 ? seatCount : Game1.random.Next(2, seatCount),
            _ => random == 0 ? seatCount : Game1.random.Next(3, seatCount)
        };
    }

    internal static bool IsChairHere(GameLocation location, Point tile)
    {
        return Mod.NetState.Tables.SelectMany((t) => t.Seats).Any((seat) => seat.TilePosition.X == tile.X && seat.TilePosition.Y == tile.Y);

        // either theres a furniture there, or there's a nonpassable tile but not a door
        //return location.GetFurnitureAt(tile.ToVector2()) != null
        //       || (!location.isTilePassable(new Location(tile.X, tile.Y), Game1.viewport) &&
        //           location.isCollidingWithDoors(new Rectangle(tile.X * 64, tile.Y * 64, 64, 64)) == null);
    }

    internal static bool IsChair(Furniture furniture)
    {
        return furniture.furniture_type.Value is 0 or 1 or 2;
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
        Rectangle source = GetEmojiSource(type);
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
