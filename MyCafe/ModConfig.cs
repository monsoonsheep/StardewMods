using MyCafe.Interfaces;

namespace MyCafe;

internal static class ModConfig
{
    internal static void InitializeGmcm()
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
            reset: () => Mod.Config = new ConfigModel(),
            save: () => Mod.ModHelper.WriteConfig(Mod.Config)
        );
    }
}

internal class ConfigModel
{
    public bool EnableScrollbarInMenuBoard { get; set; } = false;
    public int CustomerSpawnFrequency { get; set; } = 2;
    public int NpcCustomerSpawnFrequency { get; set; } = 2;
    public string YoutubeClientId { get; set; } = "Your Client Id here";
    public string YoutubeClientSecret { get; set; } = "Your Client Secret here";
    public string TwitchClientId { get; set; } = "Your Twitch OAuth Password here";
    public string TwitchClientSecret { get; set; } = "Your Twitch OAuth Password here";
}