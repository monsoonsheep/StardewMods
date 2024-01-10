using System.Collections.Generic;
using System.Xml.Serialization;
using MyCafe.Customers;
using Netcode;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Vector2 = Microsoft.Xna.Framework.Vector2;


namespace MyCafe.ChairsAndTables;

[XmlType("Mods_MonsoonSheep_MyCafe_Table")]
[XmlInclude(typeof(FurnitureTable))]
[XmlInclude(typeof(LocationTable))]
public abstract class Table : INetObject<NetFields>
{
    public NetFields NetFields { get; }

    public readonly NetString NetCurrentLocation = new NetString();

    public readonly NetVector2 NetPosition = new NetVector2();

    public virtual NetRectangle BoundingBox { get; set; } = new NetRectangle();

    public NetBool IsReadyToOrder = new NetBool(false);

    public NetBool IsReserved = new NetBool(false);

    internal readonly NetCollection<Seat> Seats = new NetCollection<Seat>();

    public virtual Vector2 Position
    {
        get => NetPosition.Value;
        set => NetPosition.Set(value);
    }

    internal string CurrentLocation
    {
        get => NetCurrentLocation.Value;
        set => NetCurrentLocation.Set(value);
    }

    internal virtual Vector2 Center => BoundingBox.Center.ToVector2();

    public Table()
    {
        NetFields = new NetFields(NetFields.GetNameForInstance(this));
        InitNetFields();
    }

    protected Table(string location) : this()
    {
        CurrentLocation = location;
    }

    protected virtual void InitNetFields()
    {
        NetFields.SetOwner(this)
            .AddField(IsReserved).AddField(IsReadyToOrder).AddField(Seats).AddField(NetCurrentLocation).AddField(NetPosition).AddField(BoundingBox);
    }

    internal virtual void Free()
    {
        IsReadyToOrder.Set(false);
        IsReserved.Set(false);
        foreach (var s in Seats)
            s.Free();
    }

    internal virtual bool Reserve(List<Customer> customers)
    {
        if (IsReserved.Value || Seats.Count < customers.Count)
            return false;

        for (int i = 0; i < customers.Count; i++)
        {
            customers[i].ReservedSeat.Set(Seats[i]);
            Seats[i].Reserve(customers[i]);
        }

        IsReserved.Set(true);
        return true;
    }
}
