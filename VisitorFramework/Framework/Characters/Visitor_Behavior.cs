using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VisitorFramework.Framework.Managers;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Pathfinding;
using static VisitorFramework.Framework.Utility;
using SUtility = StardewValley.Utility;
namespace VisitorFramework.Framework.Characters
{
    public partial class Visitor
    {
        
        public void SetConvenePoint(GameLocation location, Point posistion)
        {

        }

        public void StartConvening()
        {
            controller = null;
            conveneWaitingTimer = Game1.random.Next(500, 3000);
            State.Set(VisitorState.Convening);
            Group.GetLookingDirections();
        }

        public void FinishConvening()
        {
            
        }

        public void LookAround()
        {
            faceDirection(LookingDirections[Game1.random.Next(LookingDirections.Count)]);
        }

        public void SitDown(int direction, int facingDirection)
        {
            IsSitting.Set(true);
            State.Set(VisitorState.Sitting);
            controller = null;
            isCharging = true;

            Vector2 seatTile = Tile + DirectionIntToDirectionVector(direction);
            LerpPosition(
                Position,
                seatTile * 64f,
                0.15f,
                OnSitDown);

            faceDirection(facingDirection);

            // Correct drawing position because of sitting issues
            Vector2 vec = facingDirection switch
            {
                0 => new Vector2(0f, -24f), // up
                1 => new Vector2(12f, -8f), // right
                2 => new Vector2(0f, 0f), // down 
                3 => new Vector2(-12f, -8f), // left
                _ => drawOffsetForSeat.Value
            };
            drawOffsetForSeat.Set(vec);
        }

        public void GetUpFromSeat(int direction)
        {
            
        }

        public void GetUpFromSeat()
        {
            State.Set(VisitorState.Leaving);
            int[] directions = { (FacingDirection + 1) % 4, (FacingDirection + 3) % 4, (FacingDirection + 2) % 4 };

            foreach (int direction in directions)
            {
                var boundingBox = GetBoundingBox();
                boundingBox.X += (int)DirectionIntToDirectionVector(direction).X * 64;
                boundingBox.Y += (int)DirectionIntToDirectionVector(direction).Y * 64;

                if (!currentLocation.isCollidingPosition(boundingBox, Game1.viewport, false, 0, glider: false, this))
                {
                    IsSitting.Set(false);
                    drawOffsetForSeat.Set(new Vector2(0, 0));
                    var nextPos = Position + (DirectionIntToDirectionVector(direction) * 64f);
                    LerpPosition(
                        Position,
                        nextPos,
                        0.15f,
                        null
                        // TODO: ^ Next activity for group
                        );

                    OnGetUp();
                    return;
                }
            }
            // TODO: Handle when Visitor can't get up from seat because of collisions
        }

        public void EndVisit()
        {
            if (ignoreScheduleToday)
            {
                OnLeaveFarm.Invoke(this);
            }
            else
            {
                IsInvisible = true;
                base.currentLocation.characters.Remove(this);
                if (Group.Members.All(c => c.IsInvisible))
                    VisitorManager.DeleteGroup(Group);
            }
        }
    }
}