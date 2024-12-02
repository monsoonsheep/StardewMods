#nullable disable
namespace StardewMods.VisitorsMod.Framework.Models.Activities;

public class ActivityModel
{
    public string Id { get; set; }

    public string Location { get; set; }

    public string Schedule { get; set; } = string.Empty;

    public int[] TimeRange { get; set; } = [600, 2200];

    public string Animation { get; set; } = string.Empty;

    public bool ForRandomVisitors { get; set; } = false;

    public string Duration { get; set; } = "Short";

    public List<string> ArriveBy { get; set; } = ["WarpIntoLocation"];

    public List<ActivityActorModel> Actors { get; set; }
}

