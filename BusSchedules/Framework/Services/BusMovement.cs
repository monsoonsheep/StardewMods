using StardewMods.BusSchedules.Framework.Enums;
using StardewValley.Locations;
using xTile.Tiles;

namespace StardewMods.BusSchedules.Framework.Services;

/// <summary>
/// Handle the movement of the bus sprite and the door closing opening 
/// in the bus stop location by patching update methods
/// </summary>
internal class BusMovement : Service
{
    private static BusMovement instance = null!;

    // SMAPI services
    private readonly IReflectionHelper reflection;
    private readonly Harmony harmony;

    // Mod Services
    private readonly ModEvents modEvents;
    private readonly MultiplayerMessaging multiplayer;
    private readonly LocationProvider locations;

    // State
    internal BusState State;
    private IReflectedField<TemporaryAnimatedSprite> BusDoorField = null!;
    private Tile RoadTile = null!;
    private Tile LineTile = null!;
    private Tile ShadowTile = null!;

    internal TemporaryAnimatedSprite BusDoor
    {
        get => this.BusDoorField.GetValue();
        set => this.BusDoorField.SetValue(value);
    }

    internal bool IsMoving
        => this.State is BusState.DrivingIn or BusState.DrivingOut;

    public BusMovement(
        Harmony harmony,
        ModEvents modEvents,
        MultiplayerMessaging multiplayer,
        LocationProvider locations,
        IReflectionHelper reflection,
        ILogger logger,
        IManifest manifest)
        : base(logger, manifest)
    {
        instance = this;

        this.harmony = harmony;
        this.multiplayer = multiplayer;
        this.reflection = reflection;
        this.locations = locations;
        this.modEvents = modEvents;

        harmony.Patch(
            original: AccessTools.Method(typeof(BusStop), "doorOpenAfterReturn", [typeof(int)]),
            postfix: new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(After_doorOpenAfterReturn)))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(BusStop), "UpdateWhenCurrentLocation", [typeof(GameTime)]),
            postfix: new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(After_UpdateWhenCurrentLocation)))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(GameLocation), "cleanupForVacancy", []),
            postfix: new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(After_cleanupForVacancy)))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(BusStop), "resetLocalState", []),
            postfix: new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(After_resetLocalState)))
        );

    }

    /// <summary>
    /// When the player comes back from the desert and the bus is supposed to be away, reset the bus out of frame
    /// </summary>
    private static void After_doorOpenAfterReturn(BusStop __instance)
    {
        if (instance.State == BusState.Gone)
        {
            instance.MoveOutOfMap();
            instance.ResetDoor(closed: false);
        }
    }

    /// <summary>
    /// Called when player enters the location. We put bus out of frame if it shouldn't be there
    /// </summary>
    private static void After_resetLocalState(BusStop __instance)
    {
        instance.BusDoorField = instance.reflection.GetField<TemporaryAnimatedSprite>(__instance, "busDoor");

        if (!Context.IsMainPlayer)
        {
            instance.locations.UpdateLocation("BusStop");
        }

        TileArray tiles = __instance.Map.GetLayer("Buildings").Tiles;
        instance.RoadTile = tiles[22, 7];
        instance.LineTile = tiles[22, 8];
        instance.ShadowTile = tiles[23, 8];

        if (instance.State is BusState.DrivingOut or BusState.Gone)
        {
            instance.MoveOutOfMap();
            instance.ResetDoor(closed: true);
        }
    }

    /// <summary>
    /// Called when all players leave the bus stop
    /// </summary>
    private static void After_cleanupForVacancy(GameLocation __instance)
    {
        if (Context.IsMainPlayer && __instance is BusStop busStop)
        {
            if (instance.State is BusState.DrivingOut or BusState.Gone)
            {
                instance.MoveOutOfMap();
            }
            else if (instance.State is BusState.DrivingIn)
            {
                instance.AfterDriveBack(animate: false);
            }
        }
    }

    /// <summary>
    /// Bus movement
    /// When the player is in BusStop and the bus is moving, update the busMotion every tick
    /// </summary>
    private static void After_UpdateWhenCurrentLocation(BusStop __instance, GameTime time)
    {
        if (instance.State == BusState.DrivingOut)
        {
            // Accelerate toward left
            __instance.busMotion.X = __instance.busMotion.X - 0.075f;

            // Stop moving the bus once it's off screen
            if (__instance.busPosition.X < -512f)
            {
                instance.MoveOutOfMap();
                instance.ResetDoor(closed: true);
            }
        }

        if (instance.State == BusState.DrivingIn)
        {
            // Decelerate after getting to X=15 from the right
            if (__instance.busPosition.X - 1344f < 512f)
            {
                __instance.busMotion.X = Math.Min(-1f, __instance.busMotion.X * 0.98f);
            }

            // Teleport to position bus reached the stop
            if (Math.Abs(__instance.busPosition.X - 1344f) <= Math.Abs(__instance.busMotion.X * 1.5f))
            {
                instance.AfterDriveBack(animate: true);
            }
        }
    }

    internal void DriveIn()
    {
        if (Context.IsMainPlayer)
            this.multiplayer.SendMessage("BusDriveBack");

        this.Log.Info("Bus is returning");
        this.ResetDoor(closed: true);

        if (Game1.player.currentLocation.Equals(this.locations.BusStop))
        {
            this.MoveOutOfMap();
            this.locations.BusStop.localSound("busDriveOff");
            // The UpdateWhenCurrentLocation postfix will handle the movement and call AfterDriveBack
            this.State = BusState.DrivingIn;
            this.locations.BusStop.busMotion = new Vector2(-12f, 0f);
        }
        else
        {
            // Instantly move into parking space and call arrive function
            this.State = BusState.Parked;
            this.AfterDriveBack(animate: false);
        }
    }

    internal void DriveOut()
    {
        if (Context.IsMainPlayer)
            this.multiplayer.SendMessage("BusDoorClose");

        this.Log.Info("Bus is leaving");

        this.RemoveTiles();

        if (Game1.player.currentLocation.Equals(this.locations.BusStop))
        {
            this.AnimateDoorClose(delegate
            {
                this.State = BusState.DrivingOut;
                this.RemoveTiles();
                this.locations.BusStop.localSound("batFlap");
                this.locations.BusStop.localSound("busDriveOff");
            });
        }
        else
        {
            // Instantly leave if farmer is not in bus stop
            this.MoveOutOfMap();
        }
    }

    private void AfterDriveBack(bool animate = true)
    {
        this.ParkBus();
        TemporaryAnimatedSprite.endBehavior endFunction = delegate (int a)
        {
            this.ResetDoor(closed: false);
            this.modEvents.Invoke_BusArrive();
        };

        // Animate bus door to open
        if (animate)
            this.AnimateDoorOpen(endFunction);
        else
            endFunction.Invoke(0);
    }

    internal void ParkBus()
    {
        this.State = BusState.Parked;
        this.locations.BusStop.busMotion = Vector2.Zero;
        this.locations.BusStop.busPosition.X = 1344f;
        this.RestoreTiles();
    }

    internal void MoveOutOfMap()
    {
        this.State = BusState.Gone;
        this.locations.BusStop.busMotion = Vector2.Zero;
        this.locations.BusStop.busPosition.X = this.locations.BusStop.map.GetLayer("Back").DisplayWidth;
        this.RemoveTiles();
    }

    internal void RestoreTiles()
    {
        TileArray tiles = this.locations.BusStop.Map.GetLayer("Buildings").Tiles;
        for (int i = 21; i <= 28; i++)
        {
            for (int j = 7; j <= 9; j++)
            {
                if (j == 7 || j == 9)
                    tiles[i, j] = this.RoadTile;
                else if (j == 8)
                    tiles[i, j] = this.LineTile;
            }
        }
        tiles[23, 8] = this.ShadowTile;
        tiles[26, 8] = this.ShadowTile;
        tiles[22, 9] = null;
    }

    internal void RemoveTiles()
    {
        TileArray tiles = this.locations.BusStop.Map.GetLayer("Buildings").Tiles;
        for (int i = 21; i <= 28; i++)
        {
            for (int j = 7; j <= 9; j++)
            {
                tiles[i, j] = null;
            }
        }
    }

    private void AnimateDoorOpen(TemporaryAnimatedSprite.endBehavior endFunction)
    {
        this.BusDoor.timer = 0f;
        this.BusDoor.pingPong = true;
        this.BusDoor.interval = 70f;
        this.BusDoor.currentParentTileIndex = 5;
        this.BusDoor.endFunction = endFunction;
        this.BusDoor.paused = false;
        this.locations.BusStop.localSound("trashcanlid");
    }

    private void AnimateDoorClose(TemporaryAnimatedSprite.endBehavior endFunction)
    {
        this.ResetDoor(closed: false);
        this.BusDoor.interval = 70f;
        this.BusDoor.timer = 0f;
        this.BusDoor.endFunction = endFunction;
        this.BusDoor.paused = false;
    }

    internal void ResetDoor(bool closed = false)
    {
        this.BusDoorField = this.reflection.GetField<TemporaryAnimatedSprite>(this.locations.BusStop, "busDoor");
        this.BusDoor = new TemporaryAnimatedSprite(
            "LooseSprites\\Cursors",
            new Rectangle(closed ? 368 : 288, 1311, 16, 38), this.locations.BusStop.busPosition + new Vector2(16f, 26f) * 4f,
            false, 0f, Color.White)
        {
            interval = 999999f,
            animationLength = closed ? 1 : 6,
            holdLastFrame = true,
            layerDepth = (this.locations.BusStop.busPosition.Y + 192f) / 10000f + 1E-05f,
            scale = 4f
        };
    }
}
