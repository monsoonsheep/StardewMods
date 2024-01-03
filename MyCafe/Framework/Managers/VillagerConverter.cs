using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MyCafe.Framework.Customers;
using StardewValley.Pathfinding;
using StardewValley;
using SUtility = StardewValley.Utility;

namespace MyCafe.Framework.Managers;

internal class VillagerConverter
{
    internal static Customer ConvertVillagerToCustomer(NPC npc)
    {
        Customer customer = new Customer(npc.Name, npc.Position, npc.DefaultMap, npc.Sprite, npc.Portrait);
        npc.currentLocation.characters.Remove(npc);
        npc.currentLocation.characters.Add(customer);
        return customer;
    }

    internal static void RevertVillagerCustomerToOriginal(Customer v)
    {
        NPC original = v.OriginalNpc;
        if (original != null)
        {
            original.currentLocation = v.currentLocation;
            original.Position = v.Position;
            original.TryLoadSchedule(v.ScheduleKey);
            original.faceDirection(v.FacingDirection);
            original.ignoreScheduleToday = false;

            var activityTimes = v.Schedule.Keys.OrderBy(i => i).ToList();
            int timeOfCurrent = activityTimes.LastOrDefault(t => t <= Game1.timeOfDay);
            int timeOfNext = activityTimes.FirstOrDefault(t => t > Game1.timeOfDay);
            var minutesSinceCurrentStarted = SUtility.CalculateMinutesBetweenTimes(timeOfCurrent, Game1.timeOfDay);
            var minutesTillNextStarts = SUtility.CalculateMinutesBetweenTimes(Game1.timeOfDay, timeOfNext);
            int timeOfActivity;
            if (timeOfCurrent == 0) // Means it's the start of the day
            {
                timeOfActivity = activityTimes.First();
            }
            else if (timeOfNext == 0) // Means it's the end of the day
            {
                timeOfActivity = activityTimes.Last();
            }
            else
            {
                if (minutesTillNextStarts < minutesSinceCurrentStarted && minutesTillNextStarts <= 30)
                    // If we're very close to the next item, 
                    timeOfActivity = timeOfNext;
                else
                    timeOfActivity = timeOfCurrent;

            }

            SchedulePathDescription originalPathDescription = original.Schedule[timeOfActivity];

            GameLocation targetLocation = Game1.getLocationFromName(originalPathDescription.targetLocationName);
            if (targetLocation != null)
            {
                Stack<Point> routeToScheduleItem =
                    Pathfinding.PathfindFromLocationToLocation(original.currentLocation, original.TilePoint,
                                                               targetLocation, originalPathDescription.targetTile,
                                                               original);

                SchedulePathDescription toInsert = new SchedulePathDescription(
                    routeToScheduleItem,
                    originalPathDescription.facingDirection,
                    originalPathDescription.endOfRouteBehavior,
                    originalPathDescription.endOfRouteMessage,
                    targetLocation.Name,
                    originalPathDescription.targetTile)
                {
                    time = Game1.timeOfDay
                };

                original.queuedSchedulePaths.Clear();
                original.Schedule[Game1.timeOfDay] = toInsert;
                original.checkSchedule(Game1.timeOfDay);
            }
        }

        v.currentLocation.characters.Remove(v);
        v.currentLocation.addCharacter(original);
    }
}
