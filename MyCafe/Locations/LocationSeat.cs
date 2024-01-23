using Microsoft.Xna.Framework;
using MyCafe.Customers;
using StardewValley;
using System.Linq;
using System.Xml.Serialization;

namespace MyCafe.Locations;

[XmlType("Mods_MonsoonSheep_MyCafe_LocationSeat")]
public sealed class LocationSeat : Seat
{
    public LocationSeat() : base()
    {

    }

    public LocationSeat(Point position, Table table) : base(table)
    {
        Position = position;
    }

    internal override int SittingDirection
    {
        get
        {
            if (Table is LocationTable table)
            {
                Rectangle tableBox = table.BoundingBox.Value;
                Vector2 myPos = new Vector2(Position.X * 64, Position.Y * 64);
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
        MapSeat mapSeat = location.mapSeats.FirstOrDefault(s => s.tilePosition.Value.Equals(Position.ToVector2()));
        mapSeat?.sittingFarmers.Add(Game1.MasterPlayer.UniqueMultiplayerID, 0);

        return true;
    }

    internal override void Free()
    {
        base.Free();
        GameLocation location = Utility.GetLocationFromName(Table.CurrentLocation);
        MapSeat mapSeat = location.mapSeats.ToList().FirstOrDefault(s => s.tilePosition.Value.Equals(Position.ToVector2()));
        mapSeat?.RemoveSittingFarmer(Game1.MasterPlayer);
    }

}