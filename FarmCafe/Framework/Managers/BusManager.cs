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

namespace FarmCafe.Framework.Managers
{
    internal class BusManager
    {
        internal static BusStop BusLocation;

        internal static bool BusLeaving;
        internal static bool BusReturning;
        internal static bool BusGone;

        internal static int[] BusDepartureTimes = {};
        internal static byte BusDeparturesToday = 0;
        internal static int MinutesSinceBusLeft = 0;

        internal static FieldInfo busMotion;
        internal static FieldInfo busPosition;
        internal static FieldInfo busDoor;

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
                    busDoorAnimation.endFunction = ResetBusDoor;
                    BusLocation.localSound("trashcanlid");
                }
            }
            else
            {
                ResetBusDoor(0);
            }

            busPosition.SetValue(BusLocation, busPositionValue);
            BusReturning = false;

            // Pam walks back to position
            NPC pam = Game1.getCharacterFromName("Pam");
            pam.controller = new PathFindController(pam, BusLocation, new Point(11, 10), 2, PamBackToSchedule);
        }

        public static void ResetBusDoor(int _)
        {
            Vector2 busPositionValue = (Vector2) busPosition.GetValue(BusLocation);
            busDoor.SetValue(BusLocation, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(288, 1311, 16, 38), busPositionValue + new Vector2(16f, 26f) * 4f, flipped: false, 0f, Color.White)
            {
                interval = 999999f,
                animationLength = 6,
                holdLastFrame = true,
                layerDepth = (busPositionValue.Y + 192f) / 10000f + 1E-05f,
                scale = 4f
            });
        }

        internal static void UpdateBusStopLocation()
        {
            BusLocation = (BusStop) Game1.getLocationFromName("BusStop");
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
    }
}
