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
public class ModEntry : Mod
{
    internal static ModEntry Instance = null!;

    public ModEntry()
        => Instance = this;

    public override void Entry(IModHelper helper)
    {
        this.Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        new NpcMovement();
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        
    }
}
