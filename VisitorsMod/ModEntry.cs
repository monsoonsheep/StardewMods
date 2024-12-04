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

public class ModEntry : Mod
{
    internal static ModEntry Instance = null!;

    internal static IModEvents Events { get; private set; } = null!;
    internal static IManifest Manifest { get; private set; } = null!;
    internal new static IModHelper Helper { get; private set; } = null!;
    internal static Harmony Harmony { get; private set; } = null!;
    internal static NetState NetState { get; private set; } = null!;
    internal static Content ContentPacks { get; private set; } = null!;
    internal static VisitorManager Visitors { get; private set; } = null!;
    internal static NpcMovement NpcMovement { get; private set; } = null!;
    internal static RandomSprites RandomSprites { get; private set; } = null!;

    public ModEntry()
        => Instance = this;

    public override void Entry(IModHelper helper)
    {
        Log.Monitor = base.Monitor;

        Events = base.Helper.Events;
        Manifest = base.ModManifest;
        Helper = base.Helper;
        Harmony = new Harmony(this.ModManifest.UniqueID);

        NetState = new NetState();
        ContentPacks = new Content();

        Visitors = new VisitorManager();
        RandomSprites = new RandomSprites(new ColorsManager());
        I18n.Init(Helper.Translation);
        Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
    }

    /// <summary>
    /// Run once, wire up the dependency injection
    /// </summary>
    [EventPriority(EventPriority.Low)]
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        NpcMovement = NpcMovement.Instance;

        NetState.Initialize();
        ContentPacks.Initialize();
        RandomSprites.Initialize();
        Visitors.Initialize();
    }
}
