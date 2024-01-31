using Microsoft.Xna.Framework;
using MonsoonSheep.Stardew.Common;
using MyCafe.Customers;
using Netcode;
using StardewValley;
using System.Xml.Serialization;

namespace MyCafe.Locations.Objects;

[XmlType("Mods_MonsoonSheep_MyCafe_Seat")]
[XmlInclude(typeof(FurnitureSeat))]
[XmlInclude(typeof(LocationSeat))]
public abstract class Seat : INetObject<NetFields>
{
    public NetFields NetFields { get; }

    private readonly NetPoint _position = [];

    private readonly NetRef<Customer?> _reservingCustomer = [];

    private Table? _table;

    internal Table Table
    {
        get => _table ??= Mod.Cafe.GetTableOfSeat(this);
        set => _table = value;
    }

    internal Customer? ReservingCustomer
    {
        get => _reservingCustomer.Value;
        set => _reservingCustomer.Set(value);
    }

    internal GameLocation? Location
    {
        get
        {
            if (Table?.CurrentLocation != null)
                return CommonHelper.GetLocation(Table.CurrentLocation);
            return null;
        }
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
        NetFields.SetOwner(this)
            .AddField(_position).AddField(_reservingCustomer);
    }

    internal virtual bool Reserve(Customer customer)
    {
        if (ReservingCustomer != null)
            return false;

        customer.ReservedSeat = this;
        ReservingCustomer = customer;
        return true;
    }

    internal virtual void Free()
        => ReservingCustomer = null;
}
