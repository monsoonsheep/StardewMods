using System.Collections.Generic;
using MyCafe.Framework.Customers;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Vector2 = Microsoft.Xna.Framework.Vector2;


namespace MyCafe.Framework.Objects;

internal abstract class Table
{
    internal bool IsReadyToOrder;

    protected Table(string location) => CurrentLocation = location;

    internal virtual List<Seat> Seats { get; set; }

    internal virtual Vector2 Position { get; set; }

    internal string CurrentLocation { get; set; }

    internal virtual bool IsReserved { get; set; }

    internal virtual Rectangle BoundingBox { get; set; }

    internal virtual Vector2 Center => BoundingBox.Center.ToVector2();

    internal virtual void Free()
    {
        IsReadyToOrder = false;
        IsReserved = false;
        Seats.ForEach(s => s.Free());
    }

    internal virtual bool Reserve(List<Customer> customers)
    {
        if (IsReserved || Seats.Count < customers.Count)
            return false;

        for (int i = 0; i < customers.Count; i++)
        {
            customers[i].ReservedSeat = Seats[i];
        }

        IsReserved = true;
        return true;
    }
}
