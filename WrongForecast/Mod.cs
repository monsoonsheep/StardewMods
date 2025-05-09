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
using StardewValley.Extensions;
using StardewValley.Objects;
using StardewValley.Pathfinding;

namespace StardewMods.WrongForecast;

public class Mod : StardewModdingAPI.Mod
{
    internal static Mod Instance = null!;

    internal static Harmony Harmony { get; private set; } = null!;
    internal static ConfigModel Config { get; private set; } = null!;
    internal static IModEvents Events { get; private set; } = null!;
    public Mod()
        => Instance = this;

    private bool scienceLiesToday = false;

    private string fakeWeatherIdToday = "Rain";

    private string[] weatherIdsToTarget = ["Sun", "Rain", "Storm"];

    public override void Entry(IModHelper helper)
    {
        Config = this.Helper.ReadConfig<ConfigModel>();
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

    private static bool Before_getWeatherForecast(TV __instance, ref string weatherId, ref string __result)
    {
        if (Instance.scienceLiesToday)
        {
            weatherId = Instance.fakeWeatherIdToday;
        }

        return true;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.InitializeGmcm();
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        this.scienceLiesToday = Game1.random.NextBool(Config.PercentageChanceOfLying / 100f);

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

    private void InitializeGmcm()
    {
        IModHelper helper = this.Helper;
        IManifest manifest = this.ModManifest;

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

        configMenu.AddNumberOption(
            mod: manifest,
            getValue: () => Config.PercentageChanceOfLying,
            setValue: (value) => Config.PercentageChanceOfLying = value,
            name: () => "Percentage chance of wrong forecast",
            min: 0,
            max: 100,
            interval: 1,
            formatValue: (value) => $"{value}%"
        );
    }
}
