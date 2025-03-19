global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using System.Threading.Tasks;
global using HarmonyLib;
global using Microsoft.Xna.Framework;
global using StardewModdingAPI;
global using StardewModdingAPI.Events;
global using StardewValley;
global using StardewMods.Common;
using StardewMods.SheepCore.Framework.Services;

namespace StardewMods.SheepCore;
public class Mod : StardewModdingAPI.Mod
{
    internal static Mod Instance = null!;

    internal static Harmony Harmony { get; private set; } = null!;
    internal static IModEvents Events { get; private set; } = null!;
    internal static Pathfinding Pathfinding => Pathfinding.Instance;
    public Mod()
        => Instance = this;

    public override void Entry(IModHelper helper)
    {
        Log.Monitor = base.Monitor;
        Harmony = new Harmony(base.ModManifest.UniqueID);
        Events = this.Helper.Events;
        this.Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        new Pathfinding();
        Pathfinding.Instance.Initialize();

        new LocationProvider();
        LocationProvider.Instance.Initialize();
    }
}
