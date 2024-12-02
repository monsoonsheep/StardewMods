namespace StardewMods.BusSchedules.Framework.Services;
internal class AssetHandler : Service
{
    public AssetHandler(
        IModEvents events,
        ILogger logger,
        IManifest manifest)
        : base(logger, manifest)
    {
        events.Content.AssetRequested += this.OnAssetRequested;
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {

    }
}
