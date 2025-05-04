global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using System.Threading.Tasks;
global using HarmonyLib;
global using Microsoft.Xna.Framework;
global using StardewModdingAPI;
global using StardewModdingAPI.Events;
global using StardewValley;
using StardewMods.WrongForecast.Framework.Services;
using StardewValley.Extensions;
using StardewValley.Objects;
using StardewValley.Pathfinding;

namespace StardewMods.WrongForecast;

public class Mod : StardewModdingAPI.Mod
{
    internal static Mod Instance = null!;

    internal static Harmony Harmony { get; private set; } = null!;
    internal static IModEvents Events { get; private set; } = null!;
    public Mod()
        => Instance = this;

    public int percentageChanceOfLying = 5;

    private bool scienceLiesToday = false;

    private string fakeWeatherIdToday = "Rain";

    private string[] weatherIdsToTarget = ["Sun", "Rain", "Storm"];

    public override void Entry(IModHelper helper)
    {
        Log.Monitor = base.Monitor;
        Harmony = new Harmony(base.ModManifest.UniqueID);
        Events = this.Helper.Events;
        this.Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        this.Helper.Events.GameLoop.DayStarted += this.OnDayStarted;
        this.Helper.Events.GameLoop.DayEnding += this.OnDayEnding;

        Harmony.Patch(
            AccessTools.Method(typeof(TV), "getWeatherForecast", [typeof(string)]),
            prefix: new HarmonyMethod(this.GetType(), nameof(Before_getWeatherForecast))
            );
    }

    private bool Before_getWeatherForecast(TV __instance, string weatherId, ref string __result)
    {
        if (this.scienceLiesToday)
        {
            weatherId = this.fakeWeatherIdToday;
        }

        return true;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {

    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        this.scienceLiesToday = Game1.random.NextBool(this.percentageChanceOfLying / 100f);

        if (this.scienceLiesToday)
        {

            WorldDate tomorrow = new WorldDate(Game1.Date);
            int totalDays = tomorrow.TotalDays + 1;
            tomorrow.TotalDays = totalDays;
            string weatherId = ((!Game1.IsMasterGame) ? Game1.getWeatherModificationsForDate(tomorrow, Game1.netWorldState.Value.WeatherForTomorrow) : Game1.getWeatherModificationsForDate(tomorrow, Game1.weatherForTomorrow));

            if (this.weatherIdsToTarget.Contains(weatherId))
            {
                var lies = this.weatherIdsToTarget.Where(i => i != weatherId).ToList();

                this.fakeWeatherIdToday = lies[Game1.random.Next(lies.Count)];
            }
            else
            {
                this.scienceLiesToday = false;
            }
        }

    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        this.scienceLiesToday = false;
    }
}
