using StardewValley;
using StardewValley.Objects;
using System.Linq;
using MyCafe.Locations.Objects;

namespace MyCafe;

internal static class Utility
{
    internal static bool IsChair(Furniture furniture)
    {
        return furniture.furniture_type.Value is 0 or 1 or 2;
    }

    internal static bool IsTable(Furniture furniture)
    {
        return furniture.furniture_type.Value == 11;
    }

    internal static bool IsTableTracked(Furniture table, GameLocation location, out FurnitureTable outTable)
    {
        FurnitureTable? t = Mod.Cafe.Tables
            .OfType<FurnitureTable>().FirstOrDefault(t => t.CurrentLocation.Equals(location.NameOrUniqueName) && t.Position == table.TileLocation);
        if (t != null)
        {
            outTable = t;
            return true;
        }
        else
        {
            outTable = null!;
            return false;
        }
    }
}