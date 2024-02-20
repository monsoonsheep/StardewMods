using System;

namespace BusSchedules;

public class BusEvents
{
    public static EventHandler? BusArrive;

    internal static void Invoke_BusArrive()
    {
        BusArrive?.Invoke(null, EventArgs.Empty);
    }
}
