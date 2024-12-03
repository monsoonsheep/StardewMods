global using StardewValley;
global using StardewModdingAPI;
global using StardewModdingAPI.Events;
global using MonsoonSheep.Stardew.Common;

using SimpleInjector;
using HarmonyLib;
using StardewMods.VisitorsMod.Framework.Services;
using StardewMods.VisitorsMod.Framework.Services.Visitors;
using StardewMods.VisitorsMod.Framework.Data;
using Microsoft.Xna.Framework.Graphics;
using StardewMods.VisitorsMod.Framework.Services.Visitors.RandomVisitors;
using StardewMods.VisitorsMod.Framework;
using StardewMods.VisitorsMod.Framework.Interfaces;
using StardewMods.VisitorsMod.Framework.Visitors;
using StardewMods.VisitorsMod.Framework.Services.Visitors.Activities;

namespace StardewMods.VisitorsMod;

public class ModEntry : Mod
{
    internal static ModEntry Instance = null!;
    private SimpleInjector.Container _container = null!;

    
    public ModEntry() => Instance = this;

    public override void Entry(IModHelper helper)
    {
        I18n.Init(this.Helper.Translation);
        this.Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
    }

    /// <summary>
    /// Run once, wire up the dependency injection
    /// </summary>
    [EventPriority(EventPriority.Low)]
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        // Init
        SimpleInjector.Container c = new SimpleInjector.Container();
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
        c.RegisterSingleton<ContentPacks>();
        c.RegisterSingleton<LocationProvider>();
        c.RegisterSingleton<MultiplayerMessaging>();
        c.RegisterSingleton<NetState>();

        c.RegisterSingleton<VisitorManager>();
        c.RegisterSingleton<NpcMovement>();
        c.RegisterSingleton<RandomVisitorBuilder>();
        c.RegisterSingleton<RandomSprites>();
        c.RegisterSingleton<Colors>();
        c.RegisterSingleton<ActivityManager>();

        List<Type> spawners = [
            typeof(TrainSpawner),
            typeof(RoadSpawner),
            typeof(WarpSpawner)
            ];
        c.Collection.Register<ISpawner>(spawners, Lifestyle.Singleton);

        IBusSchedulesApi? busSchedules = this.Helper.ModRegistry.GetApi<IBusSchedulesApi>("MonsoonSheep.BusSchedules");
        if (busSchedules != null)
        {
            c.RegisterInstance<IBusSchedulesApi>(busSchedules);
            c.Collection.Append(typeof(ISpawner), typeof(BusSpawner));
        }

#if DEBUG
        c.RegisterSingleton<Debug>();
#endif

        c.Verify();
    }

    internal static Texture2D GenerateTexture(GeneratedSpriteData data)
    {
        return Instance._container.GetInstance<RandomSprites>().GenerateTexture(data);
    }
}
