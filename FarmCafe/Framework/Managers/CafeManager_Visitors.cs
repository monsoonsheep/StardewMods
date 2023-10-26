using FarmCafe.Framework.Characters;
using FarmCafe.Framework.Objects;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using FarmCafe.Framework.Multiplayer;
using Microsoft.Xna.Framework;
using static FarmCafe.Framework.Utility;
using FarmCafe.Models;
using SUtility = StardewValley.Utility;
using StardewValley.Buildings;
using System.Xml.Linq;
using StardewValley.Audio;

namespace FarmCafe.Framework.Managers
{
    internal partial class CafeManager
    {
        /// <summary>
        /// Get a number from 0 to 1 as a chance for Visitors to visit, based on various factors
        /// </summary>
        /// <returns></returns>
        internal float GetChanceToSpawnVisitors()
        {
            var tables = GetFreeTables();
            int minutesTillCloses = SUtility.CalculateMinutesBetweenTimes(Game1.timeOfDay, ClosingTime);
            int minutesTillOpens = SUtility.CalculateMinutesBetweenTimes(Game1.timeOfDay, OpeningTime);
            int minutesSinceLastVisitors = SUtility.CalculateMinutesBetweenTimes(LastTimeVisitorsArrived, Game1.timeOfDay);
            float percentageOfTablesFree = (float)tables.Count / Tables.Count;

            // Can't spawn Visitors if:
            if (minutesTillOpens > 0 // Hasn't opened yet
                || minutesTillCloses <= 30 // It's less than 30 minutes to closing time
                || tables.Count == 0 // There are no free tables
                || !GetMenuItems().Any()) // There are no items in the cafe menu
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
        /// Ran every 10 minutes. Use probability logic to create custom Visitors or NPC Visitors 
        /// </summary>
        /// <returns></returns>
        internal bool TrySpawnVisitors()
        {
            float prob = GetChanceToSpawnVisitors();
            bool success = false;

            // proc the chance to spawn Custom Visitors
            if (Game1.random.NextDouble() < prob)
            {
                Game1.delayedActions.Add(new DelayedAction(Game1.random.Next(400, 4500), TryVisitVisitors));
                success = true;
            }

            // proc the chance again to spawn NPC Visitors
            if (Game1.random.NextDouble() < prob && GetFreeTables().Count > 0)
            {
                Game1.delayedActions.Add(new DelayedAction(Game1.random.Next(400, 4500), TryVisitNpcVisitors));
                success = true;
            }

            return success;
        }

        /// <summary>
        /// Iterate through <see cref="NpcVisitorSchedules"/> randomly and select an NPC to convert to Visitor and send to the cafe.
        /// </summary>
        /// <param name="timeOfDay">The curernt time of day</param>
        /// <returns></returns>
        internal void TryVisitNpcVisitors()
        {
            foreach (string name in NpcVisitorSchedules.Keys.OrderBy(_ => Game1.random.Next()))
            {
                if (TryVisitNpcVisitor(name))
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Try to visit a regular NPC
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal bool TryVisitNpcVisitor(string name)
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

            Table table = GetFreeTables().OrderBy(t => t.Seats.Count).First(t => t.Seats.Count >= npcsToVisit.Count);

            List<Visitor> Visitors = new List<Visitor>();

            // Make a Visitor from each NPC
            bool failed = false;
            for (var i = 0; i < npcsToVisit.Count; i++)
            {
                var member = npcsToVisit[i];

                CurrentNpcVisitors.Add(member.Name);
                Visitor Visitor = CreateVisitorFromNpc(member);
                if (Visitor == null)
                {
                    failed = true;
                    break;
                }

                Visitors.Add(Visitor);
                CurrentVisitors.Add(Visitor);

                Visitor.OrderItem = npcsToVisitOrderItems[i][Game1.random.Next(npcsToVisitOrderItems[i].Count)];
            }

            // If any of the NPCs failed to convert to Visitors, revert them all back
            if (failed)
            {
                foreach (var Visitor in Visitors)
                {
                    Visitor.RevertOriginalNpc();
                    CurrentVisitors.Remove(Visitor);
                }

                foreach (var member in npcsToVisit)
                {
                    if (member.currentLocation == null)
                        CurrentNpcVisitors.Remove(member.Name);
                }

                Logger.Log("Failed to convert NPCs to Visitors", LogLevel.Warn);
                return false;
            }

            VisitorGroup group = new VisitorGroup(Visitors, table);
            CurrentGroups.Add(group);

            // Make them all go to the cafe
            foreach (var Visitor in Visitors)
            {
                Logger.LogWithHudMessage($"{Visitor.Name} is visiting the cafe");
                Visitor.GoToSeat();
            }

            // If any of them fail to go to the cafe (due to pathfinding problems), revert them all back
            if (Visitors.Any(c => c.controller == null))
            {
                Logger.Log($"NPC Visitor(s) ({string.Join(", ", Visitors.Where(c => c.controller == null))}) couldn't find path, converting back.",
                    LogLevel.Warn);
                Visitors.ForEach(c => c.RevertAndReturnOriginalNpc());
            }

            return true;
        }

        /// <summary>
        /// Spawn a group of Custom Visitors from the bus
        /// </summary>
        internal void TryVisitVisitors()
        {
            VisitorGroup group = CreateVisitorGroup(GetLocationFromName("BusStop"), BusPosition);

            if (group == null)
                return;

            var memberCount = group.Members.Count;
            List<Point> convenePoints = GetBusConvenePoints(memberCount);

            for (var i = 0; i < memberCount; i++)
            {
                group.Members[i].SetBusConvene(convenePoints[i], i * 800 + 1);
                group.Members[i].faceDirection(2);
                group.Members[i].State.Set(VisitorState.ExitingBus);
            }

            Logger.LogWithHudMessage($"{memberCount} Visitor(s) arriving");
        }

        /// <summary>
        /// Determine if the given NPC is able to go to the cafe today at the given time. If they're sleeping, or their <see cref="NPC.ScheduleKey"/> isn't found,
        /// or they're marked already visited today, then we check their busy schedule from <see cref="ScheduleData"/>
        /// </summary>
        /// <param name="npc">The NPC to evaluate</param>
        /// <param name="timeOfDay">The current time of day</param>
        /// <returns></returns>
        internal bool CanNpcVisitDuringTime(NPC npc, int timeOfDay)
        {
            ScheduleData visitData = NpcVisitorSchedules[npc.Name];

            // NPC can't visit cafe if:
            if (CurrentNpcVisitors.Contains(npc.Name) // The NPC is already a Visitor right now
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
        /// Instantiate a Visitor from an NPC by calling its secondary constructor
        /// </summary>
        /// <param name="npc">The NPC to convert</param>
        /// <returns></returns>
        internal Visitor CreateVisitorFromNpc(NPC npc)
        {
            Visitor c;
            try
            {
                c = new Visitor(npc);
            }
            catch
            {
                return null;
            }

            c.OnFinishedDined += OnVisitorDined;
            return c;
        }

        /// <summary>
        /// The Visitor fires an event when they get up from their table, so we can save their <see cref="ScheduleData.LastVisitedDate"/>
        /// </summary>
        /// <param name="Visitor">The NPC Visitor to evaluate</param>
        internal void OnVisitorDined(Visitor Visitor)
        {
            if (Visitor.OriginalNpc != null)
            {
                NpcVisitorSchedules[Visitor.Name].LastVisitedDate = Game1.Date;
            }
        }

        /// <summary>
        /// Check the items in <see cref="MenuItems"/> and return a list of those that are Loved, Liked, or Neutral
        /// </summary>
        /// <param name="npc">The NPC to evaluate</param>
        /// <returns></returns>
        internal List<Item> GetLikedItemsFromMenu(NPC npc)
        {
            var menuItems = GetMenuItems();
            //return menuItems.Where(item => npc.getGiftTasteForThisItem(item) is 0 or 2 or 8).ToList();
            return menuItems.Where(item => item != null).ToList();
        }

        /// <summary>
        /// Create <see cref="Visitor"/>s based on a random free table found, then put them in a <see cref="VisitorGroup"/> and adds them to the given location at the given position
        /// </summary>
        internal VisitorGroup CreateVisitorGroup(GameLocation location, Point tilePosition, int memberCount = 0)
        {
            var tables = GetFreeTables(memberCount);
            if (tables.Count == 0)
            {
                Logger.LogWithHudMessage("No tables to spawn Visitors");
                return null;
            }

            Table table;
            if (memberCount == 0)
            {
                // If memberCount not specified, calculate how many members it should be based on
                // the number of chairs on the table
                table = tables.First();
                int countSeats = table.Seats.Count;
                memberCount = countSeats switch
                {
                    1 => 1,
                    2 => Game1.random.Next(2) == 0 ? 2 : 1,
                    <= 4 => Game1.random.Next(countSeats) == 0 ? 2 : Game1.random.Next(3, countSeats + 1),
                    _ => Game1.random.Next(3, countSeats + 1)
                };
            }
            else
            {
                // If memberCount is specified, get the table that has 
                table = tables.OrderBy(t => t.Seats.Count).First();
            }

            // If not enough models free, adjust member count
            List<VisitorModel> modelsToUse = GetRandomVisitorModels(memberCount);
            //if (modelsToUse.Count < memberCount)
            //    memberCount = modelsToUse.Count;
            //if (memberCount == 0)
            //{
            //    Logger.Log("No more models to use for Visitors");
            //    return null;
            //}

            List<Visitor> Visitors = new List<Visitor>();
            for (var i = 0; i < memberCount; i++)
            {
                Visitor c = SpawnVisitor(location, tilePosition, modelsToUse[0]);
                Visitors.Add(c);
                c.OrderItem = GetRandomItemFromMenu();
            }

            VisitorGroup group = new VisitorGroup(Visitors, table);
            CurrentGroups.Add(group);


            //Sync.AddVisitorGroup(group);
            return group;
        }

        /// <summary>
        /// Return a list of positions that a group can convene on. After Visitors depart from the bus, they have to stand in a small area and look around for a few seconds. Those positions are based on the bus position.
        /// </summary>
        internal List<Point> GetBusConvenePoints(int count)
        {
            var startingPoint = BusPosition + new Point(0, 3);
            var points = new List<Point>();

            for (var i = 1; i <= 4; i++)
            {
                points.Add(new Point(startingPoint.X, startingPoint.Y + i));
                points.Add(new Point(startingPoint.X + 1, startingPoint.Y + i));
            }

            return points.OrderBy(x => Game1.random.Next()).Take(count).OrderBy(p => -p.Y).ToList();
        }

        /// <summary>
        /// Return a list of usable <see cref="VisitorModel"/>s that are used for creating Visitors. This also registers them as used and adds them to <see cref="VisitorModelsInUse"/>
        /// </summary>
        internal List<VisitorModel> GetRandomVisitorModels(int count = 0)
        {
            List<VisitorModel> results = new List<VisitorModel>();
            if (count == 0)
                count = VisitorModels.Count;
            foreach (var model in VisitorModels)
            {
                if (VisitorModelsInUse.Contains(model.Name))
                    continue;
                if (count == 0)
                    break;

                count--;
                //VisitorModelsInUse.Add(model.Name);
                results.Add(model);
            }

            return results;
        }

        /// <summary>
        /// Calls the <see cref="Visitor"/> contructor and creates an instance
        /// </summary>
        internal Visitor SpawnVisitor(GameLocation location, Point tilePosition, VisitorModel model)
        {
            Visitor Visitor = new Visitor(
                name: $"VisitorNPC_{model.Name}{CurrentVisitors.Count + 1}",
                targetTile: tilePosition,
                location: location, 
                sprite: new AnimatedSprite(model.TilesheetPath, 0, 16, 32),
                portrait: Game1.content.Load<Texture2D>(model.Portrait));
            Logger.Log($"Visitor {Visitor.Name} spawned");

            CurrentVisitors.Add(Visitor);
            return Visitor;
        }

        /// <summary>
        /// After a <see cref="VisitorGroup"/> is done visiting and all members are gone, we call this to remove the group and all members from tracking.
        /// </summary>
        public void DeleteGroup(VisitorGroup group)
        {
            foreach (Visitor c in group.Members)
            {
                CurrentVisitors.Remove(c);
                CurrentNpcVisitors.Remove(c.Name);
            }           
            CurrentGroups.Remove(group);
        }

        /// <summary>
        /// When a Visitor warps, it may end up overlapping with another Visitor so we make one of them charging
        /// </summary>
        internal void HandleWarp(Visitor Visitor, GameLocation location, Vector2 position)
        {
            foreach (var other in CurrentVisitors)
            {
                if (other.Equals(Visitor)
                    || !other.currentLocation.Equals(Visitor.currentLocation)
                    || !other.Tile.Equals(Visitor.Tile))
                    continue;

                other.isCharging = true;
                Logger.Log("Warping group, charging");
            }
        }

        /// <summary>
        /// Warp all members of a <see cref="VisitorGroup"/> to the given position and make them repath to their destination
        /// </summary>
        /// <param name="group"></param>
        /// <param name="location"></param>
        /// <param name="warpPosition"></param>
        internal void WarpGroup(VisitorGroup group, GameLocation location, Point warpPosition)
        {
            var points = AdjacentTiles(warpPosition).ToList();
            if (points.Count < group.Members.Count)
                return;
            for (var i = 0; i < group.Members.Count; i++)
            {
                Game1.warpCharacter(group.Members[i], location, points[i].ToVector2());
                group.Members[i].StartConvening();
            }
        }

        /// <summary>
        /// Clear all tracked Visitors and free all tables
        /// </summary>
        public void RemoveAllVisitors()
        {
            Logger.Log("Removing Visitors");
            foreach (var c in CurrentVisitors)
            {
                c.currentLocation?.characters?.Remove(c);
            }

            Sync.RemoveAllVisitors();
            CurrentVisitors.Clear();
            CurrentNpcVisitors.Clear();
            VisitorModelsInUse?.Clear();
            CurrentGroups?.Clear();
            FreeAllTables();
        }

        /// <summary>
        /// Locate every NPC instance in every location that is an instance of the <see cref="Visitor"/> class
        /// </summary>
        internal static List<Visitor> GetAllVisitorsInGame()
        {
            var locationVisitors = Game1.locations
                .SelectMany(l => l.characters)
                .OfType<Visitor>();

            
            var buildingVisitors = (Game1.getFarm().buildings
                    .Where(b => b.GetIndoors() != null)
                    .SelectMany(b => b.GetIndoors().characters))
                .OfType<Visitor>();

            var list = locationVisitors.Concat(buildingVisitors).ToList();

            return list;
        }

        /// <summary>
        /// Search <see cref="Visitor"/> objects in all locations and buildings and return one with the given name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static Visitor GetVisitorFromName(string name)
        {
            return GetAllVisitorsInGame().FirstOrDefault(c => c.name == name);
        }
    }
}
