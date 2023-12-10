using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Pathfinding;
using xTile.Tiles;

namespace BusSchedules;

internal class BusManager
{
    internal BusStop BusLocation;
    internal Point BusDoorPosition;

    internal byte BusArrivalsToday;
    internal int[] BusArrivalTimes = { 630, 1200, 1500, 1800, 2400 };

    internal bool BusLeaving;
    internal bool BusGone;
    internal bool BusReturning;

    internal IReflectedField<TemporaryAnimatedSprite> BusDoorField;
    internal IReflectedField<Vector2> BusMotionField;
    internal IReflectedField<Vector2> BusPositionField;

    private static Tile _roadTile;
    private static Tile _lineTile;
    private static Tile _shadowTile;

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

    internal void SetUp(IModHelper helper)
    {
        BusLocation = (BusStop)Game1.getLocationFromName("BusStop");

        BusPositionField = helper.Reflection.GetField<Vector2>(BusLocation, "busPosition");
        BusMotionField = helper.Reflection.GetField<Vector2>(BusLocation, "busMotion");
        BusDoorField = helper.Reflection.GetField<TemporaryAnimatedSprite>(BusLocation, "busDoor");

        BusDoorPosition = new Point((int)(BusPosition.X / 64) + 1, (int)(BusPosition.Y / 64) + 3);

        var tiles = BusLocation.Map.GetLayer("Buildings").Tiles;
        _roadTile = tiles[12, 7];
        _lineTile = tiles[12, 8];
        _shadowTile = tiles[13, 8];
    }

    internal void DayUpdate(IModHelper helper)
    {
        BusArrivalsToday = 0;

        BusStop newLocation = Game1.getLocationFromName("BusStop") as BusStop;
        if (!BusLocation.Equals(newLocation))
        {
            BusLocation = newLocation;
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
            pam.temporaryController = new PathFindController(pam, BusLocation, new Point(12, 9), 3, CloseDoor);
        else
            CloseDoor(null, BusLocation);
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
        }

        BusLeaving = false;
        BusGone = false;
        BusArrivalsToday++;
    }

    internal void StopBus(bool openDoor = true)
    {
        BusPosition = new Vector2(704f, BusPosition.Y);
        BusMotion = Vector2.Zero;
        BusReturning = false;
        BusLeaving = false;
        BusGone = false;

        var tiles = BusLocation.Map.GetLayer("Buildings").Tiles;
        for (int i = 11; i <= 18; i++)
        {
            for (int j = 7; j <= 9; j++)
            {
                if (j == 7 || j == 9)
                    tiles[i, j] = _roadTile;
                else if (j == 8)
                    tiles[i, j] = _lineTile;
            }
        }

        tiles[13, 8] = _shadowTile;
        tiles[16, 8] = _shadowTile;
        tiles[12, 9] = null;

        if (openDoor)
            OpenDoor();
        else
            ResetDoor();
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
            busDoorSprite.endFunction = _ => Events.Invoke_BusArrive();
            BusLocation.localSound("trashcanlid");
        }
        else
            Events.Invoke_BusArrive();
    }

    public void CloseDoor(Character c, GameLocation location)
    {
        if (c is NPC pam)
        {
            pam.Position = new Vector2(-1000f, -1000f);
        }

        var busDoorSprite = BusDoor;
        if (busDoorSprite != null && BusLocation.farmers.Any())
        {
            busDoorSprite.interval = 70f;
            busDoorSprite.animationLength = 6;
            busDoorSprite.holdLastFrame = true;
            busDoorSprite.layerDepth = (BusPosition.X + 192f) / 10000f + 1E-05f;
            busDoorSprite.scale = 4f;
            busDoorSprite.timer = 0f;
            busDoorSprite.endFunction = delegate
            {
                BusDriveAway();
                BusLocation.localSound("batFlap");
                BusLocation.localSound("busDriveOff");
            };
            busDoorSprite.paused = false;
        }
        else
            BusDriveAway();

        BusLocation.localSound("trashcanlid");

        var tiles = BusLocation.Map.GetLayer("Buildings").Tiles;
        for (int i = 11; i <= 18; i++)
        {
            for (int j = 7; j <= 9; j++)
            {
                tiles[i, j] = null;
            }
        }
    }

    internal void BusDriveAway()
    {
        BusLeaving = true;
        BusReturning = false;

        // Instantly leave if no farmer is in bus stop
        if (BusLocation.farmers.Count == 0)
        {
            BusGone = true;
            BusLeaving = false;
            BusMotion = Vector2.Zero;
            BusPosition = new Vector2(-1000f, BusPosition.Y);
        }
    }

    internal void OnDoorOpen(object sender, EventArgs e)
    {
        if (BusLocation.farmers.Any())
        {
            // Reset bus door sprite
            ResetDoor();
            TemporaryAnimatedSprite door = BusDoor;
            door.interval = 999999f;
            door.animationLength = 6;
            door.holdLastFrame = true;
            door.layerDepth = (BusPosition.Y + 192f) / 10000f + 1E-05f;
            door.scale = 4f;
        }
        
        var pam = Game1.getCharacterFromName("Pam");
        if (BusLocation.characters.Contains(pam))
            pam.temporaryController = new PathFindController(pam, BusLocation, new Point(11, 10), 2, PamBackToSchedule);
    }

    internal void ResetDoor()
    {
        BusDoor = new TemporaryAnimatedSprite(
            "LooseSprites\\Cursors",
            new Rectangle(288, 1311, 16, 38),
            BusPosition + new Vector2(16f, 26f) * 4f,
            false, 0f, Color.White);
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
}