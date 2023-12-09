using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewValley;
using VisitorFramework.Framework.Visitors.Activities;

namespace VisitorFramework.Framework.Managers
{
    internal class ActivityManager
    {
        internal static Dictionary<string, VisitorActivity> Activities = new Dictionary<string, VisitorActivity>();
        internal static List<string> ActivitiesInUse = new List<string>();

        internal static string ReserveActivity(int memberCount, List<string> preferredActivities)
        {
            var available = Activities
                .Where(x => x.Value.Actions.Count >= memberCount && !ActivitiesInUse.Contains(x.Key))
                .Select(pair => pair.Key)
                .OrderBy(_ => Game1.random.Next()).ToList();

            if (available.Count == 0)
            {
                return null;
            }

            string result = preferredActivities.FirstOrDefault(item => available.Contains(item));
            if (string.IsNullOrEmpty(result))
            {
                result = available.First();
            }

            ActivitiesInUse.Add(result);
            return result;
        }

        internal static void FreeActivity(string name)
        {
            ActivitiesInUse.Remove(name);
        }

        internal static VisitorActivity GetReturnHomeActivity(int count)
        {
            VisitorActivity activity = new VisitorActivity();
            List<VisitAction> actions = new List<VisitAction>();
            for (int i = 0; i < count; i++)
            {
                VisitAction a = new VisitAction(BusManager.BusLocation, BusManager.BusDoorPosition, 0);
                actions.Add(a);
            }

            activity.Actions = actions;
            return activity;
        }
    }
}
