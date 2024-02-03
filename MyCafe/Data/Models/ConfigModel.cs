using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCafe.Data.Models;

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
