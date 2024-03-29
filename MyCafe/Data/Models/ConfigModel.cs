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
    public int EnableCustomCustomers { get; set; } = 5;
    public int EnableRandomlyGeneratedCustomers { get; set; } = 5;
    public int EnableNpcCustomers { get; set; } = 5;

#if YOUTUBE
    public string YoutubeClientId { get; set; } = "Your Client Id here";
    public string YoutubeClientSecret { get; set; } = "Your Client Secret here";
#endif

#if TWITCH
    public string TwitchClientId { get; set; } = "Your Twitch OAuth Password here";
    public string TwitchClientSecret { get; set; } = "Your Twitch OAuth Password here";
#endif
}
