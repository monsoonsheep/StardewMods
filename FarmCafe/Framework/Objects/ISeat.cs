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
    public interface ISeat
    {
        public ITable Table { get; set;  }

        public Vector2 TileLocation { get; }

        public Customer ReservingCustomer { get; set; }

        public bool Reserve(Customer customer);

        public void Free();

        public bool IsReserved();

        public int GetSittingDirection();
    }

    internal class FurnitureChair : ISeat
    {
        internal Furniture ActualChair;

        public FurnitureChair(Furniture actualChair)
        {
            this.ActualChair = actualChair;
        }

        public Vector2 TileLocation => ActualChair.TileLocation;

        public int GetSittingDirection()
        {
            return ActualChair.GetSittingDirection();
        }

        public ITable Table { get; set; }

        public Customer ReservingCustomer { get; set; }

        public bool Reserve(Customer customer)
        {
            if (ReservingCustomer != null)
                return false;

            this.ActualChair.modData["FarmCafeChairIsReserved"] = "T";
            this.ReservingCustomer = customer;
            return true;
        }

        public void Free()
        {
            this.ActualChair.modData.Remove("FarmCafeChairIsReserved");
            this.ReservingCustomer = null;
        }

        public bool IsReserved() => this.ActualChair.modData.TryGetValue("FarmCafeChairIsReserved", out var val) && val == "T";
    }

    public class MapChair : ISeat
    {
        public MapChair(MapTable table, Vector2 position)
        {
            this.Table = table;
            this.TileLocation = position;
        }

        public ITable Table { get; set;  }

        public Vector2 TileLocation { get; }

        public Customer ReservingCustomer { get; set; }

        public bool Reserve(Customer customer)
        {
            if (ReservingCustomer != null) return false;
            if (Table.CurrentLocation is CafeLocation cafe)
            {
                MapSeat mapSeat = cafe.mapSeats.ToList().FirstOrDefault(s => s.tilePosition.Value.Equals(this.TileLocation));
                if (mapSeat != null)
                {
                    mapSeat.sittingFarmers.Add(Game1.MasterPlayer.UniqueMultiplayerID, 0);
                }
            }
            ReservingCustomer = customer;
            return true;
        }

        public void Free()
        {
            if (Table.CurrentLocation is CafeLocation cafe)
            {
                MapSeat mapSeat = cafe.mapSeats.ToList().FirstOrDefault(s => s.tilePosition.Value.Equals(this.TileLocation));
                if (mapSeat != null)
                {
                    mapSeat.RemoveSittingFarmer(Game1.MasterPlayer);
                }
            }
            ReservingCustomer = null;
        }

        public bool IsReserved()
        {
            return (ReservingCustomer != null);
        }

        public int GetSittingDirection()
        {
            Rectangle tableBox = (Table as MapTable).BoundingBox;
            Vector2 myPos = TileLocation * 64;
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
