using System.Collections.Generic;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Vector2 = Microsoft.Xna.Framework.Vector2;


namespace MyCafe.Framework.Objects;

internal class MapTable : Table
{
    internal MapTable(Rectangle boundingBox, string location, List<Vector2> seatPositions) : base(location)
    {
        base.BoundingBox = boundingBox;
        base.Position = boundingBox.Center.ToVector2();

        base.Seats = new List<Seat>();
        foreach (var seat in seatPositions)
        {
            var mapSeat = new MapChair(seat)
            {
                Table = this
            };
            base.Seats.Add(mapSeat);
        }
    }
}
