#region Usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewValley;
using VisitorFramework.Framework.Visitors;
using xTile.Tiles;
#endregion

namespace VisitorFramework.Framework.Visitors.Activities
{
    public class ActionWalkInSquare : VisitAction
    {
        internal int SquareWidth;
        internal int SquareHeight;
        internal int PreferredLookingDirection;

        public ActionWalkInSquare(GameLocation location, Point tilePosition, int squareWidth, int squareHeight, int preferredLookingDirection) : base(location, tilePosition, -1)
        {
            SquareWidth = squareWidth;
            SquareHeight = squareHeight;
            PreferredLookingDirection = preferredLookingDirection;
        }

        public override void Behavior(Visitor v)
        {
            v.lastCrossroad = new Rectangle(v.TilePoint.X * 64, v.TilePoint.Y * 64, 64, 64);
            v.squareMovementFacingPreference = PreferredLookingDirection;
            v.walkInSquare(SquareWidth, SquareHeight, 2000);
        }
    }
}
