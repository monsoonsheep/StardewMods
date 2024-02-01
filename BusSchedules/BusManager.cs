using System;
using HarmonyLib;
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
    private readonly WeakReference<BusStop> BusLocationRef = new WeakReference<BusStop>((BusStop) Game1.getLocationFromName("BusStop"));
   
    internal BusStop BusLocation
    {
        get
        {
            if (this.BusLocationRef.TryGetTarget(out BusStop? b))
                return b;

            Log.Warn("Bus Location updating");
            BusStop l = (BusStop) Game1.getLocationFromName("BusStop");
            Mod.Instance.UpdateBusLocation(l);
            return l;
        }
    }
    internal Point BusDoorPosition = Mod.BusDoorTile;

    internal bool BusLeaving;
    internal bool BusGone;
    internal bool BusReturning;

    internal IReflectedField<TemporaryAnimatedSprite> BusDoorField = null!;
    internal IReflectedField<Vector2> BusMotionField = null!;
    internal IReflectedField<Vector2> BusPositionField = null!;

    private static Tile RoadTile = null!;
    private static Tile LineTile = null!;
    private static Tile ShadowTile = null!;

    internal Vector2 BusPosition
    {
        get => this.BusPositionField.GetValue();
        set => this.BusPositionField.SetValue(value);
    }
    internal Vector2 BusMotion
    {
        get => this.BusMotionField.GetValue();
        set => this.BusMotionField.SetValue(value);
    }
    internal TemporaryAnimatedSprite BusDoor
    {
        get => this.BusDoorField.GetValue();
        set => this.BusDoorField.SetValue(value);
    }

    internal void UpdateLocation(IModHelper helper, BusStop location)
    {
        this.BusLocationRef.SetTarget(location);

        this.BusPositionField = helper.Reflection.GetField<Vector2>(location, "busPosition");
        this.BusMotionField = helper.Reflection.GetField<Vector2>(location, "busMotion");
        this.BusDoorField = helper.Reflection.GetField<TemporaryAnimatedSprite>(location, "busDoor");

        var tiles = location.Map.GetLayer("Buildings").Tiles;
        RoadTile = tiles[12, 7];
        LineTile = tiles[12, 8];
        ShadowTile = tiles[13, 8];
    }

    /// <summary>
    ///     Call when all visitors have entered the bus and Pam is standing in position. This will make Pam get in and drive
    ///     away
    /// </summary>
    internal void BusLeave()
    {
        NPC pam = Game1.getCharacterFromName("Pam");
        if (!this.BusLocation.characters.Contains(pam) || pam.TilePoint is not { X: 11, Y: 10 })
            this.CloseDoorAndDriveAway();
        else
            pam.temporaryController = new PathFindController(pam, this.BusLocation, Mod.BusDoorTile, 3, delegate(Character c, GameLocation _)
            {
                if (c is NPC p)
                    p.Position = new Vector2(-1000f, -1000f);

                this.CloseDoorAndDriveAway();
            });
    }

    internal void CloseDoorAndDriveAway()
    {
        if (Context.IsMainPlayer)
            Mod.SendMessageToClient("BusDoorClose");

        void DriveAwayAction(bool animate)
        {
            Log.Debug("Bus is leaving");

            this.BusReturning = false;
            this.BusLeaving = true;

            // Instantly leave if no farmer is in bus stop
            if (!animate)
            {
                this.BusGone = true;
                this.BusLeaving = false;
                this.BusMotion = Vector2.Zero;
                this.MoveBusOutOfMap();
            }

            var tiles = this.BusLocation.Map.GetLayer("Buildings").Tiles;
            for (int i = 11; i <= 18; i++)
            {
                for (int j = 7; j <= 9; j++)
                {
                    tiles[i, j] = null;
                }
            }
        }

        if (Game1.player.currentLocation.Equals(this.BusLocation))
        {
            this.ResetDoor();
            TemporaryAnimatedSprite door = this.BusDoor;
            door.interval = 70f;
            door.timer = 0f;
            door.endFunction = delegate
            {
                DriveAwayAction(animate: true);
                this.BusLocation.localSound("batFlap");
                this.BusLocation.localSound("busDriveOff");
            };
            door.paused = false;
            this.BusDoor = door;
        }
        else
        {
            DriveAwayAction(animate: false);
        }
    }

    internal void ParkBus()
    {
        this.BusMotion = Vector2.Zero;

        this.BusReturning = false;
        this.BusLeaving = false;
        this.BusGone = false;

        this.BusPosition = new Vector2(704f, this.BusPosition.Y);
    }

    internal void MoveBusOutOfMap()
    {
        Log.Debug("Bus is out of map");
        this.BusGone = true;
        this.BusReturning = false;
        this.BusLeaving = false;

        this.BusPosition = new Vector2(this.BusLocation.map.RequireLayer("Back").DisplayWidth, this.BusPosition.Y);
    }

    internal void DriveBack()
    {
        if (Context.IsMainPlayer)
            Mod.SendMessageToClient("BusDriveBack");

        Log.Debug("Bus is returning");

        this.BusLeaving = false;
        this.BusGone = false;

        if (Game1.player.currentLocation.Equals(this.BusLocation))
        {
            this.MoveBusOutOfMap();
            this.BusLocation.localSound("busDriveOff");
            // The UpdateWhenCurrentLocation postfix will handle the movement and call AfterDriveBack
            this.BusReturning = true;
            this.BusMotion = new Vector2(-6f, 0f);
        }
        else
        {
            this.AfterDriveBack(animate: false);
        }

        this.ResetDoor(closed: true);
    }

    internal void AfterDriveBack(bool animate = true)
    {
        this.ResetBus();

        this.BusReturning = false;
        this.BusGone = false;
        this.BusLeaving = false;

        // Animate bus door to open
        if (animate)
        {
            var door = this.BusDoor;
            door.timer = 0f;
            door.pingPong = true;
            door.interval = 70f;
            door.currentParentTileIndex = 5;
            door.endFunction = _ => BusEvents.Invoke_BusArrive();
            door.paused = false;
            this.BusDoor = door;
            this.BusLocation.localSound("trashcanlid");
        }
        else
            BusEvents.Invoke_BusArrive();
    }

    internal void ResetBus()
    {
        this.ParkBus();
        var tiles = this.BusLocation.Map.GetLayer("Buildings").Tiles;
        for (int i = 11; i <= 18; i++)
        {
            for (int j = 7; j <= 9; j++)
            {
                if (j == 7 || j == 9)
                    tiles[i, j] = RoadTile;
                else if (j == 8)
                    tiles[i, j] = LineTile;
            }
        }
        tiles[13, 8] = ShadowTile;
        tiles[16, 8] = ShadowTile;
        tiles[12, 9] = null;
    }

    internal void OnDoorOpen(object? sender, EventArgs e)
    {
        Log.Debug("Bus has arrived");
        if (Game1.player.currentLocation.Equals(this.BusLocation))
        {
            // Reset bus door sprite
            this.ResetDoor();
            TemporaryAnimatedSprite door = this.BusDoor;
            door.interval = 999999f;
            door.animationLength = 6;
            door.holdLastFrame = true;
            door.layerDepth = (this.BusPosition.Y + 192f) / 10000f + 1E-05f;
            door.scale = 4f;
        }
    }

    internal void ResetDoor(bool closed = false)
    {
        if (closed)
        {
            this.BusDoor = new TemporaryAnimatedSprite(
                "LooseSprites\\Cursors",
                new Rectangle(368, 1311, 16, 38), this.BusPosition + new Vector2(16f, 26f) * 4f,
                false, 0f, Color.White)
            {
                interval = 999999f,
                animationLength = 1,
                holdLastFrame = true,
                layerDepth = (this.BusPosition.Y + 192f) / 10000f + 1E-05f,
                scale = 4f
            };
        }
        else
        {
            this.BusDoor = new TemporaryAnimatedSprite(
                "LooseSprites\\Cursors",
                new Rectangle(288, 1311, 16, 38), this.BusPosition + new Vector2(16f, 26f) * 4f,
                false, 0f, Color.White)
            {
                interval = 999999f,
                animationLength = 6,
                holdLastFrame = true,
                layerDepth = (this.BusPosition.Y + 192f) / 10000f + 1E-05f,
                scale = 4f
            };

        }
    }

    /// <summary>
    ///     Handle Pam getting back to her regular schedule
    /// </summary>
    public void PamBackToSchedule(object? sender, EventArgs e)
    {
        var pam = Game1.getCharacterFromName("Pam");
        if (this.BusLocation.characters.Contains(pam))
        {
            pam.Position = this.BusDoorPosition.ToVector2() * 64f;
            pam.temporaryController = new PathFindController(pam, this.BusLocation, new Point(11, 10), 2, delegate(Character c, GameLocation location)
            {
                if (c is NPC p)
                {
                    Point? previousEndPoint = (Point?) AccessTools.Field(typeof(NPC), "previousEndPoint").GetValue(p);
                    if (previousEndPoint is { X: 11, Y: 10 })
                    {
                        p.temporaryController = new PathFindController(p, location, new Point(11, 10),
                            2, null);
                    }
                }
            });
        }
    }
}
