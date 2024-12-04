namespace StardewMods.BusSchedules.Framework.Services;
public class ModEvents 
{
    public delegate void BusArriveHandler(object? o, BusArriveEventArgs e);
    public event BusArriveHandler? BusArrive = null;

    internal void Invoke_BusArrive()
    {
        this.BusArrive?.Invoke(null, new BusArriveEventArgs(Game1.timeOfDay));
    }
}
