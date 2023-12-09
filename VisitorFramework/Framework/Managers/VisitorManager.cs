#region Usings

using System;
using VisitorFramework.Framework.Visitors;
using VisitorFramework.Models;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework;
using StardewValley.Pathfinding;
using VisitorFramework.Framework.Visitors.Activities;
using SUtility = StardewValley.Utility;
using static VisitorFramework.Framework.Utility;
using StardewValley.Buildings;
#endregion

namespace VisitorFramework.Framework.Managers
{
    // Main tracking list class
    
    internal class VisitorManager
    {
        internal class GroupList
        {
            private readonly List<VisitorGroup> trackedObjects = new List<VisitorGroup>();

            internal List<VisitorGroup> Get()
            {
                return trackedObjects; 
            }

            internal void Add(VisitorGroup group)
            {
                group.Finished += OnGroupFinished;
                trackedObjects.Add(group);
            }

            private void OnGroupFinished(object sender, EventArgs e)
            {
                if (sender is VisitorGroup group)
                    Remove(group);
            }

            internal void Remove(VisitorGroup group)
            {
                foreach (var member in group.Members)
                {
                    member.currentLocation.characters.Remove(member);
                    VisitorsData[member.Name].AvailableToday = CanVisitToday(member.Name);
                }
                    
                // Free up the named activity that the group was using
                if (group.CurrentActivity.Name != null && ActivityManager.Activities.ContainsKey(group.CurrentActivity.Name))
                    ActivityManager.FreeActivity(group.CurrentActivity.Name);

                trackedObjects.Remove(group);
            }

            internal void Clear()
            {
                foreach (var group in trackedObjects)
                {
                    group.FinishJob();
                }
            }
        }

        internal static List<NPC> CurrentNpcVisitors = new List<NPC>();
        internal static GroupList CurrentGroups = new GroupList();
        internal static Dictionary<string, VisitorData> VisitorsData = new Dictionary<string, VisitorData>();

        internal static List<Point> ConveneCenters = new List<Point>();
        internal static List<Point> ConveneCentersInUse = new List<Point>();

        internal static void FreeConvenePoint(Point point)
        {
            ConveneCentersInUse.Remove(point);
        }

        internal static void DayUpdate()
        {
            foreach (var pair in VisitorsData)
            {
                NPC npc = Game1.getCharacterFromName(pair.Key);
                if (npc == null) continue;
                npc.DefaultMap = "BusStop";
                npc.DefaultPosition = new Vector2(BusManager.BusDoorPosition.X * 64, BusManager.BusDoorPosition.Y * 64);
                npc.TryLoadSchedule();
                npc.reloadSprite();
                npc.Position = new Vector2(-1000, -1000);
            }

            //foreach (var data in VisitorsData)
            //{
            //    data.Value.AvailableToday = CanVisitToday(data.Key);
            //}

            //ConveneCenters.Add(new Point(9, 14));
            //ConveneCenters.Add(new Point(11, 18));
            //ConveneCenters.Add(new Point(18, 13));
            //ConveneCenters.Add(new Point(18, 19));
        }

        internal static void SpawnVisitors()
        {
            foreach (var pair in VisitorsData)
            {
                NPC npc = Game1.getCharacterFromName(pair.Key);
                if (npc?.ScheduleKey == null || !pair.Value.ScheduleKeysForBusArrival.TryGetValue(npc.ScheduleKey, out var arrivalDepartureIndices))
                {
                    Log.Debug($"Visitor {pair.Key} hasn't spawned or schedule doesn't have a schedule for bus");
                    continue;
                }

                if (arrivalDepartureIndices.Item1 == BusManager.BusArrivalsToday - 1)
                {
                    npc.Position = new Vector2(BusManager.BusDoorPosition.X * 64, BusManager.BusDoorPosition.Y * 64);
                    npc.checkSchedule(BusManager.LastArrivalTime);
                }
            }

            // Get group of VisitorData
            //KeyValuePair<string, VisitorData> leader = LoadedVisitorData.Where(p => p.Value.AvailableToday).MinBy(_ => Game1.random.Next());
            //List<KeyValuePair<string, VisitorData>> membersData = new List<KeyValuePair<string, VisitorData>>() { leader };
            //membersData.AddRange(LoadedVisitorData.Where(other => IsCompanionOf(leader.Key, other.Key)));

            //// Activities
            //List<string> preferredActivities = membersData.SelectMany(m => m.Value.Activities).ToList();
            //string activityName = ActivityManager.ReserveActivity(membersData.Count, preferredActivities);
            //if (string.IsNullOrEmpty(activityName))
            //{
            //    Log.Debug("No activities found to spawn visitors");
            //    return;
            //}
            
            //// Spawn Visitors and update their data
            //List<Visitor> members = new List<Visitor>();
            //for (int i = 0; i < membersData.Count; i++)
            //{
            //    Visitor v = CreateVisitor(BusManager.BusLocation, BusManager.BusDoorPosition, membersData[i].Key, 800 * i);
            //    if (v == null)
            //    {
            //        Log.Error($"Failed to create visitor: {membersData[i].Key}");
            //        continue;
            //    }
            //    members.Add(v);
            //    membersData[i].Value.AvailableToday = false;
            //    membersData[i].Value.LastVisited = Game1.Date;
            //}

            //VisitorGroup group = new VisitorGroup(members);

            //VisitorActivity busStopConveneActivity = GetConveneAtBusStopActivity(group);

            //if (busStopConveneActivity != null)
            //    group.ActivityQueue.Enqueue(busStopConveneActivity);
            
            //group.ActivityQueue.Enqueue(ActivityManager.Activities[activityName]);

            //group.NextActivity();
            //CurrentGroups.Add(group);
        }

        public static void CharacterReachBusEndBehavior(Character c, GameLocation location)
        {
            if (c is NPC npc)
            {
                npc.Position = new Vector2(-1000, -1000);
                npc.controller = null;
                npc.followSchedule = false;
            }
        }

        internal static bool CanVisitToday(string visitorId)
        {
            VisitorData data = VisitorsData[visitorId];
            
            if (data.AlwaysWithGroup)
            {
                if (VisitorsData.Where(other => IsCompanionOf(visitorId, other.Key)).Any(other => !other.Value.AvailableToday))
                {
                    return false;
                }
            }

            // TODO: conditions and past visits take into account and decide if they should visit today
            return true;
        }

        internal static bool IsCompanionOf(string v1, string v2)
        {
            return v1 != v2 && (VisitorsData[v1].Companions.Contains(v2) || VisitorsData[v2].Companions.Contains(v1));
        }

        internal static VisitorActivity GetConveneAtBusStopActivity(VisitorGroup group)
        {
            VisitorActivity busActivity = new VisitorActivity();
            GameLocation busStop = BusManager.BusLocation;

            List<Point> convenePoints = GetConvenePoints(group.Members.Count);
            if (convenePoints == null)
                return null;

            foreach (Point point in convenePoints)
            {
                busActivity.Actions.Add(new VisitAction(busStop, point, -1));
            }

            return busActivity;
        }

        internal static List<Point> GetConvenePoints(int count)
        {
            var centers = ConveneCenters.Where(x => !ConveneCentersInUse.Contains(x));
            foreach (var center in centers.OrderBy(_ => Game1.random.Next()))
            {
                List<Point> points = AdjacentTilesCollision(center, BusManager.BusLocation, reach: 1);

                if (points.Count >= count)
                    return points.OrderBy(_ => Game1.random.Next()).Take(4).ToList();
            }

            return null;
        }

        #region NPC Visitors
        /// <summary>
        /// Instantiate a visitor from an NPC by calling its secondary constructor
        /// </summary>
        /// <param name="npc">The NPC to convert</param>
        /// <returns></returns>
        internal static Visitor CreateVisitorFromNpc(NPC npc)
        {
            // character = new NPC(new AnimatedSprite("Characters\\" + characterTextureName, 0, size.X, size.Y), new Vector2(homeTile.X * 64, homeTile.Y * 64), homeName, direction, characterId, datable, null, content.Load<Texture2D>("Portraits\\" + characterTextureName));
            Visitor v = new Visitor(npc.Name, npc.Position, npc.currentLocation.Name, npc.Sprite, npc.Portrait)
            {
                Breather = npc.Breather,
                CurrentDialogue = npc.CurrentDialogue,
                Position = npc.Position,
            };

            npc.currentLocation.characters.Remove(npc);

            v.TryLoadSchedule();
            v.reloadData();
            v.faceDirection(npc.FacingDirection);
            v.lastSeenMovieWeek.Set(npc.lastSeenMovieWeek.Value);

            return v;
        }

        /// <summary>
        /// Return the villager visitor to their original state, restoring their schedule, and remove the visitor
        /// </summary>
        /// <param name="v"></param>
        internal static void RevertNpcVisitorToOriginal(Visitor v)
        {
            NPC original = CurrentNpcVisitors.FirstOrDefault(n => n.Name == v.Name);
            if (original != null)
            {
                // Remove this visitor object from its location
                v.currentLocation.characters.Remove(v);

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
        /// Get the time of day for the action that's either right before or right after the current time, based on distance
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
                if (minutesTillNextStarts < minutesSinceCurrentStarted && minutesTillNextStarts <= 30)
                    // If we're very close to the next item, 
                    timeOfActivity = timeOfNext;
                else
                    timeOfActivity = timeOfCurrent;

            }

            return timeOfActivity;
        }
        #endregion

        /// <summary>
        /// Calls the <see cref="Visitor"/> contructor and creates an instance
        /// </summary>
        internal static Visitor CreateVisitor(GameLocation location, Point tilePosition, string visitorId, int movementPause)
        {
            if (!NPC.TryGetData(visitorId, out var npcData))
            {
                Log.Warn($"NPC Data not found for visitor: {visitorId}");
                return null;
            }

            var characterTextureName = NPC.getTextureNameForCharacter(visitorId);
            Visitor visitor = new Visitor(visitorId, new Vector2(tilePosition.X * 64, tilePosition.Y * 64), location.Name, new AnimatedSprite("Characters\\" + characterTextureName, 0, 16, 32), Game1.content.Load<Texture2D>("Portraits\\" + characterTextureName))
            {
                Breather = npcData.Breather,
                FacingDirection = 2,
                IsInvisible = true,
                movementPause = movementPause
            };

            location.addCharacter(visitor);

            Game1.delayedActions.Add(new DelayedAction(movementPause, () => visitor.IsInvisible = false));

            Log.Debug($"visitor {visitor.Name} spawned");
            return visitor;
        }

        /// <summary>
        /// When a visitor warps, it may end up overlapping with another visitor so we make one of them charging
        /// </summary>
        internal static void HandleWarp(Visitor visitor, GameLocation location, Vector2 position)
        {
            foreach (var other in CurrentGroups.Get().SelectMany(g => g.Members))
            {
                if (other.Equals(visitor)
                    || !other.currentLocation.Equals(visitor.currentLocation)
                    || !other.Tile.Equals(visitor.Tile))
                    continue;

                other.isCharging = true;
                Log.Debug("Warping group, charging");
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
            }
        }

        /// <summary>
        /// Clear all tracked Visitors and free all tables
        /// </summary>
        public static void RemoveAllVisitors()
        {
            CurrentGroups.Clear();
        }

        /// <summary>
        /// Locate every NPC instance in every location that is an instance of the <see cref="Visitor"/> class
        /// </summary>
        internal static List<Visitor> GetAllVisitorsInGame()
        {
            var locationVisitors = Game1.locations
                .SelectMany(l => l.characters)
                .OfType<Visitor>();


            var buildingVisitors = Game1.getFarm().buildings
                    .Where(b => b.GetIndoors() != null)
                    .SelectMany(b => b.GetIndoors().characters)
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

        /// <summary>
        /// Given a starting and an ending location, return a list of names of Game Locations as a route between start and end
        /// </summary>
        /// <returns></returns>
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
                    if (end is Farm)
                    {
                        return routeToBus.Concat(new[] { "Farm" }).ToArray();
                    }
                    else
                    {
                        var routeFromBusToEnd = GetLocationRoute(Game1.getLocationFromName("BusStop"), end);
                        if (routeFromBusToEnd != null)
                        {
                            return routeToBus.Take(routeToBus.Length - 1).Concat(routeFromBusToEnd).ToArray();
                        }
                    }
                }
            }

            return route;
        }
    }
}
