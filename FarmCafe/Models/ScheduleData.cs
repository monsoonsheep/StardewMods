using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FarmCafe.Models
{
    public class ScheduleData
    {
        public Dictionary<string, List<BusyPeriod>> BusyTimes;

        [JsonIgnore]
        internal (int, string, int) LastVisitedDate = new(1, "spring", 1);

        public ScheduleData()
        {
        }
    }

    public class BusyPeriod
    {
        public int From = 600;
        public int To = 2600;
        public int Priority = 4;
    }

}
