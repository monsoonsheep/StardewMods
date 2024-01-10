using Microsoft.Xna.Framework;
using MyCafe.Customers;
using Netcode;
using StardewValley.Locations;
using StardewValley.Network;
using System.Xml.Serialization;

namespace MyCafe.ChairsAndTables;

[XmlType("Mods_MonsoonSheep_MyCafe_Seat")]
[XmlInclude(typeof(FurnitureSeat))]
[XmlInclude(typeof(LocationSeat))]
public abstract class Seat: INetObject<NetFields>
{
    public NetFields NetFields { get; }

    private readonly NetPoint _position = new NetPoint();

    private readonly NetNPCRef _reservingCustomer = new NetNPCRef();

    private Table _table;

    internal Table Table
    {
        get => _table ??= Mod.Cafe.GetTableOfSeat(this);
        set => _table = value;
    }

    internal Customer ReservingCustomer
    {
        get => _reservingCustomer.Get(Utility.GetLocationFromName(Table?.CurrentLocation)) as Customer;
        set => _reservingCustomer.Set(Utility.GetLocationFromName(Table?.CurrentLocation), value);
    }

    internal Point Position
    {
        get => _position.Value;
        set => _position.Set(value);
    }

    internal virtual int SittingDirection { get; set; }

    internal bool IsReserved 
        => ReservingCustomer != null;

    public Seat()
    {
        NetFields = new NetFields(NetFields.GetNameForInstance(this));
        InitNetFields();
    }

    public Seat(Table table) : this()
    {
        Table = table;
    }

    protected virtual void InitNetFields()
    {
        NetFields.SetOwner(this).AddField(_position);
    }

    internal virtual bool Reserve(Customer customer)
    {
        if (ReservingCustomer != null)
            return false;
        
        customer.ReservedSeat.Set(this);
        return true;
    }

    internal virtual void Free()
        => ReservingCustomer = null;
}
