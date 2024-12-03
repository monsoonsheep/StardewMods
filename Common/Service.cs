namespace StardewMods.Common;
public abstract class Service
{
    public Service(ILogger logger, IManifest manifest)
    {
        this.Log = logger;
        this.ModManifest = manifest;
    }

    protected IManifest ModManifest { get; }
    protected ILogger Log { get; }
}
