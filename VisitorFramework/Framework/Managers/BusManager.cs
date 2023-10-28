using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Pathfinding;
using static VisitorFramework.Framework.Utility;


namespace VisitorFramework.Framework.Managers
{
    internal static class BusManager
    {
        internal static BusStop BusLocation;

        internal static bool BusLeaving;
        internal static bool BusReturning;
        internal static bool BusGone;

        internal static int[] BusDepartureTimes = {};
        internal static byte BusDeparturesToday;
        internal static int MinutesSinceBusLeft;

        internal static FieldInfo busMotion;
        internal static FieldInfo busPosition;
        internal static FieldInfo busDoor;

        internal static Point BusDoorPosition = new Point(12, 10);

        /// <summary>
        /// Call when all visitors have entered the bus and Pam is standing in position. This will make Pam get in and drive away without waiting for anyone
        /// </summary>
        internal static void BusLeave()
        {
            busMotion = typeof(BusStop).GetField("busMotion", BindingFlags.NonPublic | BindingFlags.Instance);
            busPosition = typeof(BusStop).GetField("busPosition", BindingFlags.NonPublic | BindingFlags.Instance);
            busDoor = typeof(BusStop).GetField("busDoor", BindingFlags.NonPublic | BindingFlags.Instance);

            if (busMotion == null || busPosition == null || busDoor == null)
            {
                Logger.Log("Bus can't leave", LogLevel.Error);
                return;
            }

            NPC pam = Game1.getCharacterFromName("Pam");
            pam.controller = new PathFindController(pam, BusLocation, new Point(12, 9), 3, PamReachedBus);
        }

        /// <summary>
        /// Start the bus driving back animation
        /// </summary>
        internal static void BusReturn()
        {
            TemporaryAnimatedSprite busDoorSprite = (TemporaryAnimatedSprite) busDoor.GetValue(BusLocation);
            if (busDoorSprite == null)
            {
                Logger.Log("Bus can't leave", LogLevel.Error);
                return;
            }

            Vector2 busPositionVal = (Vector2) busPosition.GetValue(BusLocation);

            busPosition.SetValue(BusLocation, new Vector2(BusLocation.map.RequireLayer("Back").DisplayWidth, busPositionVal.Y));
            busPositionVal = (Vector2) busPosition.GetValue(BusLocation);

            busDoorSprite.Position = busPositionVal + new Vector2(16f, 26f) * 4f;
            BusLocation.localSound("busDriveOff");
            busMotion.SetValue(BusLocation, new Vector2(-6f, 0f));

            BusReturning = true;
            BusLeaving = false;
            BusGone = false;
            MinutesSinceBusLeft = 0;
        }

        internal static void BusFinishReturning(bool animate = true)
        {
            Vector2 busPositionValue = (Vector2) busPosition.GetValue(BusLocation);
            busPositionValue.X = 704f;

            busMotion.SetValue(BusLocation, Vector2.Zero);

            if (animate)
            {
                // Animate bus door to open
                TemporaryAnimatedSprite busDoorAnimation = (TemporaryAnimatedSprite) busDoor.GetValue(BusLocation);
                if (busDoorAnimation != null)
                {
                    busDoorAnimation.Position = busPositionValue + new Vector2(16f, 26f) * 4f;
                    busDoorAnimation.pingPong = true;
                    busDoorAnimation.interval = 70f;
                    busDoorAnimation.currentParentTileIndex = 5;
                    busDoorAnimation.endFunction = BusDoorClose;
                    BusLocation.localSound("trashcanlid");
                }
            }
            else
            {
                BusDoorClose(0);
            }

            busPosition.SetValue(BusLocation, busPositionValue);
            BusReturning = false;

            // Pam walks back to position
            NPC pam = Game1.getCharacterFromName("Pam");
            pam.controller = new PathFindController(pam, BusLocation, new Point(11, 10), 2, PamBackToSchedule);
        }

        public static void PamBackToSchedule(Character pam, GameLocation BusStop)
        {
            // TODO: Edge cases for Pam going back to regular schedule
        }

        public static void PamReachedBus(Character pam, GameLocation busStop)
        {
            pam.position.X = -10000;

            Vector2 busPositionVal = (Vector2) busPosition.GetValue(BusLocation);
            busMotion.SetValue(BusLocation, new Vector2(0f, 0f));

            // Close door animation
            busDoor.SetValue(BusLocation, new TemporaryAnimatedSprite(
                "LooseSprites\\Cursors", 
                new Rectangle(288, 1311, 16, 38), 
                busPositionVal + new Vector2(16f, 26f) * 4f, 
                false, 0f, Color.White)
            {
                interval = 70f,
                animationLength = 6,
                holdLastFrame = true,
                layerDepth = (busPositionVal.X + 192f) / 10000f + 1E-05f,
                scale = 4f,
                timer = 0f,
                endFunction = delegate
                {
                    BusLocation.localSound("batFlap");
                    BusLocation.localSound("busDriveOff");
                },
                paused = false
            });

            BusLocation.localSound("trashcanlid");

            BusGone = false;
            BusReturning = false;
            BusLeaving = true;
            BusDeparturesToday++;
        }

        public static void BusDoorClose(int _)
        {
            // Reset bus door sprite
            Vector2 busPositionValue = (Vector2) busPosition.GetValue(BusLocation);
            busDoor.SetValue(BusLocation, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(288, 1311, 16, 38), busPositionValue + new Vector2(16f, 26f) * 4f, flipped: false, 0f, Color.White)
            {
                interval = 999999f,
                animationLength = 6,
                holdLastFrame = true,
                layerDepth = (busPositionValue.Y + 192f) / 10000f + 1E-05f,
                scale = 4f
            });

            // Spawn visitors
            VisitorManager.TrySpawnBusVisitors();
        }

        /// <summary>
        /// Get a list of positions that a group can convene on. After Visitors depart from the bus, they have to stand in a small area and look around for a few seconds. Those positions are based on the bus position.
        /// </summary>
        internal static List<Point> GetBusConvenePoints(int count)
        {
            var startingPoint = BusDoorPosition + new Point(0, Game1.random.Next(3, 6));
            var points = new List<Point>();

            for (var i = 1; i <= 4; i++)
            {
                points.Add(new Point(startingPoint.X, startingPoint.Y + i));
                points.Add(new Point(startingPoint.X + 1, startingPoint.Y + i));
            }

            return points.OrderBy(x => Game1.random.Next()).Take(count).OrderBy(p => -p.Y).ToList();
        }

        /// <summary>
        /// TODO: Call when bus stop is loaded
        /// </summary>
        internal static void UpdateBusDoorPosition()
        {
            var tiles = GetLocationFromName("BusStop").Map.GetLayer("Back").Tiles.Array;

            for (var i = 0; i < tiles.GetLength(0); i++)
            {
                for (var j = 0; j < tiles.GetLength(1); j++)
                {
                    if (tiles[i, j].Properties.ContainsKey("TouchAction") && tiles[i, j].Properties["TouchAction"] == "Bus")
                    {
                        BusDoorPosition = new Point(i, j + 1);
                        return;
                    }
                }
            }
            
            Logger.Log("Couldn't find Bus position in Bus Stop");
            BusDoorPosition = new Point(12, 10);
        }

        internal static void UpdateBusStopLocation()
        {
            BusLocation = (BusStop) Game1.getLocationFromName("BusStop");
        }

    }
}
