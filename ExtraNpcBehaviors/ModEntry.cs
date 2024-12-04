global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using HarmonyLib;
global using Microsoft.Xna.Framework;
global using StardewModdingAPI;
global using StardewModdingAPI.Events;
global using StardewValley;
global using StardewMods.Common;

using StardewMods.ExtraNpcBehaviors.Framework;

namespace StardewMods.ExtraNpcBehaviors;
public class ModEntry : Mod
{
    internal static ModEntry Instance = null!;

    public ModEntry()
        => Instance = this;

    internal Harmony Harmony { get; private set; } = null!;
    internal EndBehaviors EndBehaviors { get; private set; } = null!;

    public override void Entry(IModHelper helper)
    {
        this.Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

        this.EndBehaviors = new EndBehaviors();
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.Harmony = new Harmony(this.ModManifest.UniqueID);

        this.EndBehaviors.Initialize();
    }
}
