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

namespace FarmCafe.Framework.Managers
{
    internal class BusManager
    {
        internal BusStop BusLocation;

        internal bool BusLeaving;
        internal bool BusReturning;
        internal bool BusGone;

        internal int[] BusDepartureTimes = new[] { 1100, 1430, 1800 };
        internal byte BusDeparturesToday = 0;

        internal void BusLeave()
        {
            FieldInfo busMotion = typeof(BusStop).GetField("busMotion", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo busPosition = typeof(BusStop).GetField("busPosition", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo busDoor = typeof(BusStop).GetField("busDoor", BindingFlags.NonPublic | BindingFlags.Instance);

            if (busMotion == null || busPosition == null || busDoor == null)
            {
                Logger.Log("Bus can't leave");
                return;
            }

            Vector2 busPositionVal = (Vector2) busPosition.GetValue(BusLocation);

            busMotion.SetValue(BusLocation, new Vector2(0f, 0f));
            busDoor.SetValue(BusLocation, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(288, 1311, 16, 38), busPositionVal + new Vector2(16f, 26f) * 4f, flipped: false, 0f, Color.White)
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

            BusReturning = false;
            BusLeaving = true;
        }

        internal void BusReturn()
        {
            FieldInfo busMotion = typeof(BusStop).GetField("busMotion", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo busPosition = typeof(BusStop).GetField("busPosition", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo busDoor = typeof(BusStop).GetField("busDoor", BindingFlags.NonPublic | BindingFlags.Instance);
            
            TemporaryAnimatedSprite busDoorSprite = (TemporaryAnimatedSprite) busDoor.GetValue(BusLocation);
            if (busDoorSprite == null)
            {
                Logger.Log("Bus can't leave", LogLevel.Error);
                return;
            }

            Vector2 busPositionVal = (Vector2) busPosition.GetValue(BusLocation);

            busPosition.SetValue(BusLocation, new Vector2(BusLocation.map.RequireLayer("Back").DisplayWidth, busPositionVal.Y));
            busPositionVal = (Vector2) busPosition.GetValue(BusLocation);

            (busDoorSprite).Position = busPositionVal + new Vector2(16f, 26f) * 4f;
            BusLocation.localSound("busDriveOff");
            busMotion.SetValue(BusLocation, new Vector2(-6f, 0f));

            BusReturning = true;
            BusLeaving = false;
            BusGone = false;
        }

        internal void UpdateBusStopLocation()
        {
            BusLocation = (BusStop) Game1.getLocationFromName("BusStop");
        }
    }
}
