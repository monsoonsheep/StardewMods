using StardewMods.FoodJoints.Framework.Data.Models;
using StardewMods.FoodJoints.Framework.Interfaces;

namespace StardewMods.FoodJoints.Framework.Services;
internal class ConfigManager
{
    internal static ConfigManager Instance = null!;

    internal ConfigManager()
    {
        Instance = this;
        Mod.Config = Mod.ModHelper.ReadConfig<ConfigModel>();
        this.InitializeGmcm();
    }

    internal void InitializeGmcm()
    {
        IModHelper helper = Mod.ModHelper;
        IManifest manifest = Mod.Manifest;

        // get Generic Mod Config Menu's API (if it's installed)
        IGenericModConfigMenuApi? configMenu = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (configMenu == null)
            return;

        // register mod
        configMenu.Register(
            mod: manifest,
            reset: () => Mod.Config = new ConfigModel(),
            save: () => helper.WriteConfig(Mod.Config)
            );

        configMenu.AddBoolOption(
            mod: manifest,
            getValue: () => Mod.Config.ShowPricesInFoodMenu,
            setValue: (value) => Mod.Config.ShowPricesInFoodMenu = value,
            name: () => "Show Prices in Food Menu");

        configMenu.AddSectionTitle(
            mod: manifest,
            text: () => "Customers"
            );

        configMenu.AddBoolOption(
            mod: manifest,
            getValue: () => Mod.Config.WarpCustomers,
            setValue: (value) => Mod.Config.WarpCustomers = value,
            name: () => "Warp Customers",
            tooltip: () => "Teleport customers through unloaded locations to save time for their schedules and speed up visits");

        configMenu.AddNumberOption(
            mod: manifest,
            getValue: () => Mod.Config.MinutesBeforeCustomersLeave,
            setValue: (value) => Mod.Config.MinutesBeforeCustomersLeave = value,
            name: () => "Minutes Before Customers Leave",
            tooltip: () => "How many minutes (game time) customers wait for their order before they leave",
            min: 20,
            max: 720,
            interval: 10,
            formatValue: (value) => value + "m"
        );

        configMenu.AddNumberOption(
            mod: manifest,
            getValue: () => Mod.Config.EnableNpcCustomers,
            setValue: (value) => Mod.Config.EnableNpcCustomers = value,
            name: () => "NPC Customers",
            tooltip: () => "How often villager/townspeople customers will visit",
            min: 0,
            max: 5,
            interval: 1,
            formatValue: (value) => value == 0 ? "Disabled" : value.ToString()
        );

        configMenu.AddNumberOption(
            mod: manifest,
            getValue: () => Mod.Config.EnableCustomCustomers,
            setValue: (value) => Mod.Config.EnableCustomCustomers = value,
            name: () => "Custom Customers",
            tooltip: () => "How often custom-made customers will visit",
            min: 0,
            max: 5,
            interval: 1,
            formatValue: (value) => value == 0 ? "Disabled" : value.ToString()
            );

        configMenu.AddNumberOption(
            mod: manifest,
            getValue: () => Mod.Config.EnableRandomlyGeneratedCustomers,
            setValue: (value) => Mod.Config.EnableRandomlyGeneratedCustomers = value,
            name: () => "Randomly Generated Customers",
            tooltip: () => "How often randomly generated customers will visit",
            min: 0,
            max: 5,
            interval: 1,
            formatValue: (value) => value == 0 ? "Disabled" : value.ToString()
            );

        configMenu.AddSectionTitle(
            mod: manifest,
            text: () => "Tables"
            );

        configMenu.AddNumberOption(
            mod: manifest,
            getValue: () => Mod.Config.DistanceForSignboardToRegisterTables,
            setValue: (value) => Mod.Config.DistanceForSignboardToRegisterTables = value,
            name: () => "Distance to register tables",
            tooltip: () => "Radius from the signboard to detect tables",
            min: 3,
            max: 25,
            interval: 1,
            formatValue: (value) => value + " tiles"
            );
    }
}
