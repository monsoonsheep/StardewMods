using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;


namespace MyCafe.Locations.Objects;

[XmlType("Mods_MonsoonSheep_MyCafe_LocationTable")]
public class LocationTable : Table
{
    public LocationTable() : base()
    {

    }

    internal LocationTable(Rectangle boundingBox, string location, List<Vector2> seatPositions) : base(location)
    {
        base.BoundingBox.Set(boundingBox);
        base.Position = boundingBox.Center.ToVector2();

        foreach (var seat in seatPositions)
        {
            var locationSeat = new LocationSeat(seat.ToPoint(), this);
            this.Seats.Add(locationSeat);
        }
    }
}
