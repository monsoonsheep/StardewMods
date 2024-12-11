using System.Text.RegularExpressions;
using StardewMods.SheepCore;
using StardewMods.VisitorsMod.Framework.Data.Models.Activities;

namespace StardewMods.VisitorsMod.Framework.Services;

internal class ActivityManager
{
    internal static ActivityManager Instance = null!;

    internal Dictionary<string, ActivityModel> Activities = [];

    public ActivityManager()
        => Instance = this;

    internal void Initialize()
    {
        Mod.Events.Content.AssetRequested += this.OnAssetRequested;

        this.Activities = Mod.ModHelper.GameContent.Load<Dictionary<string, ActivityModel>>("Mods/MonsoonSheep.VisitorsMod/Activities");
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Mods/MonsoonSheep.VisitorsMod/Activities"))
        {
            e.LoadFrom(() => {
                Dictionary<string, ActivityModel> activities = new Dictionary<string, ActivityModel>();

                DirectoryInfo folder = new DirectoryInfo(Path.Combine(Mod.ModHelper.DirectoryPath, "assets", "Activities"));

                foreach (FileInfo f in folder.GetFiles())
                {
                    string id = f.Name.Replace(".json", "");
                    activities[id] = Mod.ModHelper.ModContent.Load<ActivityModel>(Path.Combine("assets", "Activities", $"{f.Name}"));
                    activities[id].Id = id;
                }

                return activities;
            }, AssetLoadPriority.Medium);
        }
    }

    internal List<ActivityModel> GetActivitiesForToday()
    {
        List<ActivityModel> list = [];

        foreach (ActivityModel activity in this.Activities.Values)
        {
            string[] split = activity.Schedule.Split(' ');

            List<WorldDate> dates = [];
            List<DayOfWeek> daysOfWeekExclude = [];
            int startingyear = 1;

            Regex rDayOfWeekExclude = new Regex(@"-(mon|tue|wed|thu|fri|sat|sun)");
            Regex rDateEveryMonth = new Regex(@"(\d+)");
            Regex rDateOfMonth = new Regex(@"(\d+)_(spring|summer|fall|winter)");
            Regex rYear = new Regex(@"y(\d+)");

            foreach (string s in split)
            {
                Match dayOfWeekExclude = rDayOfWeekExclude.Match(s);
                Match dateEveryMonth = rDateEveryMonth.Match(s);
                Match dateOfMonth = rDateOfMonth.Match(s);
                Match year = rYear.Match(s);

                if (year.Success)
                {
                    startingyear = int.Parse(year.Groups[1].Value);
                }
                else if (dayOfWeekExclude.Success)
                {
                    DayOfWeek day = dayOfWeekExclude.Groups[1].Value switch
                    {
                        "mon" => DayOfWeek.Monday,
                        "tue" => DayOfWeek.Tuesday,
                        "wed" => DayOfWeek.Wednesday,
                        "thu" => DayOfWeek.Thursday,
                        "fri" => DayOfWeek.Friday,
                        "sat" => DayOfWeek.Saturday,
                        "sun" => DayOfWeek.Sunday,
                        _ => throw new InvalidDataException()
                    };

                    daysOfWeekExclude.Add(day);
                }
                else if (dateOfMonth.Success)
                {
                    int date = int.Parse(dateOfMonth.Groups[1].Value);
                    Season season = dateOfMonth.Groups[2].Value switch
                    {
                        "spring" => Season.Spring,
                        "summer" => Season.Summer,
                        "fall" => Season.Fall,
                        "winter" => Season.Winter,
                        _ => throw new NotImplementedException()
                    };
                    dates.Add(new WorldDate(1, season, date));
                }
                else if (dateEveryMonth.Success)
                {
                    int date = int.Parse(dateOfMonth.Groups[1].Value);
                    dates.Add(new WorldDate(1, Season.Spring, date));
                    dates.Add(new WorldDate(1, Season.Summer, date));
                    dates.Add(new WorldDate(1, Season.Fall, date));
                    dates.Add(new WorldDate(1, Season.Winter, date));
                }
            }

            if (startingyear > Game1.Date.Year || daysOfWeekExclude.Contains(Game1.Date.DayOfWeek))
                continue;

            if (activity.Schedule == string.Empty
                || dates.Any(d => Game1.Date.DayOfMonth == d.DayOfMonth && Game1.Date.Season == d.Season))
            {
                list.Add(activity);
            }
        }

        return list;
    }
}
