using StardewValley.Locations;
using StardewValley.Pathfinding;
using StardewMods.BusSchedules.Framework.Enums;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection.Emit;
using System.Reflection;

namespace StardewMods.BusSchedules.Framework.Services;

/// <summary>
/// The main modifications to the bus. Make it drive away and back and handle the drawing and other
/// changes.
/// </summary>
internal class BusManager : Service
{
    private static BusManager Instance = null!;

    // SMAPI Services
    private readonly Harmony harmony;
    private readonly IReflectionHelper reflection;
    private readonly IModEvents events;

    // Mod Services
    private readonly MultiplayerMessaging multiplayer;
    private readonly BusMovement busMovement;
    private readonly Timings timings;
    private readonly LocationProvider locations;

    // State
    internal bool BusEnabled;

    public BusManager(
        IManifest manifest,
        ILogger log,
        Harmony harmony,
        IModEvents events,
        IReflectionHelper reflection,
        ModEvents modEvents,
        Timings timings,
        BusMovement busMovement,
        MultiplayerMessaging multiplayer,
        LocationProvider locations)
        : base(log, manifest)
    {
        Instance = this;

        this.harmony = harmony;
        this.events = events;
        this.multiplayer = multiplayer;
        this.reflection = reflection;
        this.busMovement = busMovement;
        this.locations = locations;
        this.timings = timings;

        events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
        events.GameLoop.DayStarted += this.OnDayStarted;
        events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;
        modEvents.BusArrive += this.OnBusArrive;

        harmony.Patch(
            original: AccessTools.Method(typeof(BusStop), "answerDialogue", [typeof(Response)]),
            prefix: new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(Before_answerDialogue))),
            postfix: new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(After_answerDialogue))),
            transpiler: new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(Transpiler_answerDialogue)))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(BusStop), "draw", [typeof(SpriteBatch)]),
            postfix: new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(After_draw)))
        );
    }

    #region Patches
    /// <summary>
    /// If the player tries to board the bus while it's about to arrive or just left, make the player wait
    /// </summary>
    private static bool Before_answerDialogue(BusStop __instance, Response answer, ref bool __result)
    {
        // If bus is currently moving in the location or it's about to arrive in 20 minutes or it's only been 20 minutes since it left
        if (Instance.busMovement.IsMoving || Instance.busMovement.State is BusState.Gone && (Instance.timings.TimeUntilNextArrival <= 20 || Instance.timings.TimeSinceLastArrival <= 20))
        {
            Game1.chatBox.addMessage("The bus will arrive shortly.", Color.White);
            __result = false;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Prevent the game from stopping you from boarding the bus when Pam isn't there
    /// </summary>
    private static IEnumerable<CodeInstruction> Transpiler_answerDialogue(IEnumerable<CodeInstruction> instructions)
    {
        var list = instructions.ToList();
        int idx1 = -1;
        int idx2 = -1;

        MethodInfo drawObjectDialogMethod = AccessTools.Method(typeof(Game1), nameof(Game1.drawObjectDialogue), new[] { typeof(string) });

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Is(OpCodes.Ldstr, "Pam"))
            {
                idx1 = i;
            }

            if (list[i].Calls(drawObjectDialogMethod))
            {
                idx2 = i + 2;
                break;
            }
        }

        if (idx1 != -1 && idx2 != -1)
        {
            list.RemoveRange(idx1, idx2 - idx1);
        }

        return list.AsEnumerable();
    }

    /// <summary>
    /// After the player confirms to use the bus, we intercept the commands. If the bus isn't present, we put the
    /// player's controller away, fade to black and return the controller after the fade
    /// </summary>
    private static void After_answerDialogue(BusStop __instance, Response answer, ref bool __result)
    {
        if (__result == true && Game1.player.controller != null)
        {
            if (Math.Abs(__instance.busPosition.X - 1344f) < 0.001)
            {
                __instance.busMotion = Vector2.Zero;
            }
            else
            {
                if (Instance.timings.TimeUntilNextArrival <= 20 || Instance.timings.TimeSinceLastArrival <= 20)
                    return;

                PathFindController controller = Game1.player.controller;
                Game1.player.controller = null;
                Game1.globalFadeToBlack(delegate
                {
                    __instance.busPosition = new Vector2(21f, 6f) * 64f;
                    Game1.player.controller = controller;
                    Game1.globalFadeToClear();
                    Instance.busMovement.ResetDoor(closed: false);
                });
            }
        }
    }

    /// <summary>
    /// Patch the draw method of BusStop to show pam if she's there
    /// </summary>
    private static void After_draw(BusStop __instance, SpriteBatch spriteBatch)
    {
        if (Instance.busMovement.IsMoving && __instance.characters.Any(x => x.Name == "Pam"))
        {
            spriteBatch.Draw(Game1.mouseCursors,
                Game1.GlobalToLocal(Game1.viewport, new Vector2((int)__instance.busPosition.X, (int)__instance.busPosition.Y) + new Vector2(0f, 29f) * 4f),
                new Rectangle(384, 1311, 15, 19), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (__instance.busPosition.Y + 192f + 4f) / 10000f);
        }
    }
    #endregion

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        this.locations.UpdateLocation("BusStop");

        if (Context.IsMainPlayer)
        {
            if (!this.BusEnabled && Game1.MasterPlayer.mailReceived.Contains("ccVault"))
            {
                this.Log.Debug("Enabling bus");
                this.BusEnabled = true;
                this.events.GameLoop.TimeChanged += this.OnTimeChanged;
            }

            this.timings.BusArrivalsToday = 0;
        }

        // Early morning bus departure
        //if (this.BusEnabled)
        //{
        //    this.BusLeave();
        //}
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        this.events.GameLoop.TimeChanged -= this.OnTimeChanged;
        this.BusEnabled = false;
        this.busMovement.State = BusState.Parked;
    }

    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        if (this.timings.CheckReturnTime(e.NewTime))
        {
            this.timings.BusArrivalsToday++;
            this.busMovement.DriveIn();
        }

        else if (this.timings.CheckLeaveTime(e.NewTime) && this.busMovement.State == BusState.Parked)
        {
            this.BusLeave();
        }
    }

    // For synchronizing bus door closing and arriving in the location to multiplayer clients
    private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (!Context.IsWorldReady || !e.FromModID.Equals(this.ModManifest.UniqueID))
            return;

        string? data = e.ReadAs<string>();

        // From host to client messages
        if (e.FromPlayerID == Game1.MasterPlayer.UniqueMultiplayerID && !Context.IsMainPlayer)
        {
            switch (data)
            {
                case "BusDoorClose":
                    this.Log.Debug("Received message to close door and drive away");
                    this.locations.UpdateLocation("BusStop");
                    this.busMovement.DriveOut();
                    return;
                case "BusDriveBack":
                    this.Log.Debug("Received message to drive bus in");
                    this.locations.UpdateLocation("BusStop");
                    this.busMovement.DriveIn();
                    return;
            }
        }
    }

    private void OnBusArrive(object? sender, BusArriveEventArgs e)
    {
        this.PamBackToSchedule();
    }

    /// <summary>
    /// Call when all visitors have entered the bus and Pam is standing in position. This will make Pam get in and drive
    /// away
    /// </summary>
    internal void BusLeave()
    {
        NPC pam = Game1.getCharacterFromName("Pam");
        if (!this.locations.BusStop.characters.Contains(pam) || pam.TilePoint is not { X: 21, Y: 10 })
            this.busMovement.DriveOut();
        else
        {
            pam.temporaryController = new PathFindController(pam, this.locations.BusStop, Values.BusDoorTile, 3,
                delegate (Character c, GameLocation _)
            {
                c.Position = new Vector2(-1000f, -1000f);
                c.controller = null; // TODO good?
                this.busMovement.DriveOut();
            });
        }
    }

    /// <summary>
    /// Handle Pam getting back to her regular schedule
    /// </summary>
    internal void PamBackToSchedule()
    {
        NPC pam = Game1.getCharacterFromName("Pam");
        if (!this.locations.BusStop.characters.Contains(pam))
            return;

        pam.Position = Values.BusDoorTile.ToVector2() * 64f;
        pam.temporaryController = new PathFindController(pam, this.locations.BusStop, new Point(21, 10), 2,
            delegate (Character c, GameLocation location)
        {
            if (c is NPC p)
            {
                Point? previousEndPoint = (Point?)AccessTools.Field(typeof(NPC), "previousEndPoint").GetValue(p);
                if (previousEndPoint is { X: 21, Y: 10 })
                {
                    p.temporaryController = new PathFindController(p, location, new Point(21, 10), 2, null);
                }
            }
        });
    }
}
