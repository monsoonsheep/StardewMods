using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StuffGathersDust.Patching;

namespace StuffGathersDust;

public class ModEntry : Mod
{
    internal static ModEntry Instance;

    internal new static IMonitor Monitor;
    internal static IModHelper ModHelper;
    internal new static IManifest ModManifest;

    internal static Texture2D FurnitureDustTexture;

    public override void Entry(IModHelper helper)
    {
        Instance = this;

        Monitor = base.Monitor;
        ModHelper = helper;
        ModManifest = base.ModManifest;
        Log.Monitor = Monitor;

        // Harmony patches
        try
        {
            var harmony = new Harmony(ModManifest.UniqueID);
            new List<PatchCollection>()
            {
                new DrawPatches(),
            }.ForEach(l => l.ApplyAll(harmony));
        }
        catch (Exception e)
        {
            Log.Error($"Couldn't patch methods - {e}");
            return;
        }

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.Content.AssetRequested += OnAssetRequested;

#if DEBUG
        helper.Events.Input.ButtonPressed += Debug.ButtonPress;
#endif
    }

    private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
    {
        FurnitureDustTexture = Game1.content.Load<Texture2D>("MonsoonSheep.CleaningMod_DustTexture");
    }

    private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
    {

    }

    private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
    {
        if (e.Name.IsEquivalentTo("MonsoonSheep.CleaningMod_DustTexture"))
        {
            e.LoadFromModFile<Texture2D>("assets/dust.png", AssetLoadPriority.Medium);
        }
    }
}