using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using System.Collections.Generic;
using static FarmCafe.Framework.Customers.Customer;
using System.Linq;
using static FarmCafe.Framework.Utilities.Utility;
using FarmCafe.Framework.Managers;
using System.Runtime.CompilerServices;
using StardewValley.Objects;
using System;
using System.IO;

namespace FarmCafe.Framework.Customers
{
	internal static class CustomerBehavior
	{
        internal static void LeaveBus(this Customer me)
        {
            me.collidesWithOtherCharacters.Set(false);
            if (me.Group.Members.Count == 1)
            {
                me.GoToCafe();
            }
            else
            {
                me.HeadTowards(Game1.getLocationFromName("BusStop"), me.busConvenePoint, 2, me.StartConvening);
            }
        }

        public static void GoToCafe(this Customer me)
        {
            me.State = CustomerState.MovingToTable;
            me.collidesWithOtherCharacters.Set(false);
            me.HeadTowards(me.Group.TableLocation, me.Seat.TileLocation.ToPoint(), -1, me.SitDown);
            me.controller.finalFacingDirection = DirectionIntFromPoints(me.controller.pathToEndPoint.Last(), me.Seat.TileLocation.ToPoint());
        }

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
            foreach (Customer mate in me.Group.Members)
                mate.GoToCafe();
        }

        internal static void LookAround(this Customer me)
        {
            me.faceDirection(me.lookingDirections[Game1.random.Next(me.lookingDirections.Count)]);
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
            me.orderTimer = Game1.random.Next(300, 500);
		}

        internal static void SitDownFinishLerping(this Customer me)
        {
            me.State = CustomerState.Sitting;
        }

        internal static void GetUp(this Customer me, int direction)
		{
			me.drawOffset = new Vector2(0, 0);
			var nextPos = me.Position + (DirectionIntToDirectionVector(direction) * 64f);
            me.LerpPosition(me.Position, nextPos, 0.15f);
        }

        internal static void OrderReady(this Customer me)
        {
            me.State = CustomerState.ReadyToOrder;
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
            me.CurrentDialogue.Push(new Dialogue("boyboy", me));
            me.OrderItem = 746;
        }

        internal static void OrderReceive(this Customer me)
        {
            me.emoteLoop = false;
            me.State = CustomerState.Eating;
            me.doEmote(20);
        }

        internal static void DoNothingAndWait(this Customer me)
		{
			me.State = CustomerState.Free;
		}


		internal static void GoHome(this Customer me)
		{

		}


        internal static void HeadTowards(this Customer me, GameLocation targetLocation, Point targetTile, int finalFacingDirection = 0, BehaviorFunction endBehaviorFunction = null)
        {
            me.controller = null;
            me.FreezeMotion = false;
            me.isCharging = false;

            Stack<Point> path = me.PathTo(me.currentLocation, me.getTileLocationPoint(), targetLocation, targetTile);
            
            if (path == null || !path.Any())
            {
                Debug.Log("Customer couldn't find path.", LogLevel.Warn);
                me.GoHome();
                return;
            }

           
            me.controller = new PathFindController(path, me.currentLocation, me, path.Last())
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
        }


        internal static Stack<Point> PathTo(this Customer me, GameLocation startingLocation, Point startTile, GameLocation targetLocation, Point targetTile)
        {
            Stack<Point> path = new Stack<Point>();
            Point locationStartPoint = startTile;
            if (startingLocation.Name.Equals(targetLocation.Name, StringComparison.Ordinal))
                return FindPath(me, locationStartPoint, targetTile, startingLocation);

            List<string> locationsRoute = getLocationRoute(startingLocation, targetLocation);

            if (locationsRoute == null)
            {
                Debug.Log("Route to cafe not found!", LogLevel.Error);
                return null;
            }

            for (int i = 0; i < locationsRoute.Count; i++)
            {
                GameLocation currentLocation = Game1.getLocationFromName(locationsRoute[i]);
                if (i < locationsRoute.Count - 1)
                {
                    Point target = currentLocation.getWarpPointTo(locationsRoute[i + 1]);
                    if (target.Equals(Point.Zero) || locationStartPoint.Equals(Point.Zero))
                        throw new Exception("schedule pathing tried to find a warp point that doesn't exist.");

                    path = combineStacks(path, FindPath(me, locationStartPoint, target, currentLocation));
                    locationStartPoint = currentLocation.getWarpPointTarget(target);
                }
                else
                {
                    path = combineStacks(path, FindPath(me, locationStartPoint, targetTile, currentLocation));
                }
            }

            return path;
        }

        internal static Stack<Point> FindPath(this Customer me, Point startTile, Point targetTile, GameLocation location, int iterations = 600)
        {
            if (IsChair(location.GetFurnitureAt(targetTile.ToVector2())))
            {
                return PathToChair(me, location, startTile, targetTile, location.GetFurnitureAt(targetTile.ToVector2()));
            }
            else if (location.Name.Equals("Farm"))
            {
                return PathFindController.FindPathOnFarm(startTile, targetTile, location, iterations);
            }
            else
            {
                return PathFindController.findPath(startTile, targetTile, new PathFindController.isAtEnd(PathFindController.isAtEndPoint), location, me, iterations);
            }
        }

        internal static Stack<Point> PathToChair(this Customer me, GameLocation location, Point startTile, Point targetTile, Furniture chair)
        {
            var directions = new List<sbyte[]>
            {
                new sbyte[] { 0, -1 }, // up
			    new sbyte[] { -1, 0 }, // left
			    new sbyte[] { 0, 1 }, // down
			    new sbyte[] { 1, 0 }, // right
		    };

            if (!chair.Name.ToLower().Contains("stool"))
                directions.RemoveAt(chair.currentRotation.Value);

            Stack<Point> shortestPath = null;
            int shortestPathLength = 99999;

            foreach (var direction in directions)
            {
                Furniture obstructionChair = location.GetFurnitureAt((targetTile + new Point(direction[0], direction[1])).ToVector2());
                if (IsChair(obstructionChair))
                    continue;

                var pathRightNextToChair = FindPath(
                    me,
                    startTile,
                    targetTile + new Point(direction[0], direction[1]),
                    location,
                    1500
                );

                if (pathRightNextToChair == null || pathRightNextToChair.Count >= shortestPathLength)
                    continue;

                shortestPath = pathRightNextToChair;
                shortestPathLength = pathRightNextToChair.Count;
            }
            if (shortestPath == null || shortestPath.Count == 0)
            {
                Debug.Log("path to chair can't be found");
            }
            return shortestPath;
        }


        internal static void UpdatePathingTarget(this Customer me, Point targetTile)
        {
            //Debug.Log($"repathing to {targetTile.X}, {targetTile.Y}");
            me.HeadTowards(me.currentLocation, targetTile);
        }

        internal static Stack<Point> combineStacks(Stack<Point> original, Stack<Point> toAdd)
        {
            if (toAdd == null)
            {
                return original;
            }
            original = new Stack<Point>(original);
            while (original.Count > 0)
            {
                toAdd.Push(original.Pop());
            }
            return toAdd;
        }

        internal static List<string> getLocationRoute(GameLocation start, GameLocation end)
        {
            foreach (var r in CustomerManager.routesToCafe)
            {
                if (r.First() == start.Name && r.Last() == end.Name)
                {
                    return r;
                }
            }

            return null;
        }

        internal static string GetCurrentPathStack(this Customer me)
        {
            return string.Join(" - ", me.controller.pathToEndPoint);
        }

        internal static string GetCurrentPathStackShort(this Customer me)
        {
            if (me.controller == null)
            {
                return "No controller";
            }

            if (me.controller.pathToEndPoint == null)
            {
                return "No path";
            }
            return $"{me.controller.pathToEndPoint.Count()} nodes: {me.controller.pathToEndPoint.First()} --> {me.controller.pathToEndPoint.Last()}";
        }

    }
}
