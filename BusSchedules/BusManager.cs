using System;
using System.Collections.Generic;
using System.Threading;
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
    private readonly WeakReference<BusStop> _busLocation = new WeakReference<BusStop>((BusStop) Game1.getLocationFromName("BusStop"));
   
    internal BusStop BusLocation
    {
        get
        {
            if (_busLocation.TryGetTarget(out BusStop? b))
                return b;

            Log.Warn("Bus Location updating");
            BusStop l = (BusStop) Game1.getLocationFromName("BusStop");
            Mod.Instance.UpdateBusLocation(l);
            return l;
        }
    }
    internal Point BusDoorPosition = new Point(12, 9);

    internal bool BusLeaving;
    internal bool BusGone;
    internal bool BusReturning;

    internal IReflectedField<TemporaryAnimatedSprite> BusDoorField = null!;
    internal IReflectedField<Vector2> BusMotionField = null!;
    internal IReflectedField<Vector2> BusPositionField = null!;

    private static Tile _roadTile = null!;
    private static Tile _lineTile = null!;
    private static Tile _shadowTile = null!;

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

    internal void UpdateLocation(IModHelper helper, BusStop location)
    {
        _busLocation.SetTarget(location);

        BusPositionField = helper.Reflection.GetField<Vector2>(location, "busPosition");
        BusMotionField = helper.Reflection.GetField<Vector2>(location, "busMotion");
        BusDoorField = helper.Reflection.GetField<TemporaryAnimatedSprite>(location, "busDoor");

        var tiles = location.Map.GetLayer("Buildings").Tiles;
        _roadTile = tiles[12, 7];
        _lineTile = tiles[12, 8];
        _shadowTile = tiles[13, 8];
    }

    /// <summary>
    ///     Call when all visitors have entered the bus and Pam is standing in position. This will make Pam get in and drive
    ///     away
    /// </summary>
    internal void BusLeave()
    {
        NPC pam = Game1.getCharacterFromName("Pam");
        if (!BusLocation.characters.Contains(pam) || pam.TilePoint is not { X: 11, Y: 10 })
            CloseDoorAndDriveAway();

        else
            pam.temporaryController = new PathFindController(pam, BusLocation, new Point(12, 9), 3, delegate(Character c, GameLocation loc)
            {
                if (c is NPC p)
                {
                    p.Position = new Vector2(-1000f, -1000f);
                }

                CloseDoorAndDriveAway();
            });
    }

    internal void CloseDoorAndDriveAway()
    {
        void DriveAwayAction(bool animate)
        {
            Log.Debug("Bus is leaving");

            BusLeaving = true;
            BusReturning = false;

            // Instantly leave if no farmer is in bus stop
            if (!animate)
            {
                BusGone = true;
                BusLeaving = false;
                BusMotion = Vector2.Zero;
                SetBusOutOfFrame();
            }

            var tiles = BusLocation.Map.GetLayer("Buildings").Tiles;
            for (int i = 11; i <= 18; i++)
            {
                for (int j = 7; j <= 9; j++)
                {
                    tiles[i, j] = null;
                }
            }
        }

        if (Game1.player.currentLocation.Equals(BusLocation))
        {
            ResetDoor();
            var door = BusDoor;
            door.interval = 70f;
            door.timer = 0f;
            door.endFunction = delegate
            {
                DriveAwayAction(animate: true);
                BusLocation.localSound("batFlap");
                BusLocation.localSound("busDriveOff");
            };
            door.paused = false;
            BusDoor = door;
        }
        else
        {
            DriveAwayAction(animate: false);
        }
    }

    internal void SetBusPark()
    {
        BusMotion = Vector2.Zero;
        
        BusReturning = false;
        BusLeaving = false;
        BusGone = false;

        BusPosition = new Vector2(704f, BusPosition.Y);
    }

    internal void SetBusOutOfFrame()
    {
        BusGone = true;
        BusReturning = false;
        BusLeaving = false;

        BusPosition = new Vector2(BusLocation.map.RequireLayer("Back").DisplayWidth, BusPosition.Y);
    }

    internal void DriveBack()
    {
        if (Context.IsMainPlayer)
            Mod.SendMessageToClient("BusDriveBack");

        Log.Debug("Bus is returning");

        BusLeaving = false;
        BusGone = false;

        if (Game1.player.currentLocation.Equals(BusLocation))
        {
            SetBusOutOfFrame();
            BusLocation.localSound("busDriveOff");
            // The UpdateWhenCurrentLocation postfix will handle the movement and call AfterDriveBack
            BusReturning = true;
            BusMotion = new Vector2(-6f, 0f);
        }
        else
        {
            AfterDriveBack(animate: false);
        }

        ResetDoor(closed: true);
    }

    internal void AfterDriveBack(bool animate = true)
    {
        ResetBus();

        BusReturning = false;
        BusGone = false;
        BusLeaving = false;

        // Animate bus door to open
        if (animate)
        {
            var door = BusDoor;
            door.timer = 0f;
            door.pingPong = true;
            door.interval = 70f;
            door.currentParentTileIndex = 5;
            door.endFunction = _ => BusEvents.Invoke_BusArrive();
            door.paused = false;
            BusDoor = door;
            BusLocation.localSound("trashcanlid");
        }
        else
            BusEvents.Invoke_BusArrive();
    }

    internal void ResetBus()
    {
        SetBusPark();
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
    }

    internal void OnDoorOpen(object? sender, EventArgs e)
    {
        Log.Debug("Bus has arrived");
        if (Game1.player.currentLocation.Equals(BusLocation))
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
    }

    internal void ResetDoor(bool closed = false)
    {
        if (closed)
        {
            BusDoor = new TemporaryAnimatedSprite(
                "LooseSprites\\Cursors",
                new Rectangle(368, 1311, 16, 38),
                BusPosition + new Vector2(16f, 26f) * 4f,
                false, 0f, Color.White)
            {
                interval = 999999f,
                animationLength = 1,
                holdLastFrame = true,
                layerDepth = (BusPosition.Y + 192f) / 10000f + 1E-05f,
                scale = 4f
            };
        }
        else
        {
            BusDoor = new TemporaryAnimatedSprite(
                "LooseSprites\\Cursors",
                new Rectangle(288, 1311, 16, 38),
                BusPosition + new Vector2(16f, 26f) * 4f,
                false, 0f, Color.White)
            {
                interval = 999999f,
                animationLength = 6,
                holdLastFrame = true,
                layerDepth = (BusPosition.Y + 192f) / 10000f + 1E-05f,
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
        if (BusLocation.characters.Contains(pam))
        {
            pam.Position = BusDoorPosition.ToVector2() * 64f;
            pam.temporaryController = new PathFindController(pam, BusLocation, new Point(11, 10), 2, delegate(Character c, GameLocation location)
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