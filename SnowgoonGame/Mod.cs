global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using System.Threading.Tasks;
global using HarmonyLib;
global using Microsoft.Xna.Framework;
global using StardewModdingAPI;
global using StardewModdingAPI.Events;
global using StardewMods.Common;
global using StardewValley;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewMods.SnowgoonGame.Framework;
using StardewValley.Characters;
using StardewValley.TokenizableStrings;

namespace StardewMods.SnowgoonGame;
public class Mod : StardewModdingAPI.Mod
{
    internal static Mod Instance = null!;

    private Harmony harmony = null!;

    internal static Harmony Harmony => Instance.harmony;
    internal static IModHelper ModHelper => Instance.Helper;
    internal static IMultiplayerHelper Multiplayer => Instance.Helper.Multiplayer;
    internal static IManifest Manifest => Instance.ModManifest;
    internal static IReflectionHelper Reflection => Instance.Helper.Reflection;
    internal static IModEvents Events => Instance.Helper.Events;

    public Mod()
        => Instance = this;

    public override void Entry(IModHelper helper)
    {
        Log.Monitor = base.Monitor;
        I18n.Init(this.Helper.Translation);
        this.harmony = new Harmony(base.ModManifest.UniqueID);

        this.Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
       
    }
}
