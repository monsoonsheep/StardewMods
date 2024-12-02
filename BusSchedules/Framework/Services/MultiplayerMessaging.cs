namespace StardewMods.BusSchedules.Framework.Services;

internal class MultiplayerMessaging : Service
{
    private readonly IMultiplayerHelper _multiplayerHelper;

    public MultiplayerMessaging(
        ILogger logger,
        IManifest manifest,
        IModEvents events,
        IMultiplayerHelper multiplayerHelper)
        : base(logger, manifest)
    {
        this._multiplayerHelper = multiplayerHelper;
    }

    internal void SendMessage(string message)
    {
        this._multiplayerHelper.SendMessage(message, "mod", null, null);
    }
}
