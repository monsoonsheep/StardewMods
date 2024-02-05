using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using MonsoonSheep.Stardew.Common;
using MyCafe.Characters;
using Netcode;
using StardewValley;

namespace MyCafe.Locations.Objects;

[XmlType("Mods_MonsoonSheep_MyCafe_Seat")]
[XmlInclude(typeof(FurnitureSeat))]
[XmlInclude(typeof(LocationSeat))]
public abstract class Seat : INetObject<NetFields>
{
    public NetFields NetFields { get; }

    private readonly NetPoint NetPosition = [];

    private readonly NetRef<Customer?> NetReservingCustomer = [];

    private Table? TableField;

    internal int OptimalDirectionToGetUp;

    internal Table Table
    {
        get => this.TableField ??= Mod.Cafe.GetTableOfSeat(this);
        set => this.TableField = value;
    }

    internal Customer? ReservingCustomer
    {
        get => this.NetReservingCustomer.Value;
        set => this.NetReservingCustomer.Set(value);
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
        get => this.NetPosition.Value;
        set => this.NetPosition.Set(value);
    }

    internal virtual int SittingDirection { get; set; }

    internal bool IsReserved
        => this.ReservingCustomer != null;

    public Seat()
    {
        this.NetFields = new NetFields(NetFields.GetNameForInstance(this));
        // ReSharper disable once VirtualMemberCallInConstructor
        this.InitNetFields();
    }

    public Seat(Table table) : this()
    {
        this.Table = table;
        GameLocation? location = this.Location;
        if (location == null)
        {
            Log.Error("Cannot find location for seat");
            return;
        }
    }

    protected virtual void InitNetFields()
    {
        this.NetFields.SetOwner(this)
            .AddField(this.NetPosition).AddField(this.NetReservingCustomer);
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
    {
        Log.Debug("Freeing base seat");
        this.ReservingCustomer = null;
    }
}
