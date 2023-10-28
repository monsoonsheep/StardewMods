using VisitorFramework.Framework.Characters;
using VisitorFramework.Framework.Multiplayer;
using VisitorFramework.Models;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewValley.Pathfinding;
using SUtility = StardewValley.Utility;
using static VisitorFramework.Framework.Utility;

namespace VisitorFramework.Framework.Managers
{
    internal class VisitorManager
    {
        internal static List<Visitor> CurrentVisitors = new List<Visitor>();
        internal static List<NPC> CurrentNpcVisitors = new List<NPC>();

        internal static List<VisitorModel> VisitorModels = new List<VisitorModel>();
        internal static List<string> VisitorModelsInUse = new List<string>();
        internal static List<VisitorGroup> CurrentGroups = new List<VisitorGroup>();

        internal static string[] GetLocationRoute(GameLocation start, GameLocation end)
        {
            string[] route = WarpPathfindingCache.GetLocationRoute(start.Name, end.Name, NPC.male);

            // TODO: Extra routes to farm and farm's buildings

            if (route == null)
            {
                // If an NPC wants to get out of the women's locker in the spa, this won't work. do something later.
                var routeToBus = WarpPathfindingCache.GetLocationRoute(start.NameOrUniqueName, "BusStop", NPC.male);
                if (routeToBus != null)
                {
                    var routeFromBusToEnd = GetLocationRoute(Game1.getLocationFromName("BusStop"), end);
                    if (routeFromBusToEnd != null)
                    {
                        return routeToBus.Take(routeToBus.Length - 1).Concat(routeFromBusToEnd).ToArray();
                    }
                }
            }
            // TODO reverse route (out of cafe)


            return route;
        }

        /// <summary>
        /// Spawn visitor groups at the bus door when the bus arrives
        /// </summary>
        /// <returns></returns>
        internal static bool TrySpawnBusVisitors()
        {
            GameLocation busStop = GetLocationFromName("BusStop");

            VisitorGroup group = CreateVisitorGroup(busStop, BusManager.BusDoorPosition);

            if (group == null)
                return false;

            var memberCount = group.Members.Count;
            List<Point> convenePoints = BusManager.GetBusConvenePoints(memberCount);
            for (var i = 0; i < memberCount; i++)
            {
                group.Members[i].SetConvenePoint(busStop, convenePoints[i]);
                group.Members[i].State.Set(VisitorState.MovingToConvene);
            }

            Logger.LogWithHudMessage($"{memberCount} Visitor(s) arriving");
            return true;
        }

        /// <summary>
        /// Instantiate a Visitor from an NPC by calling its secondary constructor
        /// </summary>
        /// <param name="npc">The NPC to convert</param>
        /// <returns></returns>
        internal static Visitor CreateVisitorFromNpc(NPC npc)
        {
            Visitor v;
            try
            {
                v = new Visitor(npc);
            }
            catch
            {
                return null;
            }

            v.OnLeaveFarm += RevertNpcVisitorToOriginal;
            return v;
        }

        /// <summary>
        /// Return the villager visitor to their original state, restoring their schedule, and remove the Visitor
        /// </summary>
        /// <param name="v"></param>
        internal static void RevertNpcVisitorToOriginal(Visitor v)
        {
            NPC original = CurrentNpcVisitors.FirstOrDefault(n => n.Name == v.Name);
            if (original != null)
            {
                // Remove this Visitor object from the game and mod
                v.currentLocation.characters.Remove(v);
                DeleteGroup(v.Group);

                original.currentLocation = v.currentLocation;
                original.Position = v.Position;
                original.TryLoadSchedule(v.ScheduleKey);
                original.faceDirection(v.FacingDirection);
                original.ignoreScheduleToday = false;

                // Add the original back to the game
                v.currentLocation.addCharacter(original);

                SchedulePathDescription originalPathDescription = original.Schedule[GetTimeOfActivityAfterReverting(v)];

                GameLocation targetLocation = Game1.getLocationFromName(originalPathDescription.targetLocationName);
                if (targetLocation != null)
                {
                    Stack<Point> routeToScheduleItem =
                        original.PathTo(original.currentLocation, original.TilePoint, targetLocation, originalPathDescription.targetTile);

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
        }

        /// <summary>
        /// Get the time of day for the activity that's either right before or right after the current time, based on distance
        /// </summary>
        /// <returns></returns>
        internal static int GetTimeOfActivityAfterReverting(Visitor v)
        {
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
                if ((minutesTillNextStarts < minutesSinceCurrentStarted) && minutesTillNextStarts <= 30)
                    // If we're very close to the next item, 
                    timeOfActivity = timeOfNext;
                else
                    timeOfActivity = timeOfCurrent;

            }

            return timeOfActivity;
        }

        /// <summary>
        /// Create <see cref="Visitor"/>s based on a random free table found, then put them in a <see cref="VisitorGroup"/> and adds them to the given location at the given position
        /// </summary>
        internal static VisitorGroup CreateVisitorGroup(GameLocation location, Point tilePosition, int memberCount = 0)
        {
            List<VisitorModel> models = GetRandomVisitorModels(memberCount);
            if (models.Count == 0)
            {
                Logger.LogWithHudMessage("No models for Visitors");
                return null;
            }

            List<Visitor> visitors = new List<Visitor>(memberCount);

            for (var i = 0; i < memberCount; i++)
            {
                Visitor v = SpawnVisitor(location, tilePosition, models[0]);
                visitors[i] = v;
            }

            VisitorGroup group = new VisitorGroup(visitors);
            CurrentGroups.Add(group);
            return group;
        }

        /// <summary>
        /// Return a list of usable <see cref="VisitorModel"/>s that are used for creating Visitors. This also registers them as used and adds them to <see cref="VisitorModelsInUse"/>
        /// </summary>
        internal static List<VisitorModel> GetRandomVisitorModels(int count = 0)
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
        internal static Visitor SpawnVisitor(GameLocation location, Point tilePosition, VisitorModel model)
        {
            Visitor visitor = new Visitor(
                name: $"VisitorNPC_{model.Name}{CurrentVisitors.Count + 1}",
                targetTile: tilePosition,
                location: location,
                sprite: new AnimatedSprite(model.TilesheetPath, 0, 16, 32),
                portrait: Game1.content.Load<Texture2D>(model.Portrait));
            Logger.Log($"Visitor {visitor.Name} spawned");

            CurrentVisitors.Add(visitor);
            return visitor;
        }

        /// <summary>
        /// After a <see cref="VisitorGroup"/> is done visiting and all members are gone, we call this to remove the group and all members from tracking.
        /// </summary>
        public static void DeleteGroup(VisitorGroup group)
        {
            foreach (Visitor c in group.Members)
            {
                CurrentVisitors.Remove(c);
                CurrentNpcVisitors.RemoveAll(n => c.Name == n.Name);
            }
            CurrentGroups.Remove(group);
        }

        /// <summary>
        /// When a Visitor warps, it may end up overlapping with another Visitor so we make one of them charging
        /// </summary>
        internal static void HandleWarp(Visitor Visitor, GameLocation location, Vector2 position)
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
        internal static void WarpGroup(VisitorGroup group, GameLocation location, Point warpPosition)
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
        public static void RemoveAllVisitors()
        {
            Logger.Log("Removing Visitors");
            foreach (var c in CurrentVisitors)
            {
                c.currentLocation?.characters?.Remove(c);
            }

            Sync.RemoveAllVisitors();
            CurrentVisitors.Clear();
            VisitorModelsInUse?.Clear();
            CurrentGroups?.Clear();
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
            return GetAllVisitorsInGame().FirstOrDefault(c => c.name.Value == name);
        }
    }
}
