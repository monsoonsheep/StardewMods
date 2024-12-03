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
using SimpleInjector;
using StardewMods.BusSchedules.Framework.Api;
using StardewMods.BusSchedules.Framework.Data;

namespace StardewMods.BusSchedules;
public class ModEntry : Mod
{
    internal static ModEntry Instance = null!;
    private Container _container = null!;

    public ModEntry() => Instance = this;

    // Services
    internal readonly Dictionary<string, VisitorData> VisitorsData = [];

    public override void Entry(IModHelper helper)
    {
        I18n.Init(this.Helper.Translation);
        this.Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
    }

    [EventPriority(EventPriority.High)]
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        // Init
        Container c = new Container();
        this._container = c;

        c.RegisterSingleton(() => new Harmony(this.ModManifest.UniqueID));
        c.RegisterInstance(this.Helper);
        c.RegisterInstance(this.ModManifest);
        c.RegisterInstance(this.Monitor);
        c.RegisterInstance(this.Helper.Data);
        c.RegisterInstance(this.Helper.Events);
        c.RegisterInstance(this.Helper.GameContent);
        c.RegisterInstance(this.Helper.Input);
        c.RegisterInstance(this.Helper.ModContent);
        c.RegisterInstance(this.Helper.ModRegistry);
        c.RegisterInstance(this.Helper.Reflection);
        c.RegisterInstance(this.Helper.Translation);
        c.RegisterInstance(this.Helper.Multiplayer);

        c.RegisterSingleton<ILogger, Logger>();

        c.RegisterSingleton<ModEvents>();
        c.RegisterSingleton<AssetHandler>();
        c.RegisterSingleton<LocationProvider>();
        c.RegisterSingleton<MultiplayerMessaging>();

        c.RegisterSingleton<BusManager>();
        c.RegisterSingleton<BusMovement>();
        c.RegisterSingleton<Timings>();
        c.RegisterSingleton<NpcArrivals>();
        c.RegisterSingleton<NpcSchedulesIntercept>();

        c.RegisterInstance<Dictionary<string, VisitorData>>(this.VisitorsData);

        c.Verify();
    }

    public override object GetApi(IModInfo mod)
    {
        return new BusSchedulesApi(
            this._container.GetInstance<NpcArrivals>(),
            this._container.GetInstance<Timings>(),
            this._container.GetInstance<BusManager>()
            );
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
