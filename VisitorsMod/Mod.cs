global using StardewValley;
global using StardewModdingAPI;
global using StardewModdingAPI.Events;
global using StardewMods.Common;

using HarmonyLib;
using StardewMods.VisitorsMod.Framework.Services;
using StardewMods.VisitorsMod.Framework.Services.Visitors;
using StardewMods.VisitorsMod.Framework.Data;
using Microsoft.Xna.Framework.Graphics;
using StardewMods.VisitorsMod.Framework.Services.Visitors.RandomVisitors;
using StardewMods.VisitorsMod.Framework.Interfaces;
using StardewMods.SheepCore.Framework.Services;

namespace StardewMods.VisitorsMod;

public class Mod : StardewModdingAPI.Mod
{
    internal static Mod Instance = null!;

    internal static IModEvents Events { get; private set; } = null!;
    internal static IManifest Manifest { get; private set; } = null!;
    internal static IModHelper ModHelper { get; private set; } = null!;
    internal static Harmony Harmony { get; private set; } = null!;
    internal static NetState NetState { get; private set; } = null!;
    internal static Content ContentPacks { get; private set; } = null!;
    internal static VisitorManager Visitors { get; private set; } = null!;
    internal static Pathfinding NpcMovement { get; private set; } = null!;
    internal static RandomSprites RandomSprites { get; private set; } = null!;

    public Mod()
        => Instance = this;

    public override void Entry(IModHelper helper)
    {

        Log.Monitor = base.Monitor;

        ModHelper = base.Helper;
        Events = ModHelper.Events;
        Manifest = base.ModManifest;
        Harmony = new Harmony(this.ModManifest.UniqueID);

        I18n.Init(ModHelper.Translation);

        NetState = new NetState();
        ContentPacks = new Content();

        Visitors = new VisitorManager();
        RandomSprites = new RandomSprites(new ColorsManager());

        ModHelper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
    }

    /// <summary>
    /// Run once, wire up the dependency injection
    /// </summary>
    [EventPriority(EventPriority.Low)]
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        NpcMovement = Pathfinding.Instance;

        NetState.Initialize();
        ContentPacks.Initialize();
        RandomSprites.Initialize();
        Visitors.Initialize();
    }
}
