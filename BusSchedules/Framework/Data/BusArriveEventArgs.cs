namespace StardewMods.BusSchedules.Framework.Services;

public class BusArriveEventArgs : EventArgs
{
    public int Time;

    public BusArriveEventArgs(int time)
    {
        this.Time = time;
    }
}

