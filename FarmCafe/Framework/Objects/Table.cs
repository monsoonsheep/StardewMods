using System.Collections.Generic;
using System.Linq;
using FarmCafe.Framework.Characters;
using StardewValley;
using StardewValley.Objects;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Vector2 = Microsoft.Xna.Framework.Vector2;


namespace FarmCafe.Framework.Objects
{
    internal abstract class Table
    {
        internal bool IsReadyToOrder;

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
            IsReadyToOrder = false;
            Seats.ForEach(s => s.Free());
            IsReserved = false;
        }

        internal virtual bool Reserve(List<Visitor> Visitors)
        {
            if (IsReserved || Seats.Count < Visitors.Count)
                return false;

            for (int i = 0; i < Visitors.Count; i++)
            {
                Visitors[i].Seat = Seats[i];
                Seats[i].Reserve(Visitors[i]);
            }

            IsReserved = true;
            return true;
        }

        internal virtual Vector2 GetCenter() 
            => BoundingBox.Center.ToVector2();
    }

    internal class FurnitureTable : Table
    {
        internal Furniture ActualTable;

        public FurnitureTable(Furniture actualTable, GameLocation location) : base(location)
        {
            base.Seats = new List<Seat>();
            this.ActualTable = actualTable;

            PopulateChairs();
        }

        internal override Vector2 Position 
            => ActualTable.TileLocation;

        internal override bool IsReserved 
            => this.ActualTable.modData.TryGetValue(ModKeys.MODDATA_TABLERESERVED, out var val) && val == "T";

        internal override Rectangle BoundingBox 
            => this.ActualTable.boundingBox.Value;

        internal override void Free()
        {
            base.Free();
            this.ActualTable.modData[ModKeys.MODDATA_TABLERESERVED] = "F";
        }

        internal override bool Reserve(List<Visitor> Visitors)
        {
            if (!base.Reserve(Visitors))
                return false;

            this.ActualTable.modData[ModKeys.MODDATA_TABLERESERVED] = "T";
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

            Logger.Log("Adding chair to table");
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
                Logger.Log("Trying to remove a chair that wasn't tracked");
                return false;
            }

            chairToRemove.modData.Remove(ModKeys.MODDATA_CHAIRRESERVED);
            Seats = Seats.TakeWhile(c => c.Position != chairToRemove.TileLocation).ToList();
            Logger.Log("Removed chair from table");
            return true;
        }
    }

    internal class MapTable : Table {
        public MapTable(Rectangle boundingBox, GameLocation location, List<Vector2> seatPositions) : base(location)
        {
            base.BoundingBox = boundingBox;
            base.Position = boundingBox.Center.ToVector2();

            base.Seats = new List<Seat>();
            foreach (var seat in seatPositions)
            {
                var mapSeat = new MapChair(seat)
                {
                    Table = this
                };
                base.Seats.Add(mapSeat);
            }
        }
    }
}
