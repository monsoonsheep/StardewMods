namespace StardewMods.BusSchedules.Framework.Services;
public class ModEvents : Service
{
    public delegate void BusArriveHandler(object? o, BusArriveEventArgs e);
    public event BusArriveHandler? BusArrive = null;

    public ModEvents(
        ILogger logger,
        IManifest manifest)
        : base(logger, manifest)
    {
    }

    internal void Invoke_BusArrive()
    {
        this.BusArrive?.Invoke(null, new BusArriveEventArgs(Game1.timeOfDay));
    }
}

public class BusArriveEventArgs : EventArgs
{
    public int Time;

    public BusArriveEventArgs(int time)
    {
        this.Time = time;
    }
}

