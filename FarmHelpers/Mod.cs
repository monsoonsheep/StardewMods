global using StardewValley;
global using StardewModdingAPI;
global using StardewModdingAPI.Events;
global using StardewMods.Common;

using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Characters;
using StardewMods.FarmHelpers.Framework;
using StardewValley.Locations;

namespace StardewMods.FarmHelpers;

public class Mod : StardewModdingAPI.Mod
{
    internal static Mod Instance = null!;

    internal static IModEvents Events { get; private set; } = null!;
    internal static IManifest Manifest { get; private set; } = null!;
    internal static IModHelper ModHelper { get; private set; } = null!;
    internal static IGameContentHelper GameContent { get; private set; } = null!;
    internal static Harmony Harmony { get; private set; } = null!;

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
        _ = new HelperManager();

        _ = new ItachiHouseFixes();
    }
}
