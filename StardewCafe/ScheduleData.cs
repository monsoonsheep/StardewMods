using System.Collections.Generic;
using Newtonsoft.Json;
using StardewValley;

namespace VisitorFramework.Models
{
    public class ScheduleData
    {
        public int Frequency = 2;
        public List<string> Partners; // will be changed to something more sophisticated soon
        public Dictionary<string, List<BusyPeriod>> BusyTimes;

        internal WorldDate LastVisitedDate = new(1,Season.Spring, 1);
        internal bool CanVisitToday = false;

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
