using System;
using MyCafe.Customers;
using Netcode;
using StardewValley;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Vector2 = Microsoft.Xna.Framework.Vector2;


namespace MyCafe.ChairsAndTables;

[XmlType("Mods_MonsoonSheep_MyCafe_Table")]
[XmlInclude(typeof(FurnitureTable))]
[XmlInclude(typeof(LocationTable))]
public class Table : INetObject<NetFields>
{
    public NetFields NetFields { get; }

    public NetEnum<TableState> State = new NetEnum<TableState>(TableState.Free);

    public readonly NetString NetCurrentLocation = [];

    public readonly NetVector2 NetPosition = [];

    public virtual NetRectangle BoundingBox { get; set; } = [];

    internal readonly NetCollection<Seat> Seats = [];

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

    internal bool IsReserved => State.Value != TableState.Free;

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
            .AddField(State).AddField(Seats).AddField(NetCurrentLocation).AddField(NetPosition).AddField(BoundingBox);
    }

    internal virtual void Free()
    {
        State.Set(TableState.Free);
        foreach (var s in Seats)
            s.Free();
    }

    internal virtual bool Reserve(List<Customer> customers)
    {
        if (IsReserved || Seats.Count < customers.Count)
            return false;

        for (int i = 0; i < customers.Count; i++)
        {
            customers[i].ReservedSeat = Seats[i];
            Seats[i].Reserve(customers[i]);
        }

        State.Set(TableState.WaitingForCustomers);
        return true;
    }
}

public enum TableState
{
    Free,
    WaitingForCustomers,
    CustomersThinkingOfOrder,
    CustomersDecidedOnOrder,
    CustomersWaitingForFood,
    CustomersEating,
    CustomersFinishedEating
}

internal class TableStateChangedEventArgs : EventArgs
{
    internal TableState OldValue;
    internal TableState NewValue;
}