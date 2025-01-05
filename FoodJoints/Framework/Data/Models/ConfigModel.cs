namespace StardewMods.FoodJoints.Framework.Data.Models;

internal class ConfigModel
{
    public int DistanceForSignboardToRegisterTables { get; set; } = 7;
    public bool EnableScrollbarInMenuBoard { get; set; } = false;
    public bool WarpCustomers { get; set; } = true;
    public int EnableCustomCustomers { get; set; } = 4;
    public int EnableRandomlyGeneratedCustomers { get; set; } = 4;
    public int EnableNpcCustomers { get; set; } = 2;
    public List<MenuCategoryArchive> MenuCategories { get; set; } = [];
    public int MinutesBeforeCustomersLeave { get; set; } = 160;
    public bool ShowPricesInFoodMenu { get; set; } = true;

#if YOUTUBE
    public string YoutubeClientId { get; set; } = "Your Client Id here";
    public string YoutubeClientSecret { get; set; } = "Your Client Secret here";
#endif

#if TWITCH
    public string TwitchClientId { get; set; } = "Your Twitch OAuth Password here";
    public string TwitchClientSecret { get; set; } = "Your Twitch OAuth Password here";
#endif
}
