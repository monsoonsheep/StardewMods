#region Usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using StardewValley.GameData.Characters;
using VisitorFramework.Models;
#endregion

namespace VisitorFramework.Framework.Visitors
{
    internal class VisitorData
    {
        public string VisitDays  = "Any";
        public bool AlwaysWithGroup;
        public List<string> Companions = new List<string>();
        public List<string> Activities = new List<string>();

        public WorldDate LastVisited;
        public Dictionary<string, int> ActivitiesEngagedIn;

        internal CharacterData GameData;

        internal bool AvailableToday = true;

        // REWORK
        internal Dictionary<string, (int, int)> ScheduleKeysForBusArrival = new Dictionary<string, (int, int)>();
    }
}
