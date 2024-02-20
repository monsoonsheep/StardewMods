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

    internal Point BusDoorPosition = Mod.BusDoorTile;

    internal BusState State;

    internal IReflectedField<TemporaryAnimatedSprite> BusDoorField = null!;

    internal TemporaryAnimatedSprite BusDoor
    {
        get => this.BusDoorField.GetValue();
        set => this.BusDoorField.SetValue(value);
    }

    internal bool IsMoving
        => this.State is BusState.DrivingIn or BusState.DrivingOut;

    internal void DriveOut()
    {
        if (Context.IsMainPlayer)
            Mod.SendMessageToClient("BusDoorClose");

        void DriveAwayAction(bool animate)
        {
            Log.Info("Bus is leaving");

            this.State = BusState.DrivingOut;

            // Instantly leave if no farmer is in bus stop
            if (!animate)
            {
                this.State = BusState.Gone;
                Mod.BusLocation.busMotion = Vector2.Zero;
                this.MoveOutOfMap();
            }

            Mod.RemoveTiles();
        }

        if (Game1.player.currentLocation.Equals(Mod.BusLocation))
        {
            this.ResetDoor();
            TemporaryAnimatedSprite door = this.BusDoor;
            door.interval = 70f;
            door.timer = 0f;
            door.endFunction = delegate
            {
                DriveAwayAction(animate: true);
                Mod.BusLocation.localSound("batFlap");
                Mod.BusLocation.localSound("busDriveOff");
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
        Mod.BusLocation.busMotion = Vector2.Zero;
        this.State = BusState.Parked;
        Mod.BusLocation.busPosition = new Vector2(704f, Mod.BusLocation.busPosition.Y);
        Mod.ResetTiles();
    }

    internal void MoveOutOfMap()
    {
        Log.Trace("Bus is out of map");
        this.State = BusState.Gone;
        Mod.BusLocation.busPosition = new Vector2(Mod.BusLocation.map.RequireLayer("Back").DisplayWidth, Mod.BusLocation.busPosition.Y);
    }

    internal void DriveIn()
    {
        if (Context.IsMainPlayer)
            Mod.SendMessageToClient("BusDriveBack");

        Log.Info("Bus is returning");

        if (Game1.player.currentLocation.Equals(Mod.BusLocation))
        {
            this.MoveOutOfMap();
            Mod.BusLocation.localSound("busDriveOff");
            // The UpdateWhenCurrentLocation postfix will handle the movement and call AfterDriveBack
            this.State = BusState.DrivingIn;
            Mod.BusLocation.busMotion = new Vector2(-6f, 0f);
        }
        else
        {
            this.State = BusState.Parked;
            this.AfterDriveBack(animate: false);
        }

        this.ResetDoor(closed: true);
    }

    internal void AfterDriveBack(bool animate = true)
    {
        this.ParkBus();
        this.State = BusState.Parked;

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
            Mod.BusLocation.localSound("trashcanlid");
        }
        else
            BusEvents.Invoke_BusArrive();
    }

    internal void OnDoorOpen(object? sender, EventArgs e)
    {
        Log.Info("Bus has arrived");
        if (Game1.player.currentLocation.Equals(Mod.BusLocation))
        {
            // Reset bus door sprite
            this.ResetDoor();
            TemporaryAnimatedSprite door = this.BusDoor;
            door.interval = 999999f;
            door.animationLength = 6;
            door.holdLastFrame = true;
            door.layerDepth = (Mod.BusLocation.busPosition.Y + 192f) / 10000f + 1E-05f;
            door.scale = 4f;
        }
    }

    internal void ResetDoor(bool closed = false)
    {
        if (closed)
        {
            this.BusDoor = new TemporaryAnimatedSprite(
                "LooseSprites\\Cursors",
                new Rectangle(368, 1311, 16, 38), Mod.BusLocation.busPosition + new Vector2(16f, 26f) * 4f,
                false, 0f, Color.White)
            {
                interval = 999999f,
                animationLength = 1,
                holdLastFrame = true,
                layerDepth = (Mod.BusLocation.busPosition.Y + 192f) / 10000f + 1E-05f,
                scale = 4f
            };
        }
        else
        {
            this.BusDoor = new TemporaryAnimatedSprite(
                "LooseSprites\\Cursors",
                new Rectangle(288, 1311, 16, 38), Mod.BusLocation.busPosition + new Vector2(16f, 26f) * 4f,
                false, 0f, Color.White)
            {
                interval = 999999f,
                animationLength = 6,
                holdLastFrame = true,
                layerDepth = (Mod.BusLocation.busPosition.Y + 192f) / 10000f + 1E-05f,
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
        if (Mod.BusLocation.characters.Contains(pam))
        {
            pam.Position = this.BusDoorPosition.ToVector2() * 64f;
            pam.temporaryController = new PathFindController(pam, Mod.BusLocation, new Point(11, 10), 2, delegate(Character c, GameLocation location)
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
