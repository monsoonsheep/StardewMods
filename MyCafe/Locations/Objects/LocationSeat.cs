using System;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using MonsoonSheep.Stardew.Common;
using MyCafe.Characters;
using StardewValley;

namespace MyCafe.Locations.Objects;

public sealed class LocationSeat : Seat
{
    public LocationSeat() : base()
    {
    }

    public LocationSeat(Point position, Table table) : base(table)
    {
        this.Position = position;
    }

    internal override int SittingDirection
    {
        get
        {
            if (this.Table is LocationTable table)
            {
                Rectangle tableBox = table.BoundingBox.Value;
                Vector2 myPos = new Vector2(this.Position.X * 64, this.Position.Y * 64);
                if (tableBox.Contains(myPos.X, myPos.Y - 64))
                    return 0;
                if (tableBox.Contains(myPos.X + 64, myPos.Y))
                    return 1;
                if (tableBox.Contains(myPos.X, myPos.Y + 64))
                    return 2;
                if (tableBox.Contains(myPos.X - 64, myPos.Y))
                    return 3;
            }

            return 0;
        }
    }

    internal override bool Reserve(NPC customer)
    {
        if (!base.Reserve(customer))
            return false;

        GameLocation? location = CommonHelper.GetLocation(this.Table.CurrentLocation);
        if (location == null)
            return false;

        try
        {
            MapSeat? mapSeat = location.mapSeats.FirstOrDefault(s => s.tilePosition.Value.Equals(this.Position.ToVector2()));
            mapSeat?.sittingFarmers.Add(Game1.MasterPlayer.UniqueMultiplayerID, 0);
        }
        catch (ArgumentException ex)
        {
            Log.Debug("Couldn't add fake farmer to map seat");
            Log.Trace($"{ex.Message}");
            Log.Trace($"{ex.StackTrace}");
            return false;
        }

        return true;
    }

    internal override void Free()
    {
        Log.Debug("Freeing seat");
        base.Free();
        GameLocation? location = CommonHelper.GetLocation(this.Table.CurrentLocation);
        MapSeat? mapSeat = location?.mapSeats?.ToList().FirstOrDefault(s => s.tilePosition.Value.Equals(this.Position.ToVector2()));
        if (mapSeat != null)
        {
            try
            {
                mapSeat.RemoveSittingFarmer(Game1.MasterPlayer);
            }
            catch (Exception ex)
            {
                Log.Debug("Couldn't remove farmer map seat");
                Log.Trace($"{ex.Message}\n{ex.StackTrace}");
            }
        }
        else
        {
            Log.Debug("Couldn't find map seat for freeing location seat");
        }
    }

}
