using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace StardewMods.FarmHelpers.Framework;
internal static class ModUtility
{
    private static IEnumerable<Point> GetTilesNextTo(Point target)
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
}
