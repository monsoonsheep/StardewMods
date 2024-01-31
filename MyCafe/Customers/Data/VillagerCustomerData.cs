﻿using StardewValley;
using System.Collections.Generic;

namespace MyCafe.Customers.Data;

public class VillagerCustomerData : CustomerData
{
    public Dictionary<string, List<BusyPeriod>> BusyTimes = null!;

    internal NPC RealNpc = null!;
}

public class BusyPeriod
{
    public int From = 600;
    public int To = 2600;
    public int Priority = 4;
}
