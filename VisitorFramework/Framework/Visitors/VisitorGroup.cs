#region Usings
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using VisitorFramework.Framework.Managers;
using VisitorFramework.Framework.Visitors.Activities;
using static VisitorFramework.Framework.Utility;

#endregion

namespace VisitorFramework.Framework.Visitors
{
    internal class VisitorGroup
	{
		internal List<Visitor> Members;
        internal VisitorActivity CurrentActivity;
        internal Queue<VisitorActivity> ActivityQueue = new Queue<VisitorActivity>();
        internal bool ReturningHome;

        internal EventHandler Finished;

        internal void FinishJob()
        {
            OnFinished();
        }

        private void OnFinished()
        {
            Finished?.Invoke(this, EventArgs.Empty);
        }

        internal VisitorGroup(List<Visitor> members)
        {
            Members = members;
            foreach (var member in Members)
            {
                member.ActionComplete += OnActionComplete;
            }
        }

        internal void OnActionComplete(Visitor v)
        {
            if (ReturningHome)
            {
                v.currentLocation.characters.Remove(v);
                v.CurrentAction = null;
                v.controller = null;
                v.currentLocation.characters.Remove(v);

                if (Members.All(y => y.CurrentAction == null))
                {
                    FinishJob();
                }
            }
            else
            {
                v.CurrentAction.Finished = true;
                if (Members.All(x => x.CurrentAction.Finished))
                {
                    if (!string.IsNullOrEmpty(CurrentActivity.Name) && ActivityManager.ActivitiesInUse.Contains(CurrentActivity.Name))
                    {
                        ActivityManager.FreeActivity(CurrentActivity.Name);
                    }
                    NextActivity();
                }
            }
        }

        internal void NextActivity()
        {
            if (!ActivityQueue.TryDequeue(out CurrentActivity))
            {
                // Add an activity that makes them go to the bus
                CurrentActivity = ActivityManager.GetReturnHomeActivity(Members.Count);
                ReturningHome = true;
            }
            
            List<VisitAction> actions = CurrentActivity.Actions;
            for (var i = 0; i < Members.Count; i++)
            {
                var member = Members[i];
                member.CurrentAction = actions[i];
                member.DoAction();
            }
        }

        internal void GetLookingDirections()
        {
            foreach (Visitor member in Members)
            {
                //member.LookingDirections.Clear();
                //foreach (Visitor other in Members)
                //{
                //	if (member.Equals(other)) continue;
                //	member.LookingDirections.Add(
                //		DirectionIntFromVectors(member.Tile, other.ConvenePoint.ToVector2()));
                //}
            }
        }

        internal void Destroy()
        {
            
        }

        ///// <summary>
        /// Command all members of the group to path to and start heading to the target location and position, and perform the end behavior
        /// </summary>
        /// <param name="location"></param>
        /// <param name="tilePos"></param>
        /// <param name="endBehavior"></param>
        //internal void SetActivities(GameLocation location, Point tilePos)
        //{
        //    List<Point> positions = new List<Point>();

        //    int i = 0;
        //    int j = 0;

        //    Point startPos = tilePos;
        //    Point[] directions = {
        //        new(0, 0), new(1, 0), new(0, -1), new(-1, 0), new(0, 1), new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
        //    };

        //    while (i < Members.Count && j < directions.Length)
        //    {
        //        Point newPos = startPos + directions[j++];

        //        if (Utility.IsTileCollidingInLocation(location, newPos))
        //        {
        //            positions.Add(newPos);
        //            i++;
        //        }
        //    }

        //    if (i != Members.Count)
        //    {
        //        Log.Debug("No free space on target positions for group", LogLevel.Warn);
        //        return;
        //    }
        //    else
        //    {
        //        for (var index = 0; index < Members.Count; index++)
        //        {
        //            Members[index].CurrentAction = new VisitAction(location, positions[index], 0);
        //        }
        //        StartActivities();
        //    }
        //}
    }
}
