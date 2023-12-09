#region Usings

using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Pathfinding;

#endregion


namespace VisitorFramework.Framework;

internal class BusManager
{
    internal byte BusArrivalsToday;
    internal int[] BusArrivalTimes = { 630, 1200, 1500, 1800, 2400 };
    internal IReflectedField<TemporaryAnimatedSprite> BusDoorField;

    internal EventHandler BusDoorOpen;

    internal Point BusDoorPosition;
    internal bool BusGone;

    internal bool BusLeaving;
    internal BusStop BusLocation;

    internal IReflectedField<Vector2> BusMotionField;
    internal IReflectedField<Vector2> BusPositionField;
    internal bool BusReturning;

    internal Vector2 BusPosition
    {
        get => BusPositionField.GetValue();
        set => BusPositionField.SetValue(value);
    }

    internal Vector2 BusMotion
    {
        get => BusMotionField.GetValue();
        set => BusMotionField.SetValue(value);
    }

    internal TemporaryAnimatedSprite BusDoor
    {
        get => BusDoorField.GetValue();
        set => BusDoorField.SetValue(value);
    }

    internal int NextArrivalTime => BusArrivalTimes[BusArrivalsToday];
    internal int LastArrivalTime => BusArrivalsToday == 0 ? -1 : BusArrivalTimes[BusArrivalsToday - 1];

    internal void DayUpdate(IModHelper helper)
    {
        GameLocation newLocation = Game1.getLocationFromName("BusStop");
        
        if (!BusLocation.Equals(newLocation) && newLocation is BusStop newLocationBusStop)
        {
            BusLocation = newLocationBusStop;
            Log.Debug("Updating bus stop location!");
            SetUp(helper);
        }
    }

    /// <summary>
    ///     Call when all visitors have entered the bus and Pam is standing in position. This will make Pam get in and drive
    ///     away
    /// </summary>
    internal void BusLeave()
    {
        var pam = Game1.getCharacterFromName("Pam");

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
    ///     Start the bus driving back animation
    /// </summary>
    internal void BusReturn()
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
    ///     After the bus is stopped, we open the door and return the driver
    /// </summary>
    /// <param name="animate">Whether or not to animate the door opening</param>
    internal void StopBus()
    {
        BusPosition = new Vector2(704f, BusPosition.Y);
        BusMotion = Vector2.Zero;
        BusReturning = false;
    }

    internal void OpenDoor()
    {
        // Animate bus door to open
        var busDoorSprite = BusDoor;

        if (busDoorSprite != null)
        {
            busDoorSprite.Position = BusPosition + new Vector2(16f, 26f) * 4f;
            busDoorSprite.pingPong = true;
            busDoorSprite.interval = 70f;
            busDoorSprite.currentParentTileIndex = 5;
            busDoorSprite.endFunction = _ => BusDoorOpen.Invoke(null, EventArgs.Empty);
            BusLocation.localSound("trashcanlid");

            BusDoor = busDoorSprite;
        }
        else
        {
            BusDoorOpen.Invoke(null, EventArgs.Empty);
        }
    }

    internal void OnBusDoorOpen(object sender, EventArgs e)
    {
        // Reset bus door sprite
        BusDoorField.SetValue(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(288, 1311, 16, 38), BusPosition + new Vector2(16f, 26f) * 4f,
            false, 0f, Color.White)
        {
            interval = 999999f,
            animationLength = 6,
            holdLastFrame = true,
            layerDepth = (BusPosition.Y + 192f) / 10000f + 1E-05f,
            scale = 4f
        });

        var pam = Game1.getCharacterFromName("Pam");
        if (BusLocation.characters.Contains(pam))
            pam.temporaryController = new PathFindController(pam, BusLocation, new Point(11, 10), 2, PamBackToSchedule);
    }

    internal void SetUp(IModHelper helper)
    {
        BusLocation = (BusStop)Game1.getLocationFromName("BusStop");

        BusPositionField = helper.Reflection.GetField<Vector2>(BusLocation, "busPosition");
        BusMotionField = helper.Reflection.GetField<Vector2>(BusLocation, "busMotion");
        BusDoorField = helper.Reflection.GetField<TemporaryAnimatedSprite>(BusLocation, "busDoor");
        
        BusDoorPosition = new Point((int) (BusPosition.X / 64) + 1, (int) (BusPosition.Y / 64) + 3);
    }

    /// <summary>
    ///     Handle Pam getting back to her regular schedule
    /// </summary>
    /// <param name="pam"></param>
    /// <param name="busStop"></param>
    public void PamBackToSchedule(Character pam, GameLocation busStop)
    {
        // TODO: Edge cases for Pam going back to regular schedule
    }

    public void BusDoorAnimateClose(Character pam, GameLocation location)
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