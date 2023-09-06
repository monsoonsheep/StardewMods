using System.Collections.Generic;
using Newtonsoft.Json;

namespace FarmCafe.Models
{
    public class ScheduleData
    {
        public List<string> Partners; // will be changed to something more sophisticated soon
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
