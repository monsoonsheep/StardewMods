using MyCafe.Framework.Interfaces;
using StardewModdingAPI;

namespace MyCafe.Framework
{
    internal static class Config
    {
        internal static ConfigModel LoadedConfig;

        internal static void Initialize()
        {
            static string GetFrequencyText(int n)
            {
                return n switch
                {
                    1 => "Very Low",
                    2 => "Low",
                    3 => "Medium",
                    4 => "High",
                    5 => "Very High",
                    _ => "???"
                };
            }

            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = Mod.ModHelper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: Mod.ModManifest,
                reset: () => LoadedConfig = new ConfigModel(),
                save: () => Mod.ModHelper.WriteConfig(LoadedConfig)
            );

            // add some config options
            configMenu.AddNumberOption(
                mod: Mod.ModManifest,
                name: I18n.Menu_VisitorFrequency,
                tooltip: I18n.Menu_VisitorFrequencyTooltip,
                getValue: () => LoadedConfig.CustomerSpawnFrequency,
                setValue: value => LoadedConfig.CustomerSpawnFrequency = value,
                min: 1, max: 5, 
                formatValue: GetFrequencyText
            );

            configMenu.AddNumberOption(
                mod: Mod.ModManifest,
                name: I18n.Menu_NpcFrequency,
                tooltip: I18n.Menu_NpcFrequency_Tooltip,
                getValue: () => LoadedConfig.NpcCustomerSpawnFrequency,
                setValue: value => LoadedConfig.NpcCustomerSpawnFrequency = value,
                min: 1, max: 5, 
                formatValue: GetFrequencyText
            );
        }
    }

    internal class ConfigModel
    {
        public int CustomerSpawnFrequency { get; set; } = 2;

        public int NpcCustomerSpawnFrequency { get; set; } = 2;
    }
}
