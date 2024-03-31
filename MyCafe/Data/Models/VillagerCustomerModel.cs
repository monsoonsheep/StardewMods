using System.Collections.Generic;

namespace MyCafe.Data.Models;
public class VillagerCustomerModel
{
    public string NpcName { get; set; } = null!;

    public Dictionary<string, List<BusyPeriod>> BusyTimes = null!;

    public int VisitFrequency = 2;
}
