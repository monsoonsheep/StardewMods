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


using StardewMods.CustomFarmerAnimations.Framework;
using StardewValley.Locations;
using StardewMods.CustomFarmerAnimations.Framework.SpriteEditing;


namespace StardewMods.CustomFarmerAnimations;

public class Mod : StardewModdingAPI.Mod
{
    internal static Mod Instance = null!;

    internal IGenericModConfigMenuApi GmcmApi = null!;
    internal ModConfig Config = null!;

    internal Dictionary<string, CustomFarmerAnimationModel> Entries = [];

    internal HashSet<string> ActivatedAnimations = [];

    public Mod()
        => Instance = this;

    public override void Entry(IModHelper helper)
    {
        this.Config = new ModConfig();
        Log.Monitor = base.Monitor;

        this.Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        this.Helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        this.Helper.Events.Content.AssetRequested += this.OnAssetRequested;
        this.Helper.Events.Content.AssetReady += this.OnAssetReady;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.GmcmApi = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu") ?? throw new Exception("Couldn't load GMCM");
        this.Config = this.Helper.ReadConfig<ModConfig>();
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.Entries = Game1.content.Load<Dictionary<string, CustomFarmerAnimationModel>>("Mods/MonsoonSheep.CustomFarmerAnimations/Entries");
        this.ActivatedAnimations = this.Config.ActiveAnimations.ToHashSet();
        this.InvalidateFarmerSprites();
        this.SetupGmcm();
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Mods/MonsoonSheep.CustomFarmerAnimations/Entries"))
        {
            e.LoadFrom(() => new Dictionary<string, CustomFarmerAnimationModel>(), AssetLoadPriority.Low);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo("Characters/Farmer/farmer_base")
            || e.NameWithoutLocale.IsEquivalentTo("Characters/Farmer/farmer_girl_base"))
        {
            e.Edit((data) =>
            {
                IAssetDataForImage imageData = data.AsImage();
                foreach (string key in this.ActivatedAnimations)
                {
                    CustomFarmerAnimationModel model = this.Entries[key];

                    foreach (EditOperation edit in model.EditOperations)
                    {
                        edit.Execute(imageData);
                    }
                }
            });
        }
    }

    private void OnAssetReady(object? sender, AssetReadyEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Mods/MonsoonSheep.CustomFarmerAnimations/Entries"))
        {
            var previous = this.Entries.Keys;
            this.Entries = Game1.content.Load<Dictionary<string, CustomFarmerAnimationModel>>("Mods/MonsoonSheep.CustomFarmerAnimations/Entries");
            // If Entries has changed, update the Config Menu
            if (previous.Count != this.Entries.Count || previous.Any(i => !this.Entries.ContainsKey(i)))
            {
                this.SetupGmcm();
            }
        }
    }

    private void InvalidateFarmerSprites()
    {
        this.Helper.GameContent.InvalidateCache("Characters/Farmer/farmer_base");
        this.Helper.GameContent.InvalidateCache("Characters/Farmer/farmer_girl_base");
    }

    private void SetupGmcm()
    {
        // get Generic Mod Config Menu's API (if it's installed)
        IGenericModConfigMenuApi configMenu = this.GmcmApi;

        configMenu.Unregister(this.ModManifest);

        // register mod
        configMenu.Register(
            mod: this.ModManifest,
            reset: () => this.Config = new ModConfig(),
            save: () => this.Helper.WriteConfig(this.Config)
        );

        configMenu.AddSectionTitle(
            mod: this.ModManifest,
            text: () => "Activated Animations"
        );

        foreach (var entry in this.Entries)
        {
            configMenu.AddBoolOption(
            mod: this.ModManifest,
            name: () => entry.Value.Name,
            tooltip: () => $"Check this to activate \"{entry.Value.Name}\"",
            getValue: () => this.ActivatedAnimations.Contains(entry.Key),
            setValue: value =>
                {
                    if (value)
                        this.ActivatedAnimations.Add(entry.Key);
                    else
                        this.ActivatedAnimations.Remove(entry.Key);

                    this.InvalidateFarmerSprites();
                    this.Config.ActiveAnimations = this.ActivatedAnimations.ToList();
                    this.Helper.WriteConfig(this.Config);
                }
            );
        }
    }
}

