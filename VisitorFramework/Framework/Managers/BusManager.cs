#region Usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Pathfinding;
using static VisitorFramework.Framework.Utility;
#endregion


namespace VisitorFramework.Framework.Managers
{
    internal static class BusManager
    {
        internal static BusStop BusLocation;

        internal static bool BusLeaving;
        internal static bool BusReturning;
        internal static bool BusGone;
        internal static int[] BusArrivalTimes = { 630, 1200, 1500, 1800, 2400 };
        internal static byte BusArrivalsToday;

        internal static IReflectedField<Vector2> BusMotionField;
        internal static IReflectedField<Vector2> BusPositionField;
        internal static IReflectedField<TemporaryAnimatedSprite> BusDoorField;

        internal static Point BusDoorPosition = new Point(12, 10);

        internal static EventHandler BusDoorOpen;

        internal static Vector2 BusPosition
        {
            get => BusPositionField.GetValue();
            set => BusPositionField.SetValue(value);
        }

        internal static Vector2 BusMotion
        {
            get => BusMotionField.GetValue();
            set => BusMotionField.SetValue(value);
        }

        internal static TemporaryAnimatedSprite BusDoor
        {
            get => BusDoorField.GetValue();
            set => BusDoorField.SetValue(value);
        }

        internal static int NextArrivalTime => BusArrivalTimes[BusArrivalsToday];
        internal static int LastArrivalTime => BusArrivalsToday == 0 ? -1 : BusArrivalTimes[BusArrivalsToday - 1];

        /// <summary>
        /// Update state of bus at the start of the day
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void DayUpdate(IModHelper helper)
        {
            UpdateBusStopLocation(helper);
        }

        /// <summary>
        /// Check every 10-minute update to make the bus leave or return
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void TenMinuteUpdate(object sender, TimeChangedEventArgs e)
        {
            if (e.NewTime == 610 || StardewValley.Utility.CalculateMinutesBetweenTimes(e.NewTime, LastArrivalTime) == -20)
            {
                BusLeave();
            }
            else if (StardewValley.Utility.CalculateMinutesBetweenTimes(e.NewTime, NextArrivalTime) == 10)
            {
                BusReturn();
            }
        }

        /// <summary>
        /// Call when all visitors have entered the bus and Pam is standing in position. This will make Pam get in and drive away
        /// </summary>
        internal static void BusLeave()
        {
            NPC pam = Game1.getCharacterFromName("Pam");

            if (BusLocation.characters.Contains(pam) && pam.TilePoint is { X: 11, Y: 10 })
                pam.temporaryController = new PathFindController(pam, BusLocation, new Point(12, 9), 3, BusDoorAnimateClose);
            else
                BusDoorAnimateClose(null, BusLocation);

            if (BusLocation.farmers.Count > 0)
            {
                // The UpdateWhenCurrentLocation postfix will handle the movement
                BusLeaving = true;
            }
            else
            {
                BusMotion = Vector2.Zero;
                BusPosition = new Vector2(-1000f, BusPosition.Y);
                BusLeaving = false;
                BusGone = true;
            }

            BusReturning = false;
        }

        /// <summary>
        /// Start the bus driving back animation
        /// </summary>
        internal static void BusReturn()
        {
            BusPosition = new Vector2(BusLocation.map.RequireLayer("Back").DisplayWidth, BusPosition.Y);
            if (BusDoor != null) 
                BusDoor.Position = BusPosition + new Vector2(16f, 26f) * 4f;
            BusMotion = new Vector2(-6f, 0f);

            BusLocation.localSound("busDriveOff");


            if (BusLocation.farmers.Count > 0)
            {
                // The UpdateWhenCurrentLocation postfix will handle the movement
                BusReturning = true;
            }
            else
            {
                StopBus();
                OpenDoor();
            }

            BusLeaving = false;
            BusGone = false;
            BusArrivalsToday++;
        }

        /// <summary>
        /// After the bus is stopped, we open the door and return the driver
        /// </summary>
        /// <param name="animate">Whether or not to animate the door opening</param>
        internal static void StopBus()
        {
            BusPosition = new Vector2(704f, BusPosition.Y);
            BusMotion = Vector2.Zero;
            BusReturning = false;
        }

        internal static void OpenDoor()
        {
            // Animate bus door to open
            TemporaryAnimatedSprite busDoorSprite = BusDoor;

            if (busDoorSprite != null)
            {
                busDoorSprite.Position = BusPosition + new Vector2(16f, 26f) * 4f;
                busDoorSprite.pingPong = true;
                busDoorSprite.interval = 70f;
                busDoorSprite.currentParentTileIndex = 5;
                busDoorSprite.endFunction = (_) => BusDoorOpen.Invoke(null, EventArgs.Empty);
                BusLocation.localSound("trashcanlid");

                BusDoor = busDoorSprite;
            }
            else
                BusDoorOpen.Invoke(null, EventArgs.Empty);
        }

        internal static void OnBusDoorOpen(object sender, EventArgs e)
        {
            // Reset bus door sprite
            BusDoorField.SetValue(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(288, 1311, 16, 38), BusPosition + new Vector2(16f, 26f) * 4f, flipped: false, 0f, Color.White)
            {
                interval = 999999f,
                animationLength = 6,
                holdLastFrame = true,
                layerDepth = (BusPosition.Y + 192f) / 10000f + 1E-05f,
                scale = 4f
            });

            NPC pam = Game1.getCharacterFromName("Pam");
            if (BusLocation.characters.Contains(pam)) 
                pam.temporaryController = new PathFindController(pam, BusLocation, new Point(11, 10), 2, PamBackToSchedule);
        }

        internal static void UpdateBusStopLocation(IModHelper helper)
        {
            BusLocation = (BusStop)Game1.getLocationFromName("BusStop");
            BusPositionField = helper.Reflection.GetField<Vector2>(BusLocation, "busPosition");
            BusMotionField = helper.Reflection.GetField<Vector2>(BusLocation, "busMotion");
            BusDoorField = helper.Reflection.GetField<TemporaryAnimatedSprite>(BusLocation, "busDoor");
            var tiles = BusLocation.Map.GetLayer("Back").Tiles.Array;

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

            Log.Debug("Couldn't find Bus position in Bus Stop");
            BusDoorPosition = new Point(12, 10);
        }

        /// <summary>
        /// Handle Pam getting back to her regular schedule
        /// </summary>
        /// <param name="pam"></param>
        /// <param name="busStop"></param>
        public static void PamBackToSchedule(Character pam, GameLocation busStop)
        {
            // TODO: Edge cases for Pam going back to regular schedule
        }

        public static void BusDoorAnimateClose(Character pam, GameLocation location)
        {
            if (pam != null)
                pam.position.X = -10000;

            BusDoor = new TemporaryAnimatedSprite(
                "LooseSprites\\Cursors",
                new Rectangle(288, 1311, 16, 38),
                BusPosition + new Vector2(16f, 26f) * 4f,
                false, 0f, Color.White)
            {
                interval = 70f,
                animationLength = 6,
                holdLastFrame = true,
                layerDepth = (BusPosition.X + 192f) / 10000f + 1E-05f,
                scale = 4f,
                timer = 0f,
                endFunction = delegate
                {
                    BusLocation.localSound("batFlap");
                    BusLocation.localSound("busDriveOff");
                },
                paused = false
            };

            BusLocation.localSound("trashcanlid");
        }
    }
}
