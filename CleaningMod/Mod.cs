using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using CleaningMod.Patching;
using MonsoonSheep.Stardew.Common.Patching;

namespace CleaningMod;

public class Mod : StardewModdingAPI.Mod
{
    internal static Mod Instance = null!;

    internal static Texture2D FurnitureDustTexture = null!;

    public override void Entry(IModHelper helper)
    {
        Instance = this;
        Log.Monitor = this.Monitor;

        //HarmonyPatcher.TryApply(this,
        //    new DrawPatcher());

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.Content.AssetRequested += this.OnAssetRequested;
        helper.Events.Display.RenderedWorld += this.OnRenderedWorld;

#if DEBUG
        helper.Events.Input.ButtonPressed += Debug.ButtonPress;
#endif

        FurnitureDustTexture = helper.ModContent.Load<Texture2D>("assets/dust.png");
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {

    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {

    }

    private void OnAssetRequested(object?sender, AssetRequestedEventArgs e)
    {
        
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
         // 
    }
}
