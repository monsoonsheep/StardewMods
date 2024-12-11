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
using StardewMods.ExtraNpcBehaviors.Framework.Data;

namespace StardewMods.ExtraNpcBehaviors;
public class Mod : StardewModdingAPI.Mod
{
    internal static Mod Instance = null!;

    public Mod()
        => Instance = this;

    internal static Harmony Harmony { get; private set; } = null!;
    internal EndBehaviors EndBehaviors { get; private set; } = null!;

    public override void Entry(IModHelper helper)
    {
        this.Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

        this.EndBehaviors = new EndBehaviors();
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        Harmony = new Harmony(this.ModManifest.UniqueID);

        NpcVirtualProperties.InjectFields();

        this.EndBehaviors.Initialize();
    }
}
