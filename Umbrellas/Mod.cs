global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using HarmonyLib;
global using Microsoft.Xna.Framework;
global using StardewModdingAPI;
global using StardewModdingAPI.Events;
global using StardewValley;


namespace StardewMods.Umbrellas;
public class Mod : StardewModdingAPI.Mod
{
    internal static Mod Instance = null!;

    public Mod()
        => Instance = this;

    internal static Harmony Harmony { get; private set; } = null!;

    public override void Entry(IModHelper helper)
    {
        this.Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        Harmony = new Harmony(this.ModManifest.UniqueID);


    }
}
