using StardewValley;
using System.Collections.Generic;

namespace MyCafe.Customers;

public class VillagerCustomerData : CustomerData
{
    public Dictionary<string, List<BusyPeriod>> BusyTimes;

    internal NPC RealNpc;
}

public class BusyPeriod
{
    public int From = 600;
    public int To = 2600;
    public int Priority = 4;
}
