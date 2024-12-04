global using StardewValley;
global using StardewModdingAPI;
global using StardewModdingAPI.Events;
global using StardewMods.Common;
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using HarmonyLib;
global using Microsoft.Xna.Framework;

using StardewMods.BusSchedules.Framework.Services;
using StardewMods.BusSchedules.Framework.Api;
using StardewMods.BusSchedules.Framework.Data;
using StardewMods.SheepCore.Framework.Services;

namespace StardewMods.BusSchedules;
public class Mod : StardewModdingAPI.Mod
{
    internal static Mod Instance = null!;

    internal IModEvents Events { get; private set; } = null!;
    internal IManifest Manifest { get; private set; } = null!;
    internal IReflectionHelper Reflection {  get; private set; } = null!;
    internal IMultiplayerHelper Multiplayer { get; private set; } = null!;
    internal Harmony Harmony { get; private set; } = null!;
    internal LocationProvider Locations { get; private set; } = null!;
    internal ModEvents ModEvents { get; private set; } = null!;
    internal BusManager BusManager { get; private set; } = null!;
    internal NpcArrivals NpcArrivals { get; private set; } = null!;
    internal NpcSchedulesIntercept NpcSchedulesIntercept { get; private set; } = null!;

    internal readonly Dictionary<string, VisitorData> VisitorsData = [];

    public Mod()
        => Instance = this;

    public override void Entry(IModHelper helper)
    {
        I18n.Init(this.Helper.Translation);
        this.Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
    }

    [EventPriority(EventPriority.High)]
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        Log.Monitor = base.Monitor;

        this.Harmony = new Harmony(this.ModManifest.UniqueID);
        this.Events = base.Helper.Events;
        this.Manifest = base.ModManifest;
        this.Reflection = base.Helper.Reflection;
        this.Multiplayer = base.Helper.Multiplayer;
        this.Locations = new LocationProvider();
        this.ModEvents = new ModEvents();

        this.BusManager = new BusManager();
        this.BusManager.Initialize();
        this.NpcArrivals = new NpcArrivals();
        this.NpcArrivals.Initialize();
        this.NpcSchedulesIntercept = new NpcSchedulesIntercept();
        this.NpcSchedulesIntercept.Initialize();
    }

    public override object GetApi(IModInfo mod)
    {
        return new BusSchedulesApi(
            this.NpcArrivals,
            this.BusManager);
    }

    /// <summary>
    /// When NPC reaches the bus at the end of their visit, move them out of the map and freeze them
    /// </summary>
    public static void VisitorReachBusEndBehavior(Character c, GameLocation location)
    {
        if (c is NPC npc)
        {
            npc.Position = new Vector2(-1000, -1000);
            npc.controller = null;
            npc.followSchedule = false;
        }
    }
}
