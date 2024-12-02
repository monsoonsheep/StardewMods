namespace StardewMods.VisitorsMod.Framework.Services;

internal class ModEvents : Service
{
    
    public ModEvents(
        ILogger logger,
        IManifest manifest)
        : base(logger, manifest)
    {
    }
}
