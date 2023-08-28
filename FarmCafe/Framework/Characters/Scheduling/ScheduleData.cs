using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarmCafe.Framework.Characters.Scheduling
{
    public class ScheduleData
    {
        public Dictionary<string, List<BusyPeriod>> BusyTimes;

        public ScheduleData()
        {

        }
    }

    public class BusyPeriod
    {
        public int From;
        public int To;
        public int Priority = 4;
    }

}
