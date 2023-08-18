using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FarmCafe.Framework.Characters;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using xTile.Dimensions;
using Microsoft.Xna.Framework;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Utility = FarmCafe.Framework.Utilities.Utility;
using Vector2 = Microsoft.Xna.Framework.Vector2;


namespace FarmCafe.Framework.Objects
{
    internal abstract class Table
    {
        protected Table(GameLocation location)
        {
            this.CurrentLocation = location;
        }

        internal virtual List<Seat> Seats { get; set; }

        internal virtual Vector2 Position { get; set; }

        internal GameLocation CurrentLocation { get; set; }

        internal virtual bool IsReserved { get; set; }

        internal virtual Rectangle BoundingBox { get; set;  }

        internal virtual void Free()
        {
            Seats.ForEach(s => s.Free());
            IsReserved = false;
        }

        internal virtual bool Reserve(List<Customer> customers)
        {
            if (IsReserved || Seats.Count < customers.Count)
                return false;

            for (int i = 0; i < customers.Count; i++)
            {
                customers[i].Seat = Seats[i];
                Seats[i].Reserve(customers[i]);
            }

            IsReserved = true;
            return true;
        }

        internal virtual Vector2 GetCenter() 
            => BoundingBox.Center.ToVector2();
    }

    internal sealed class FurnitureTable : Table
    {
        internal Furniture ActualTable;

        public FurnitureTable(Furniture actualTable, GameLocation location) : base(location)
        {
            this.Seats = new List<Seat>();
            this.ActualTable = actualTable;

            PopulateChairs();
        }

        internal override Vector2 Position 
            => ActualTable.TileLocation;

        internal override bool IsReserved 
            => this.ActualTable.modData.TryGetValue("FarmCafeTableIsReserved", out var val) && val == "T";

        internal override Rectangle BoundingBox 
            => this.ActualTable.boundingBox.Value;

        internal override void Free()
        {
            base.Free();
            this.ActualTable.modData["FarmCafeTableIsReserved"] = "F";
        }

        internal override bool Reserve(List<Customer> customers)
        {
            if (!base.Reserve(customers))
                return false;

            this.ActualTable.modData["FarmCafeTableIsReserved"] = "T";
            return true;
        }

        internal void PopulateChairs()
        {
            int sizeY = this.ActualTable.getTilesHigh();
            int sizeX = this.ActualTable.getTilesWide();

            for (int i = - 1; i <= sizeX ; i++)
            {
                for (int j = - 1; j <= sizeY; j++)
                {
                    if (
                            ( 
                            (i == -1 || i == sizeX) && (j == -1 || j == sizeY)
                            ) ||  // corners
                            ( 
                            !(i == -1 || i == sizeX) && !(j == -1 || j == sizeY)
                            )   // inside
                        )
                    {
                        continue;
                    }

                    Furniture chairAt = CurrentLocation.GetFurnitureAt(new Vector2(Position.X + i, Position.Y + j));
                    if (chairAt == null || !Utility.IsChair(chairAt)) 
                        continue;

                    int rotation = chairAt.currentRotation.Value;
                    bool valid = false;
                    valid = rotation switch
                    {
                        0 => (j == -1),
                        1 => (i == -1),
                        2 => (j == sizeY),
                        3 => (i == sizeX),
                        _ => valid
                    };

                    if (valid)
                    {
                        AddChair(chairAt);
                    }
                }
            }
        }

        internal FurnitureChair AddChair(Furniture chairToAdd)
        {
            if (IsReserved)
                return null;

            if (Seats.Any(c => c.Position == chairToAdd.TileLocation))
                return null;

            Debug.Log("Adding chair to table");
            var furnitureChair = new FurnitureChair(chairToAdd)
            {
                Table = this
            };
            this.Seats.Add(furnitureChair);
            return furnitureChair;
        }

        internal bool RemoveChair(Furniture chairToRemove)
        {
            if (IsReserved)
                return false;

            if (!Seats.Any(c => c.Position == chairToRemove.TileLocation))
            {
                Debug.Log("Trying to remove a chair that wasn't tracked");
                return false;
            }

            chairToRemove.modData.Remove("FarmCafeChairIsReserved");
            Seats = Seats.TakeWhile(c => c.Position != chairToRemove.TileLocation).ToList();
            Debug.Log("Removed chair from table");
            return true;
        }
    }

    internal sealed class MapTable : Table {
        public MapTable(Rectangle boundingBox, GameLocation location, List<Vector2> seatPositions) : base(location)
        {
            this.BoundingBox = boundingBox;
            this.Position = boundingBox.Center.ToVector2();

            this.Seats = new List<Seat>();
            foreach (var seat in seatPositions)
            {
                var mapSeat = new MapChair(seat)
                {
                    Table = this
                };
                this.Seats.Add(mapSeat);
            }
        }
    }
}
