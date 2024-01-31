using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using MonsoonSheep.Stardew.Common;
using MyCafe.Customers;
using Netcode;
using StardewValley;

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
        get => this._table ??= Mod.Cafe.GetTableOfSeat(this);
        set => this._table = value;
    }

    internal Customer? ReservingCustomer
    {
        get => this._reservingCustomer.Value;
        set => this._reservingCustomer.Set(value);
    }

    internal GameLocation? Location
    {
        get
        {
            if (this.Table?.CurrentLocation != null)
                return CommonHelper.GetLocation(this.Table.CurrentLocation);
            return null;
        }
    }

    internal Point Position
    {
        get => this._position.Value;
        set => this._position.Set(value);
    }

    internal virtual int SittingDirection { get; set; }

    internal bool IsReserved
        =>
            this.ReservingCustomer != null;

    public Seat()
    {
        this.NetFields = new NetFields(NetFields.GetNameForInstance(this));
        this.InitNetFields();
    }

    public Seat(Table table) : this()
    {
        this.Table = table;
    }

    protected virtual void InitNetFields()
    {
        this.NetFields.SetOwner(this)
            .AddField(this._position).AddField(this._reservingCustomer);
    }

    internal virtual bool Reserve(Customer customer)
    {
        if (this.ReservingCustomer != null)
            return false;

        customer.ReservedSeat = this;
        this.ReservingCustomer = customer;
        return true;
    }

    internal virtual void Free()
        =>
            this.ReservingCustomer = null;
}
