global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using System.Threading.Tasks;
global using HarmonyLib;
global using Microsoft.Xna.Framework;
global using StardewMods.MyShops.Framework;
global using StardewModdingAPI;
global using StardewModdingAPI.Events;
global using StardewValley;
global using MonsoonSheep.Stardew.Common;

using SimpleInjector;
using StardewMods.MyShops.Framework.Services;

namespace StardewMods.MyShops;
public class ModEntry : Mod
{
    internal static ModEntry Instance = null!;
    private Container _container = null!;

    public ModEntry() => Instance = this;

    public override void Entry(IModHelper helper)
    {
        I18n.Init(this.Helper.Translation);
        this.Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
    }

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
        c.RegisterSingleton<LocationProvider>();

        c.Verify();
    }
}
