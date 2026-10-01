global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using HarmonyLib;
global using Microsoft.Xna.Framework;
global using StardewModdingAPI;
global using StardewModdingAPI.Events;
global using StardewValley;
using StardewMods.DisableQuestNotification.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace StardewMods.DisableQuestNotification;

public class Mod : StardewModdingAPI.Mod
{
    internal static Mod Instance = null!;

    public Mod()
        => Instance = this;

    private bool ticking = false;

    internal ModConfig Config { get; private set; } = null!;
    internal static Harmony Harmony { get; private set; } = null!;

    public override void Entry(IModHelper helper)
    {
        this.Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

        this.Config = this.Helper.ReadConfig<ModConfig>();

        this.Ping();
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        // get Generic Mod Config Menu's API (if it's installed)
        IGenericModConfigMenuApi? configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (configMenu is null)
            return;

        // register mod
        configMenu.Register(
            mod: this.ModManifest,
            reset: () => this.Config = new ModConfig(),
            save: () => this.Helper.WriteConfig(this.Config)
        );

        // add some config options
        configMenu.AddBoolOption(
            mod: this.ModManifest,
            name: () => "Enable Mod",
            tooltip: () => "Enable this mod, disabling the pulsing",
            getValue: () => this.Config.EnableMod,
            setValue: value =>
            {
                this.Config.EnableMod = value;
                this.Ping();
            });
    }

    private void Ping()
    {
        if (this.Config.EnableMod && !this.ticking)
        {
            this.Helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            this.ticking = true;
        }
        else if (!this.Config.EnableMod && this.ticking)
        {
            this.Helper.Events.GameLoop.UpdateTicked -= this.OnUpdateTicked;
            this.ticking = false;
        }
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        Game1.dayTimeMoneyBox.questPulseTimer = 0;
        Game1.dayTimeMoneyBox.whenToPulseTimer = 0;
    }
}
