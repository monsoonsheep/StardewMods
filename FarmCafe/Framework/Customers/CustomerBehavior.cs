using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using System.Collections.Generic;
using static FarmCafe.Framework.Customers.Customer;
using System.Linq;
using static FarmCafe.Framework.Utilities.Utility;
using Pathfinder = StardewValley.PathFindController;
using Pathing = FarmCafe.Framework.Customers.CustomerPathing;

namespace FarmCafe.Framework.Customers
{
	internal static class CustomerBehavior
	{
		internal static void StartConvening(this Customer me)
		{
			me.conveneWaitingTimer = Game1.random.Next(500, 3000);
			me.State = CustomerState.Convening;
			me.Group.GetLookingDirections();
		}


        public static void FinishConvening(this Customer me)
        {
            foreach (Customer mate in me.Group.Members)
            {
                if (mate.State != CustomerState.Convening)
                {
                    return;
                }
            }
            me.Group.GroupStartMoving();
        }

        internal static void SitDown(this Customer me)
		{
			me.controller = null;
			me.isCharging = true;

			var mypos = me.Position;
			var seatpos = me.Seat.TileLocation * 64f;

			me.State = CustomerState.GoingToSit;
			me.LerpPosition(mypos, seatpos, 0.15f);
			me.faceDirection(me.Seat.GetSittingDirection());

			me.drawOffset = me.facingDirection.Value switch
			{
				0 => new Vector2(0f, -24f), // up
				1 => new Vector2(12f, -8f), // right
				2 => new Vector2(0f, 0f), // down 
				3 => new Vector2(-12f, -8f), // left
				_ => me.drawOffset
			};

			me.Breather = true;

			me.Seat.modData["FarmCafeSeat"] = "T";
            me.orderTimer = Game1.random.Next(300, 500);
		}

		internal static void GetUp(this Customer me, int direction)
		{
			me.Seat.modData.Remove("FarmCafeSeat");
			me.drawOffset = new Vector2(0, 0);

			var nextPos = me.Position + (DirectionIntToDirectionVector(direction) * 64f);
            me.LerpPosition(me.Position, nextPos, 0.15f);
        }

        internal static void OrderReady(this Customer me)
        {
            foreach (Customer mate in me.Group.Members)
            {
                if (mate.State != CustomerState.ReadyToOrder)
                {
                    return;
                }
            }

            me.tableCenterForEmote = me.GetTableCenter();
            me.emoteLoop = true;
            me.doEmote(16);
        }

        internal static void DoNothingAndWait(this Customer me)
		{
			me.State = CustomerState.Free;
		}


		internal static void GoHome(this Customer me)
		{

		}


        internal static void HeadTowards(this Customer me, Point targetTile, int finalFacingDirection = 0, BehaviorFunction endBehaviorFunction = null)
        {
            me.controller = null;
            me.FreezeMotion = false;
            
            me.isCharging = true;
            Stack<Point> path = Pathing.FindPath(me.getTileLocationPoint(), targetTile, me.currentLocation);
            me.isCharging = false;
            
            if (path == null)
            {
                if (me.State == CustomerState.MovingToTable)
                {
                    Debug.Log("Customer can't get to their chair.", LogLevel.Warn);
                }
                else
                {
                    foreach (var pos in AdjacentTiles(targetTile))
                    {
                        path = Pathfinder.findPathForNPCSchedules(me.getTileLocationPoint(), pos, me.currentLocation, 500);
                        if (path != null) break;
                    }
                }
            }

            if (path == null || !path.Any())
            {
                Debug.Log("Customer couldn't find path.", LogLevel.Warn);
                me.GoHome();
                return;
            }

            if (me.State == CustomerState.MovingToTable && me.currentLocation.Name == "Farm")
            {
                finalFacingDirection = DirectionIntFromPoints(path.Last(), targetTile);
            }

            me.controller = new Pathfinder(path, me.currentLocation, me, new Point(0, 0))
            {
                nonDestructivePathing = true,
                endBehaviorFunction = endBehaviorFunction != null ? (c, loc) => endBehaviorFunction() : null,
                finalFacingDirection = finalFacingDirection
            };

            if (me.controller == null)
            {
                Debug.Log("Can't construct controller.", LogLevel.Warn);
                me.GoHome();
            }

            //Debug.Log($"Path = {this.GetCurrentPathStackShort()}");
        }


        internal static void ArriveAtFarm(this Customer me)
		{
			if (me.Seat == null || me.Group.ReservedTable == null)
			{
				Debug.Log($"Couldn't find a seat for {me.Name}");
                
            }


            me.HeadTowards(me.Seat.TileLocation.ToPoint(), -1, me.SitDown);
		}
	}
}
