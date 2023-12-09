using StardewCafe.Framework.Objects;
using StardewModdingAPI;
using StardewValley.Buffs;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisitorFramework.Framework.Visitors;
using VisitorFramework.Models;

namespace StardewCafe.Framework
{
    internal static class CustomerManager
    {
        internal static List<Visitor> CurrentVisitors = new List<Visitor>();
        internal static List<NPC> CurrentNpcVisitors = new List<NPC>();
        internal static Dictionary<string, ScheduleData> NpcVisitorSchedules = new Dictionary<string, ScheduleData>();
        
        /// <summary>
        /// Try to visit a regular NPC
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static bool TryVisitNpcVisitor(string name)
        {
            NPC npc = Game1.getCharacterFromName(name);

            // Skip if this one can't visit right now (for various reasons)
            if (!CanNpcVisitDuringTime(npc, Game1.timeOfDay))
                return false;
            
            // TODO: Grab partners
            List<NPC> npcsToVisit = new List<NPC>() { npc };
            List<List<Item>> npcsToVisitOrderItems = npcsToVisit.Select(GetLikedItemsFromMenu).ToList();

            // Remove the partners who don't have any liked (or at least neutral) items in the menu
            for (int i = npcsToVisit.Count - 1; i >= 0; i--)
            {
                if (npcsToVisitOrderItems[i].Count == 0)
                {
                    npcsToVisit.RemoveAt(i);
                    npcsToVisitOrderItems.RemoveAt(i);
                }
            }

            // If all are removed or if the starter isn't there, then move on
            if (npcsToVisit.Count == 0 || !npcsToVisit.Contains(npc))
                return false;

            Table table = CafeManager.GetFreeTables().OrderBy(t => t.Seats.Count).First(t => t.Seats.Count >= npcsToVisit.Count);

            List<Visitor> Visitors = new List<Visitor>();

            // Make a Visitor from each NPC
            bool failed = false;
            for (var i = 0; i < npcsToVisit.Count; i++)
            {
                var member = npcsToVisit[i];

                CurrentNpcVisitors.Add(member);
                Visitor Visitor = CreateVisitorFromNpc(member);
                if (Visitor == null)
                {
                    failed = true;
                    break;
                }

                Visitors.Add(Visitor);
                CurrentVisitors.Add(Visitor);
            }

            // If any of the NPCs failed to convert to Visitors, revert them all back
            if (failed)
            {
                foreach (var visitor in Visitors)
                {
                    RevertNpcVisitorToOriginal(visitor);
                    CurrentVisitors.Remove(visitor);
                }

                foreach (var member in npcsToVisit)
                {
                    if (member.currentLocation == null)
                        CurrentNpcVisitors.Remove(member);
                }

                Logger.Log("Failed to convert NPCs to Visitors", LogLevel.Warn);
                return false;
            }

            VisitorGroup group = new VisitorGroup(Visitors);
            CurrentGroups.Add(group);

            // Make them all go to the cafe
            foreach (var Visitor in Visitors)
            {
                Logger.LogWithHudMessage($"{Visitor.Name} is visiting the cafe");
            }

            // If any of them fail to go to the cafe (due to pathfinding problems), revert them all back
            if (Visitors.Any(c => c.controller == null))
            {
                Logger.Log($"NPC Visitor(s) ({string.Join(", ", Visitors.Where(c => c.controller == null))}) couldn't find path, converting back.",
                    LogLevel.Warn);
                Visitors.ForEach(RevertNpcVisitorToOriginal);
            }

            return true;
        }

        /// <summary>
        /// Determine if the given NPC is able to go to the cafe today at the given time. If they're sleeping, or their <see cref="NPC.ScheduleKey"/> isn't found,
        /// or they're marked already visited today, then we check their busy schedule from <see cref="ScheduleData"/>
        /// </summary>
        /// <param name="npc">The NPC to evaluate</param>
        /// <param name="timeOfDay">The current time of day</param>
        /// <returns></returns>
        internal static bool CanNpcVisitDuringTime(NPC npc, int timeOfDay)
        {
            ScheduleData visitData = NpcVisitorSchedules[npc.Name];

            // NPC can't visit cafe if:
            if (CurrentNpcVisitors.Contains(npc) // The NPC is already a Visitor right now
                || npc.isSleeping.Value // They're sleeping
                || npc.ScheduleKey == null // Their schedule today isn't selected
                || !visitData.CanVisitToday // If we've marked them as can't visit for today
                || visitData.LastVisitedDate == Game1.Date) // If they've visited today
                return false;


            visitData.BusyTimes.TryGetValue(npc.ScheduleKey, out var busyPeriods);
            if (busyPeriods == null)
                return true; // If no busy period for today, they're free all day

            // Check their busy periods for their current schedule key
            foreach (BusyPeriod busyPeriod in busyPeriods)
            {
                if (SUtility.CalculateMinutesBetweenTimes(timeOfDay, busyPeriod.From) <= 120
                    && SUtility.CalculateMinutesBetweenTimes(timeOfDay, busyPeriod.To) > 0)
                {
                    if (!(busyPeriod.Priority <= 3 && Game1.random.Next(6 * busyPeriod.Priority) == 0) &&
                        !(busyPeriod.Priority == 4 && Game1.random.Next(50) == 0))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Get a number from 0 to 1 as a chance for Visitors to visit, based on various factors
        /// </summary>
        /// <returns></returns>
        internal static float GetChanceToSpawnCustomer()
        {
            var tables = CafeManager.GetFreeTables();
            int minutesTillCloses = SUtility.CalculateMinutesBetweenTimes(Game1.timeOfDay, CafeManager.ClosingTime);
            int minutesTillOpens = SUtility.CalculateMinutesBetweenTimes(Game1.timeOfDay, CafeManager.OpeningTime);
            int minutesSinceLastVisitors = SUtility.CalculateMinutesBetweenTimes(CafeManager.LastTimeCustomersArrived, Game1.timeOfDay);
            float percentageOfTablesFree = (float)tables.Count / CafeManager.Tables.Count;

            // Can't spawn Visitors if:
            if (minutesTillOpens > 0 // Hasn't opened yet
                || minutesTillCloses <= 30 // It's less than 30 minutes to closing time
                || tables.Count == 0 // There are no free tables
                || !CafeManager.GetMenuItems().Any()) // There are no items in the cafe menu
                return 0f;

            float prob = 0f;

            // more chance if it's been a while since last Visitors
            prob += minutesSinceLastVisitors switch
            {
                <= 20 => 0f,
                <= 30 => Game1.random.Next(5) == 0 ? 0.05f : -0.1f,
                <= 60 => Game1.random.Next(2) == 0 ? 0.1f : 0f,
                _ => 0.25f
            };

            // more chance if a higher percent of tables are free
            prob += (percentageOfTablesFree) switch
            {
                <= 0.2f => 0.0f,
                <= 0.5f => 0.1f,
                <= 0.8f => 0.15f,
                _ => 0.2f
            };


            // slight chance to spawn if last hour of open time
            if (minutesTillCloses <= 60)
                prob += (Game1.random.Next(20 + (minutesTillCloses / 3)) >= 28) ? 0.2f : -0.5f;

            return prob;
        }

        /// <summary>
        /// Ran every 10 minutes. Use probability logic to create custom Visitors
        /// </summary>
        /// <returns></returns>
        internal static bool TrySpawnRoadVisitors()
        {
            // proc the chance to spawn Custom Visitors
            if (Game1.random.NextDouble() < GetChanceToSpawnCustomer())
            {
                Game1.delayedActions.Add(new DelayedAction(Game1.random.Next(400, 4500), SpawnRoadVisitors));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Spawn a group of Custom Visitors from the bus
        /// </summary>
        internal static void SpawnRoadVisitors()
        {
            VisitorGroup group = CreateVisitorGroup(GetLocationFromName("BusStop"), BusManager.BusDoorPosition);

            if (group == null)
                return;

            var memberCount = group.Members.Count;
            List<Point> convenePoints = BusManager.GetBusConvenePoints(memberCount);

            for (var i = 0; i < memberCount; i++)
            {
                group.Members[i].SetConvenePoint(BusManager.BusLocation, convenePoints[i]);
                group.Members[i].State.Set(VisitorState.MovingToConvene);
            }

            Logger.LogWithHudMessage($"{memberCount} Visitor(s) arriving");
        }

        
        /// <summary>
        /// The Visitor fires an event when they get up from their table, so we can save their <see cref="ScheduleData.LastVisitedDate"/>
        /// </summary>
        /// <param name="Visitor">The NPC Visitor to evaluate</param>
        internal static void OnVisitorDined(Visitor visitor)
        {
            if (CurrentNpcVisitors.Any(n => visitor.Name == n.Name))
            {
                NpcVisitorSchedules[visitor.Name].LastVisitedDate = Game1.Date;
            }
        }

        /// <summary>
        /// Check the items in <see cref="MenuItems"/> and return a list of those that are Loved, Liked, or Neutral
        /// </summary>
        /// <param name="npc">The NPC to evaluate</param>
        /// <returns></returns>
        internal static List<Item> GetLikedItemsFromMenu(NPC npc)
        {
            var menuItems = CafeManager.GetMenuItems();
            //return menuItems.Where(item => npc.getGiftTasteForThisItem(item) is 0 or 2 or 8).ToList();
            return menuItems.Where(item => item != null).ToList();
        }

        /// <summary>
        /// Go through <see cref="NpcVisitorSchedules"/> and update their <see cref="ScheduleData.CanVisitToday"/> to
        /// store whether or not that NPC can visit the cafe today
        /// </summary>
        internal static void UpdateNpcSchedules()
        {
            // Set which NPCs can visit today based on how many days it's been since their last visit, and their 
            // visit frequency level given in their visit data.
            foreach (var npcDataPair in NpcVisitorSchedules)
            {
                int daysSinceLastVisit = Game1.Date.TotalDays - npcDataPair.Value.LastVisitedDate.TotalDays;
                int daysAllowedBetweenVisits = npcDataPair.Value.Frequency switch
                {
                    0 => 200,
                    1 => 28,
                    2 => 15,
                    3 => 8,
                    4 => 2,
                    5 => 0,
                    _ => 9999999
                };

                npcDataPair.Value.CanVisitToday = daysSinceLastVisit > daysAllowedBetweenVisits;
            }
        }

    }
}
