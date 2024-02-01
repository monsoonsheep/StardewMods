using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonsoonSheep.Stardew.Common.UI;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;

namespace MonsoonSheep.Stardew.Common
{
    /// <summary>Provides common utility methods for interacting with the game code shared by my various mods.</summary>
    internal static class CommonHelper
    {
        /*********
        ** Fields
        *********/
        /// <summary>A blank pixel which can be colorized and stretched to draw geometric shapes.</summary>
        private static readonly Lazy<Texture2D> LazyPixel = new(() =>
        {
            Texture2D pixel = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
            return pixel;
        });

        /// <summary>The width of the borders drawn by <see cref="DrawTab"/>.</summary>
        public const int ButtonBorderWidth = 4 * Game1.pixelZoom;


        /*********
        ** Accessors
        *********/
        /// <summary>A blank pixel which can be colorized and stretched to draw geometric shapes.</summary>
        public static Texture2D Pixel => LazyPixel.Value;

        /// <summary>The width of the horizontal and vertical scroll edges (between the origin position and start of content padding).</summary>
        public static readonly Vector2 ScrollEdgeSize = new(CommonSprites.Scroll.TopLeft.Width * Game1.pixelZoom, CommonSprites.Scroll.TopLeft.Height * Game1.pixelZoom);


        /*********
        ** Public methods
        *********/
        /****
        ** Enums
        ****/
        /// <summary>Get the values in an enum.</summary>
        /// <typeparam name="TValue">The enum value type.</typeparam>
        public static IEnumerable<TValue> GetEnumValues<TValue>() where TValue : struct
        {
            return Enum.GetValues(typeof(TValue)).Cast<TValue>();
        }

        /****
        ** Game
        ****/
        /// <summary>Get all game locations.</summary>
        /// <param name="includeTempLevels">Whether to include temporary mine/dungeon locations.</param>
        public static IEnumerable<GameLocation> GetLocations(bool includeTempLevels = false)
        {
            var locations = Game1.locations
                .Concat(
                    from location in Game1.locations
                    from indoors in location.GetInstancedBuildingInteriors()
                    select indoors
                );

            if (includeTempLevels)
                locations = locations.Concat(MineShaft.activeMines).Concat(VolcanoDungeon.activeLevels);

            return locations;
        }

        /// <summary>Get all game locations.</summary>
        public static GameLocation? GetLocation(string name)
        {
            return Game1.locations
                .Concat(
                    from location in Game1.locations
                    from indoors in location.GetInstancedBuildingInteriors()
                    select indoors
                ).FirstOrDefault(l => l.Name == name || l.NameOrUniqueName == name);
        }

        /// <summary>
        /// Get a vector in the direction of the given direction
        /// </summary>
        /// <param name="direction">Direction number, with 0 as up, 1 as right, 2 as down, and 3 as left</param>
        internal static Vector2 DirectionIntToDirectionVector(int direction)
        {
            return direction switch
            {
                0 => new Vector2(0, -1),
                1 => new Vector2(1, 0),
                2 => new Vector2(0, 1),
                3 => new Vector2(-1, 0),
                _ => new Vector2(0, 0)
            };
        }

        /// <summary>
        /// Get a direction number (0 as up, 1 as right, 2 as down, and 3 as left) that's the direction from the first position facing
        /// the second position
        /// </summary>
        /// <param name="startTile">The position to start from</param>
        /// <param name="facingTile">The position we're facing</param>
        internal static int DirectionIntFromVectors(Vector2 startTile, Vector2 facingTile)
        {
            int xDist = (int)Math.Abs(startTile.X - facingTile.X);
            int yDist = (int)Math.Abs(startTile.Y - facingTile.Y);

            if (yDist == 0 || xDist > yDist)
            {
                return startTile.X > facingTile.X ? 3 : 1;
            }
            else if (xDist == 0 || yDist > xDist)
            {
                return startTile.Y > facingTile.Y ? 0 : 2;
            }

            return -1;
        }

        /// <summary>Get a player's current tile position.</summary>
        /// <param name="player">The player to check.</param>
        public static Vector2 GetPlayerTile(Farmer? player)
        {
            Vector2 position = player?.Position ?? Vector2.Zero;
            return new Vector2((int)(position.X / Game1.tileSize), (int)(position.Y / Game1.tileSize)); // note: player.getTileLocationPoint() isn't reliable in many cases, e.g. right after a warp when riding a horse
        }

        /// <summary>Get whether an item ID is non-empty, ignoring placeholder values like "-1".</summary>
        /// <param name="itemId">The unqualified item ID to check.</param>
        /// <param name="allowZero">Whether to allow zero as a valid ID.</param>
        public static bool IsItemId(string itemId, bool allowZero = true)
        {
            return
                !string.IsNullOrWhiteSpace(itemId)
                && (
                    !int.TryParse(itemId, out int id)
                    || id >= (allowZero ? 0 : 1)
                );
        }

        /****
        ** Fonts
        ****/
        /// <summary>Get the dimensions of a space character.</summary>
        /// <param name="font">The font to measure.</param>
        public static float GetSpaceWidth(SpriteFont font)
        {
            return font.MeasureString("A B").X - font.MeasureString("AB").X;
        }

        /// <summary>Show an informational message to the player.</summary>
        /// <param name="message">The message to show.</param>
        /// <param name="duration">The number of milliseconds during which to keep the message on the screen before it fades (or <c>null</c> for the default time).</param>
        public static void ShowInfoMessage(string message, int? duration = null)
        {
            Game1.addHUDMessage(new HUDMessage(message, HUDMessage.error_type) { noIcon = true, timeLeft = duration ?? HUDMessage.defaultTime });
        }

        /// <summary>Show an error message to the player.</summary>
        /// <param name="message">The message to show.</param>
        public static void ShowErrorMessage(string message)
        {
            Game1.addHUDMessage(new HUDMessage(message, HUDMessage.error_type));
        }

        /// <summary>Calculate the outer dimension for a content box.</summary>
        /// <param name="contentSize">The size of the content within the box.</param>
        /// <param name="padding">The padding within the content area.</param>
        /// <param name="innerWidth">The width of the inner content area, including padding.</param>
        /// <param name="innerHeight">The height of the inner content area, including padding.</param>
        /// <param name="labelOuterWidth">The outer pixel width.</param>
        /// <param name="outerHeight">The outer pixel height.</param>
        /// <param name="borderWidth">The width of the left and right border textures.</param>
        /// <param name="borderHeight">The height of the top and bottom border textures.</param>
        public static void GetScrollDimensions(Vector2 contentSize, int padding, out int innerWidth, out int innerHeight, out int labelOuterWidth, out int outerHeight, out int borderWidth, out int borderHeight)
        {
            GetContentBoxDimensions(CommonSprites.Scroll.TopLeft, contentSize, padding, out innerWidth, out innerHeight, out labelOuterWidth, out outerHeight, out borderWidth, out borderHeight);
        }

        /// <summary>Calculate the outer dimension for a content box.</summary>
        /// <param name="topLeft">The source rectangle for the top-left corner of the content box.</param>
        /// <param name="contentSize">The size of the content within the box.</param>
        /// <param name="padding">The padding within the content area.</param>
        /// <param name="innerWidth">The width of the inner content area, including padding.</param>
        /// <param name="innerHeight">The height of the inner content area, including padding.</param>
        /// <param name="outerWidth">The outer pixel width.</param>
        /// <param name="outerHeight">The outer pixel height.</param>
        /// <param name="borderWidth">The width of the left and right border textures.</param>
        /// <param name="borderHeight">The height of the top and bottom border textures.</param>
        public static void GetContentBoxDimensions(Rectangle topLeft, Vector2 contentSize, int padding, out int innerWidth, out int innerHeight, out int outerWidth, out int outerHeight, out int borderWidth, out int borderHeight)
        {
            borderWidth = topLeft.Width * Game1.pixelZoom;
            borderHeight = topLeft.Height * Game1.pixelZoom;
            innerWidth = (int)(contentSize.X + padding * 2);
            innerHeight = (int)(contentSize.Y + padding * 2);
            outerWidth = innerWidth + borderWidth * 2;
            outerHeight = innerHeight + borderHeight * 2;
        }

        
        /****
        ** Error handling
        ****/
        /// <summary>Intercept errors thrown by the action.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="verb">The verb describing where the error occurred (e.g. "looking that up"). This is displayed on the screen, so it should be simple and avoid characters that might not be available in the sprite font.</param>
        /// <param name="action">The action to invoke.</param>
        /// <param name="onError">A callback invoked if an error is intercepted.</param>
        public static void InterceptErrors(this IMonitor monitor, string verb, Action action, Action<Exception>? onError = null)
        {
            monitor.InterceptErrors(verb, null, action, onError);
        }

        /// <summary>Intercept errors thrown by the action.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="verb">The verb describing where the error occurred (e.g. "looking that up"). This is displayed on the screen, so it should be simple and avoid characters that might not be available in the sprite font.</param>
        /// <param name="detailedVerb">A more detailed form of <see cref="verb"/> if applicable. This is displayed in the log, so it can be more technical and isn't constrained by the sprite font.</param>
        /// <param name="action">The action to invoke.</param>
        /// <param name="onError">A callback invoked if an error is intercepted.</param>
        public static void InterceptErrors(this IMonitor monitor, string verb, string? detailedVerb, Action action, Action<Exception>? onError = null)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                monitor.InterceptError(ex, verb, detailedVerb);
                onError?.Invoke(ex);
            }
        }

        /// <summary>Log an error and warn the user.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="ex">The exception to handle.</param>
        /// <param name="verb">The verb describing where the error occurred (e.g. "looking that up"). This is displayed on the screen, so it should be simple and avoid characters that might not be available in the sprite font.</param>
        /// <param name="detailedVerb">A more detailed form of <see cref="verb"/> if applicable. This is displayed in the log, so it can be more technical and isn't constrained by the sprite font.</param>
        public static void InterceptError(this IMonitor monitor, Exception ex, string verb, string? detailedVerb = null)
        {
            detailedVerb ??= verb;
            monitor.Log($"Something went wrong {detailedVerb}:\n{ex}", LogLevel.Error);
            ShowErrorMessage($"Huh. Something went wrong {verb}. The error log has the technical details.");
        }

        /****
        ** File handling
        ****/
        /// <summary>Remove one or more obsolete files from the mod folder, if they exist.</summary>
        /// <param name="mod">The mod for which to delete files.</param>
        /// <param name="relativePaths">The relative file path within the mod's folder.</param>
        public static void RemoveObsoleteFiles(IMod mod, params string[] relativePaths)
        {
            string basePath = mod.Helper.DirectoryPath;

            foreach (string relativePath in relativePaths)
            {
                string fullPath = Path.Combine(basePath, relativePath);
                if (File.Exists(fullPath))
                {
                    try
                    {
                        File.Delete(fullPath);
                        mod.Monitor.Log($"Removed obsolete file '{relativePath}'.");
                    }
                    catch (Exception ex)
                    {
                        mod.Monitor.Log($"Failed deleting obsolete file '{relativePath}':\n{ex}");
                    }
                }
            }
        }

        /// <summary>Get the MD5 hash for a file.</summary>
        /// <param name="absolutePath">The absolute file path.</param>
        public static string GetFileHash(string absolutePath)
        {
            using FileStream stream = File.OpenRead(absolutePath);
            using MD5 md5 = MD5.Create();

            byte[] hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
