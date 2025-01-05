using Netcode;
using StardewValley.Objects;


namespace StardewMods.FoodJoints.Framework.Objects;

public class FurnitureTable : Table
{
    internal readonly NetRef<Furniture> ActualTable = [];

    public FurnitureTable() : base()
    {
        this.NetFields.AddField(this.ActualTable);
    }

    internal FurnitureTable(Furniture actualTable) : this()
    {
        this.ActualTable.Set(actualTable);
        base.BoundingBox.Set(this.ActualTable.Value.boundingBox.Value);
        base.TilePosition = this.ActualTable.Value.TileLocation;
        base.Location = this.ActualTable.Value.Location.NameOrUniqueName;
        this.PopulateChairs();
    }

    internal void PopulateChairs()
    {
        int sizeY = this.ActualTable.Value.getTilesHigh();
        int sizeX = this.ActualTable.Value.getTilesWide();

        for (int i = -1; i <= sizeX; i++)
        {
            for (int j = -1; j <= sizeY; j++)
            {
                if (

                        (i == -1 || i == sizeX) && (j == -1 || j == sizeY)
                         ||  // corners

                        !(i == -1 || i == sizeX) && !(j == -1 || j == sizeY)
                    // inside
                    )
                {
                    continue;
                }

                GameLocation? location = CommonHelper.GetLocation(this.Location);
                Furniture? chairAt = location?.GetFurnitureAt(new Vector2(this.TilePosition.X + i, this.TilePosition.Y + j));
                if (chairAt == null || !ModUtility.IsChair(chairAt))
                    continue;

                int rotation = chairAt.currentRotation.Value;
                bool valid = rotation switch
                {
                    0 => j == -1,
                    1 => i == -1,
                    2 => j == sizeY,
                    3 => i == sizeX,
                    _ => false
                };

                if (valid)
                {
                    this.AddChair(chairAt);
                }
            }
        }
    }

    internal FurnitureSeat? AddChair(Furniture chairToAdd)
    {
        if (this.Seats.Any(c => (c as FurnitureSeat)?.ActualChair.Value.Equals(chairToAdd) is true))
        {
            //Log.Error("That chair has already been added.");
            return null;
        }

        //Log.Debug($"Adding chair {chairToAdd.TileLocation}");
        FurnitureSeat furnitureChair = new FurnitureSeat(chairToAdd);
        this.Seats.Add(furnitureChair);
        return furnitureChair;
    }

    internal bool RemoveChair(Furniture chairToRemove)
    {
        if (this.IsReserved)
            //Log.Error("That chair was reserved!");

        for (int i = this.Seats.Count - 1; i >= 0; i--)
        {
            if ((this.Seats[i] as FurnitureSeat)?.ActualChair.Value.Equals(chairToRemove) is true)
            {
                this.Seats.RemoveAt(i);
                //Log.Debug($"Removed chair at {chairToRemove.TileLocation}");
                return true;;
            }
        }

        return false;
    }
}
