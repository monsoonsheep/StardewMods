
namespace StardewMods.VisitorsMod.Framework.Services;
internal class SaveDataManager : Service
{
    internal Dictionary<string, WorldDate> ActivitiesHistory = [];

    public SaveDataManager(
        IModEvents events,
        ILogger logger,
        IManifest manifest)
        : base(logger, manifest)
    {

    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {

    }
}
