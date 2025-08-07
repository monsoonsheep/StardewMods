global using StardewValley;
global using StardewModdingAPI;
global using StardewModdingAPI.Events;
global using StardewMods.Common;

global using Point = Microsoft.Xna.Framework.Point;

using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Characters;
using StardewMods.FarmHelpers.Framework;
using StardewValley.Locations;
using StardewMods.SheepCore.Framework.Services;

namespace StardewMods.FarmHelpers;

public class Mod : StardewModdingAPI.Mod
{
    internal static Mod Instance = null!;

    internal static IModEvents Events { get; private set; } = null!;
    internal static IManifest Manifest { get; private set; } = null!;
    internal static IModHelper ModHelper { get; private set; } = null!;
    internal static IGameContentHelper GameContent { get; private set; } = null!;
    internal static Harmony Harmony { get; private set; } = null!;


    // Dependencies
    internal static LocationProvider Locations = null!;

    internal static Pathfinding Pathfinding = null!;

    // Mod Services
    internal static JobHandler Jobs = null!;

    internal static Worker Worker = null!;

    internal static WorkerInventory HelperInventory = null!;

    internal static Movement Movement = null!;

    internal static Farmer FakeFarmer = null!;

    public Mod()
        => Instance = this;

    public override void Entry(IModHelper helper)
    {
        Log.Monitor = base.Monitor;

        ModHelper = base.Helper;
        Events = ModHelper.Events;
        Manifest = base.ModManifest;
        GameContent = base.Helper.GameContent;
        Harmony = new Harmony(this.ModManifest.UniqueID);

        ModHelper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

        Debug.Initialize();
    }

    /// <summary>
    /// Run once, wire up the dependency injection
    /// </summary>
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        Locations = LocationProvider.Instance;
        Pathfinding = Pathfinding.Instance;

        Worker = new Worker();
        Movement = new Movement();
        Jobs = new JobHandler();
        _ = new ItachiHouseFixes();
        HelperInventory = new WorkerInventory();
        FakeFarmer = new Farmer();
    }
}
