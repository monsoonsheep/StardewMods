using System.Linq;
using Microsoft.Xna.Framework;
using MyCafe.Framework.Customers;
using StardewValley;

namespace MyCafe.Framework.ChairsAndTables;

internal sealed class MapChair : Seat
{
    public MapChair(Vector2 position)
        => Position = position;

    internal override int SittingDirection
    {
        get
        {
            if (Table is MapTable table)
            {
                Rectangle tableBox = table.BoundingBox;
                Vector2 myPos = Position * 64;
                if (tableBox.Contains(myPos.X, myPos.Y - 64))
                    return 0;
                if (tableBox.Contains(myPos.X + 64, myPos.Y))
                    return 1;
                if (tableBox.Contains(myPos.X, myPos.Y + 64))
                    return 2;
                if (tableBox.Contains(myPos.X - 64, myPos.Y))
                    return 3;
            }

            return 0;
        }
    }

    internal override bool Reserve(Customer customer)
    {
        if (!base.Reserve(customer))
            return false;

        GameLocation location = Utility.GetLocationFromName(Table.CurrentLocation);
        MapSeat mapSeat = location.mapSeats.FirstOrDefault(s => s.tilePosition.Value.Equals(Position));
        mapSeat?.sittingFarmers.Add(Game1.MasterPlayer.UniqueMultiplayerID, 0);

        return true;
    }

    internal override void Free()
    {
        base.Free();
        GameLocation location = Utility.GetLocationFromName(Table.CurrentLocation);
        MapSeat mapSeat = location.mapSeats.ToList().FirstOrDefault(s => s.tilePosition.Value.Equals(Position));
        mapSeat?.RemoveSittingFarmer(Game1.MasterPlayer);
    }

}
