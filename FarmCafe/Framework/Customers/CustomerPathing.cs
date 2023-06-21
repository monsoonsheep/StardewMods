using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using System.Collections.Generic;
using System.Linq;
using static FarmCafe.Framework.Utilities.Utility;

namespace FarmCafe.Framework.Customers
{
	internal static class CustomerPathing
	{
		internal static Stack<Point> FindPath(Point startTile, Point targetTile, GameLocation location)
		{
			if (IsChair(location.GetFurnitureAt(targetTile.ToVector2())))
			{
				return GetPathToChair(startTile, targetTile, location.GetFurnitureAt(targetTile.ToVector2()));
			}
			else if (location.Equals(Game1.getLocationFromName("Farm")))
			{
				return PathFindController.FindPathOnFarm(startTile, targetTile, location, 600);
			}
			else
			{
				return PathFindController.findPathForNPCSchedules(startTile, targetTile, location, 600);
			}
		}


		internal static Stack<Point> GetPathToChair(Point startTile, Point targetTile, Furniture chair)
		{
			var directions = new List<sbyte[]>
			{
				new sbyte[] { 0, -1 }, // up
			    new sbyte[] { -1, 0 }, // left
			    new sbyte[] { 0, 1 }, // down
			    new sbyte[] { 1, 0 }, // right
		    };

			if (!chair.Name.ToLower().Contains("stool"))
			{
				directions.RemoveAt(chair.currentRotation.Value);
			}

			Stack<Point> shortestPath = null;
			int shortestPathLength = 99999;

			foreach (var direction in directions)
			{
				var pathRightNextToChair = PathFindController.FindPathOnFarm(
					startTile,
					targetTile + new Point(direction[0], direction[1]),
					Game1.getFarm(),
					600
				);

				if (pathRightNextToChair == null || pathRightNextToChair.Count >= shortestPathLength)
					continue;

				shortestPath = pathRightNextToChair;
				shortestPathLength = pathRightNextToChair.Count;
			}

			return shortestPath;

		}


		internal static void UpdatePathingTarget(this Customer me, Point targetTile)
		{
			//Debug.Log($"repathing to {targetTile.X}, {targetTile.Y}");
			//HeadTowards(targetTile);
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
