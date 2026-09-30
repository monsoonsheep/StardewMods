global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using HarmonyLib;
global using Microsoft.Xna.Framework;
global using StardewModdingAPI;
global using StardewModdingAPI.Events;
global using StardewValley;
using DisableQuestNotification.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace DisableQuestNotification;

public class Mod : StardewModdingAPI.Mod
{
    internal static Mod Instance = null!;

    public Mod()
        => Instance = this;

    internal static Harmony Harmony { get; private set; } = null!;

    public override void Entry(IModHelper helper)
    {
        this.Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        this.Helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;

        this.Helper.Events.Content.AssetRequested += this.OnAssetRequested;
        this.Helper.Events.Content.AssetReady += this.OnAssetReady;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        Harmony = new Harmony(this.ModManifest.UniqueID);

        // example patch

        // Harmony.Patch(
        //     original: AccessTools.Method(typeof(NPC), nameof(NPC.draw), [typeof(SpriteBatch), typeof(float)]),
        //     postfix: new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(After_NpcDraw)))
        // );
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {

    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Mods/MonsoonSheep.DisableQuestNotification/MyAsset"))
        {
            // edit asset
        }
    }

    private void OnAssetReady(object? sender, AssetReadyEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Mods/MonsoonSheep.DisableQuestNotification/MyAsset"))
        {
            // edit asset
        }
    }
}
