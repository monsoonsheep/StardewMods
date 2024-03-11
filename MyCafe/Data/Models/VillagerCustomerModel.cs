using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCafe.Data.Models;
public class VillagerCustomerModel
{
    public string NpcName { get; set; } = null!;

    public Dictionary<string, List<BusyPeriod>> BusyTimes = null!;

    public int VisitFrequency = 2;
}
