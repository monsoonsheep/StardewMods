using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FarmCafe.Framework.Characters;
using FarmCafe.Locations;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using xTile.Tiles;

namespace FarmCafe.Framework.Objects
{
    public abstract class ISeat
    {
        public abstract Table Table { get; set;  }

        public abstract Vector2 Position { get; }

        public abstract Customer ReservingCustomer { get; set; }

        public abstract bool Reserve(Customer customer);

        public abstract void Free();

        public abstract bool IsReserved { get; }

        public abstract int SittingDirection { get; }
    }

    internal sealed class FurnitureChair : ISeat
    {
        internal Furniture ActualChair;

        public FurnitureChair(Furniture actualChair)
        {
            this.ActualChair = actualChair;
        }

        public override Vector2 Position => ActualChair.TileLocation;

        public override int SittingDirection => ActualChair.GetSittingDirection();

        public override Table Table { get; set; }

        public override Customer ReservingCustomer { get; set; }

        public override bool Reserve(Customer customer)
        {
            if (ReservingCustomer != null)
                return false;

            this.ActualChair.modData["FarmCafeChairIsReserved"] = "T";
            this.ReservingCustomer = customer;
            return true;
        }

        public override void Free()
        {
            this.ActualChair.modData.Remove("FarmCafeChairIsReserved");
            this.ReservingCustomer = null;
        }

        public override bool IsReserved => this.ActualChair.modData.TryGetValue("FarmCafeChairIsReserved", out var val) && val == "T";
    }

    public sealed class MapChair : ISeat
    {
        public MapChair(MapTable table, Vector2 position)
        {
            this.Table = table;
            this.Position = position;
        }

        public override Table Table { get; set;  }

        public override Vector2 Position { get; }

        public override Customer ReservingCustomer { get; set; }

        public override bool Reserve(Customer customer)
        {
            if (ReservingCustomer != null) 
                return false;
            if (Table.CurrentLocation is CafeLocation cafe)
            {
                MapSeat mapSeat = cafe.mapSeats.ToList().FirstOrDefault(s => s.tilePosition.Value.Equals(this.Position));
                mapSeat?.sittingFarmers.Add(Game1.MasterPlayer.UniqueMultiplayerID, 0);
            }
            ReservingCustomer = customer;
            return true;
        }

        public override void Free()
        {
            if (Table.CurrentLocation is CafeLocation cafe)
            {
                MapSeat mapSeat = cafe.mapSeats.ToList().FirstOrDefault(s => s.tilePosition.Value.Equals(this.Position));
                mapSeat?.RemoveSittingFarmer(Game1.MasterPlayer);
            }
            ReservingCustomer = null;
        }

        public override bool IsReserved => (ReservingCustomer != null);

        public override int SittingDirection
        {
            get 
            {
                Rectangle tableBox = (Table as MapTable).BoundingBox;
                Vector2 myPos = Position * 64;
                if (tableBox.Contains(myPos.X, myPos.Y - 64))
                    return 0;
                if (tableBox.Contains(myPos.X + 64, myPos.Y))
                    return 1;
                if (tableBox.Contains(myPos.X, myPos.Y + 64))
                    return 2;
                if (tableBox.Contains(myPos.X - 64, myPos.Y))
                    return 3;

                return 0;
            }
        }
        
    }
}
