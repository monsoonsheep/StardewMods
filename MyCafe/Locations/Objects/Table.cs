using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using MyCafe.Characters;
using MyCafe.Enums;
using MyCafe.Netcode;
using Netcode;
using StardewValley;


namespace MyCafe.Locations.Objects;

public abstract class Table : INetObject<NetFields>
{
    public NetFields NetFields { get; }

    public NetEnum<TableState> State = new NetEnum<TableState>(TableState.Free);

    public readonly NetString NetCurrentLocation = [];

    public readonly NetVector2 NetPosition = [];

    public virtual NetRectangle BoundingBox { get; set; } = [];

    internal readonly NetCollection<Seat> Seats = [];

    public virtual Vector2 Position
    {
        get => this.NetPosition.Value;
        set => this.NetPosition.Set(value);
    }

    internal string CurrentLocation
    {
        get => this.NetCurrentLocation.Value;
        set => this.NetCurrentLocation.Set(value);
    }

    internal virtual Vector2 Center => this.BoundingBox.Center.ToVector2();

    internal bool IsReserved => this.State.Value != TableState.Free;

    protected Table()
    {
        this.NetFields = new NetFields(NetFields.GetNameForInstance(this));
        // ReSharper disable once VirtualMemberCallInConstructor
        this.InitNetFields();
    }

    protected Table(string location) : this()
    {
        this.CurrentLocation = location;
    }

    protected virtual void InitNetFields()
    {
        this.NetFields.SetOwner(this)
            .AddField(this.State).AddField(this.Seats).AddField(this.NetCurrentLocation).AddField(this.NetPosition).AddField(this.BoundingBox);
    }

    internal virtual void Free()
    {
        // Log.Debug($"Freeing {(this is LocationTable ? "Location" : "Furniture")} table");
        this.State.Set(TableState.Free);
        foreach (Seat s in this.Seats)
            s.Free();
    }

    internal virtual bool Reserve(List<NPC> customers)
    {
        if (this.IsReserved || this.Seats.Count < customers.Count)
            return false;

        for (int i = 0; i < customers.Count; i++)
        {
            customers[i].set_Seat(this.Seats[i]);
            this.Seats[i].Reserve(customers[i]);
        }

        this.State.Set(TableState.CustomersComing);
        return true;
    }
}


internal class TableStateChangedEventArgs : EventArgs
{
    internal TableState OldValue;
    internal TableState NewValue;
}
