using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusSchedules;

public class Events
{
    public static EventHandler BusArrive;

    internal static void Invoke_BusArrive()
    {
        BusArrive.Invoke(null, EventArgs.Empty);
    }
}