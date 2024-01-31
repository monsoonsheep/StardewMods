using MyCafe.Interfaces;
using StardewModdingAPI;

namespace MyCafe;

internal static class ModConfig
{
    internal static void InitializeGmcm(IModHelper helper, IManifest manifest)
    {
        // get Generic Mod Config Menu's API (if it's installed)
        var configMenu = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (configMenu is null)
            return;

        // register mod
        configMenu.Register(
            mod: manifest,
            reset: () => Mod.Instance.Config = new ConfigModel(),
            save: () => helper.WriteConfig(Mod.Instance.Config)
        );
    }
}

internal class ConfigModel
{
    public int DistanceForSignboardToRegisterTables { get; set; } = 7;
    public bool EnableScrollbarInMenuBoard { get; set; } = false;
    public int CustomerSpawnFrequency { get; set; } = 2;
    public int NpcCustomerSpawnFrequency { get; set; } = 2;

#if YOUTUBE
    public string YoutubeClientId { get; set; } = "Your Client Id here";
    public string YoutubeClientSecret { get; set; } = "Your Client Secret here";
#endif

#if TWITCH
    public string TwitchClientId { get; set; } = "Your Twitch OAuth Password here";
    public string TwitchClientSecret { get; set; } = "Your Twitch OAuth Password here";
#endif
}