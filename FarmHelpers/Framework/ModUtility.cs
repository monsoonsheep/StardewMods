using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Force.DeepCloner;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;

namespace StardewMods.FarmHelpers.Framework;
internal static class ModUtility
{
    internal static IEnumerable<Point> GetTilesSurrounding(Point target)
    {
        yield return new Point(target.X - 1, target.Y);
        yield return new Point(target.X - 1, target.Y - 1);
        yield return new Point(target.X, target.Y - 1);
        yield return new Point(target.X + 1, target.Y - 1);
        yield return new Point(target.X + 1, target.Y);
        yield return new Point(target.X + 1, target.Y + 1);
        yield return new Point(target.X, target.Y + 1);
        yield return new Point(target.X - 1, target.Y + 1);
    }

    internal static IEnumerable<Point> GetTilesNextTo(Point target)
    {
        yield return new Point(target.X - 1, target.Y);
        yield return new Point(target.X, target.Y - 1);
        yield return new Point(target.X + 1, target.Y);
        yield return new Point(target.X, target.Y + 1);
    }

    internal static IEnumerable<Point> GetTilesNextTo(Point target, int directionToPrioritize)
    {
        Point[] points = [
            new Point(target.X, target.Y - 1),
            new Point(target.X + 1, target.Y),
            new Point(target.X, target.Y + 1),
            new Point(target.X - 1, target.Y)
            ];

        yield return points[directionToPrioritize];

        for (int i = 0; i < 4; i++)
        {
            if (i != directionToPrioritize)
            {
                yield return points[i];
            }
        }
    }

    internal static IEnumerable<Point> GetEmptyTilesNextTo(GameLocation loc, Point target, int directionToPrioritize)
    {
        foreach (Point p in GetTilesNextTo(target, directionToPrioritize))
        {
            if (loc.isTilePassable(p.ToVector2()) && loc.GetFurnitureAt(p.ToVector2()) == null)
            {
                yield return p;
            }
        }
    }

    internal static IEnumerable<Point> GetEmptyTilesNextTo(GameLocation loc, Point target)
    {
        foreach (Point p in GetTilesNextTo(target))
        {
            if (loc.isTilePassable(p.ToVector2()) && loc.GetFurnitureAt(p.ToVector2()) == null) {
                yield return p;
            }
        }
    }

    internal static int GetDirectionFromTiles(Point p1, Point p2)
    {
        return Utility.getDirectionFromChange(p1.ToVector2(), p2.ToVector2());
    }

    internal static List<Point> GetNaturalPath(Point startTile, List<Point> targets)
    {
        List<Point> path = [];
        Point current = startTile;
        List<Point> remaining = targets.ShallowClone();

        while (remaining.Any())
        {
            Point closest = remaining.
                OrderBy((p) => Utility.distance(current.X, p.X, current.Y, p.Y))
                .First();

            path.Add(closest);
            remaining.Remove(closest);

            current = closest;
        }

        return path;
    }

    internal static Point GetEntryTileForBuildingIndoors(Building building)
    {
        GameLocation farm = building.GetParentLocation();

        Warp warp = farm.getWarpFromDoor(building.getPointForHumanDoor());
        Point entry = new Point(warp.TargetX, warp.TargetY - 1);

        return entry;
    }

    internal static void AddDelayedAction(Action action, int delay)
    {
        Game1.delayedActions.Add(new DelayedAction(delay, action));
    }

    internal static bool IsFarmOrIndoor(GameLocation location)
    {
        return location is Farm || (location.ParentBuilding is Building b && b.GetParentLocation() is Farm);
    }

    internal static bool IsCollectableObject(StardewValley.Object obj)
    {
        return (obj.QualifiedItemId == "(O)444" || obj.HasContextTag("egg_item") || obj.HasContextTag("(O)446") || obj.HasContextTag("(O)440"));
    }
}
