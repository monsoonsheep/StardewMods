using System.Linq;
using Microsoft.Xna.Framework;
using MonsoonSheep.Stardew.Common;
using MyCafe.Netcode;
using Netcode;
using StardewValley;

namespace MyCafe.Locations.Objects;

public abstract class Seat : INetObject<NetFields>
{
    public NetFields NetFields { get; }

    private readonly NetPoint NetPosition = [];

    private readonly NetRef<NPC?> NetReservingCustomer = [];

    private Table? TableField;

    internal Table Table
    {
        get => this.TableField ??= Mod.Cafe.Tables.FirstOrDefault(t => t.Seats.Contains(this))!;
        set => this.TableField = value;
    }

    internal NPC? ReservingCustomer
    {
        get => this.NetReservingCustomer.Value;
        set => this.NetReservingCustomer.Set(value);
    }

    internal GameLocation Location
        => CommonHelper.GetLocation(this.Table.CurrentLocation)!;

    internal Point Position
    {
        get => this.NetPosition.Value;
        set => this.NetPosition.Set(value);
    }

    internal virtual int SittingDirection { get; set; }

    internal bool IsReserved
        => this.ReservingCustomer != null;

    public abstract Vector2 SittingPosition { get; }

    protected Seat()
    {
        this.NetFields = new NetFields(NetFields.GetNameForInstance(this));
        // ReSharper disable once VirtualMemberCallInConstructor
        this.InitNetFields();
    }

    protected Seat(Table table) : this()
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

    internal virtual bool Reserve(NPC customer)
    {
        if (this.ReservingCustomer != null)
            return false;

        customer.set_Seat(this);
        this.ReservingCustomer = customer;
        return true;
    }

    internal virtual void Free()
    {
        this.ReservingCustomer = null;
    }
}
