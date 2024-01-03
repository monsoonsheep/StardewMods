using Microsoft.Xna.Framework;
using MyCafe.Framework.Customers;

namespace MyCafe.Framework.Objects;

internal abstract class Seat
{
    internal virtual Table Table { get; set; }

    internal virtual Vector2 Position { get; set; }

    internal virtual Customer ReservingCustomer { get; set; }

    internal abstract int SittingDirection { get; }

    internal virtual bool IsReserved => ReservingCustomer != null;

    internal virtual bool Reserve(Customer customer)
    {
        if (ReservingCustomer != null)
            return false;

        customer.ReservedSeat = this;
        customer.modData["MonsoonSheep.MyCafe_ModDataSeatPos"] = $"{Position.X} {Position.Y}";
        ReservingCustomer = customer;

        return true;
    }

    internal virtual void Free()
        => ReservingCustomer = null;

}
