#region Usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Pathfinding;
using VisitorFramework.Framework.Visitors;
#endregion

namespace VisitorFramework.Framework.Visitors.Activities
{
    public class VisitAction
    {
        public GameLocation Location;
        public Point TilePosition;
        public int FacingDirection;

        internal bool Finished; 

        public VisitAction(GameLocation location, Point tilePosition, int facingDirection)
        {
            Location = location;
            TilePosition = tilePosition;
            FacingDirection = facingDirection;
        }

        public virtual void Start(Visitor v)
        {
            v.HeadTowards(Location, this.TilePosition, 0, (_, _) => Behavior(v));
        }

        public virtual void Behavior(Visitor v)
        {
            v.faceDirection(FacingDirection);
        }

        public virtual void Finish(Visitor v)
        {
            v.FinishAction();
        }
    }
}
