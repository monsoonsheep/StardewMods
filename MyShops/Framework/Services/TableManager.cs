using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewMods.MyShops.Framework.Characters;
using StardewMods.MyShops.Framework.Data;
using StardewMods.MyShops.Framework.Enums;
using StardewMods.MyShops.Framework.Game;
using StardewMods.MyShops.Framework.Objects;
using StardewValley.Objects;
using xTile.Layers;
using xTile.Tiles;

namespace StardewMods.MyShops.Framework.Services;
internal class TableManager : Service
{
    private static TableManager instance = null!;

    private readonly NetState netState;

    public TableManager(
        Harmony harmony,
        NetState netState,
        IModEvents events,
        ILogger logger,
        IManifest manifest
        ) : base(logger, manifest)
    {
        instance = this;

        this.netState = netState;


        
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        NetStateObject newState = this.netState.fields;

        newState.Tables.OnValueAdded += table =>
            table.State.fieldChangeVisibleEvent += (_, oldValue, newValue) => this.OnTableStateChange(table, new TableStateChangedEventArgs()
            {
                OldValue = oldValue,
                NewValue = newValue
            });
    }

    private static void After_FurnitureHasSittingFarmers(Furniture __instance, ref bool __result)
    {
        if (instance.IsRegisteredChair(__instance, out FurnitureSeat? seat) && seat.IsReserved)
        {
            __result = true;
        }
    }

    /// <summary>
    /// To avoid putting furniture on top of tables that are registered (probably? I forgor)
    /// </summary>
    private static void After_GetAdditionalFurniturePlacementStatus(Furniture __instance, GameLocation location, int x, int y, Farmer who, ref int __result)
    {
        if (__instance.IsTable())
        {
            Furniture table = location.GetFurnitureAt(new Vector2(x, y));
            if (instance.IsRegisteredTable(table, out FurnitureTable? trackedTable) && trackedTable.IsReserved)
                __result = 2;
        }
    }

    /// <summary>
    /// Prevent farmers from sitting in chairs that are reserved for customers
    /// </summary>
    private static bool Before_AddSittingFarmer(Furniture __instance, Farmer who, ref Vector2? __result)
    {
        if (ModUtility.IsChair(__instance)
            && instance.IsRegisteredChair(__instance, out FurnitureSeat? chair) && chair.IsReserved)
        {
            instance.Log.Warn("Can't sit in this chair, it's reserved");
            __result = null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    private static bool Before_PerformObjectDropInAction(Furniture __instance, Item dropInItem, bool probe, Farmer who, ref bool __result)
    {
        if (__instance.IsTable())
        {
            if (instance.IsRegisteredTable(__instance, out FurnitureTable? trackedTable)
                && trackedTable.IsReserved)
            {
                instance.Log.Warn("Can't drop in this object onto this table. It's reserved");
                __result = false;
                return false;
            }
        }

        return true;
    }

    private static void After_CanBeRemoved(Furniture __instance, Farmer who, ref bool __result)
    {
        if (__result is false)
            return;

        if (__instance.IsTable())
        {
            if (instance.IsRegisteredTable(__instance, out FurnitureTable? trackedTable) && trackedTable.IsReserved)
            {
                Game1.addHUDMessage(new HUDMessage("Can't remove this furniture", 1000, fadeIn: false));
                __result = false;
            }
        }
    }

    internal void PopulateTables()
    {
        this.netState.Tables.Clear();

        if (this.Signboard?.Location == null)
            return;

        int count = 0;

        // Furniture tables
        foreach (Furniture f in this.GetValidFurnitureInCafeLocations())
        {
            FurnitureTable newTable = new FurnitureTable(f);
            if (newTable.Seats.Count > 0)
            {
                this.AddTable(newTable);
                count++;
            }
        }

        this.Log.Debug($"{count} furniture tables found in cafe locations.");

        count = 0;

        // Map tables
        foreach (var pair in this.GetMapTables(this.Signboard.Location))
        {
            Rectangle newRectangle = new Rectangle(pair.Key.X * 64, pair.Key.Y * 64, pair.Key.Width * 64, pair.Key.Height * 64);
            LocationTable locationTable = new LocationTable(newRectangle, this.Signboard.Location.Name, pair.Value);
            if (locationTable.Seats.Count > 0)
            {
                this.AddTable(locationTable);
                count++;
            }
        }

        this.Log.Debug($"{count} map-based tables found in cafe locations.");
    }

    private Dictionary<Rectangle, List<Vector2>> GetMapTables(GameLocation cafeLocation)
    {
        Dictionary<Rectangle, List<Vector2>> mapTables = [];

        Layer layer = cafeLocation.Map.GetLayer("Back");

        Dictionary<string, Rectangle> tableRectangles = new();

        for (int i = 0; i < layer.LayerWidth; i++)
        {
            for (int j = 0; j < layer.LayerHeight; j++)
            {
                Tile tile = layer.Tiles[i, j];
                if (tile == null)
                    continue;

                if (tile.TileIndexProperties.TryGetValue(Values.MAPPROPERTY_TABLE, out var val)
                    || tile.Properties.TryGetValue(Values.MAPPROPERTY_TABLE, out val))
                {
                    Rectangle thisTile = new Rectangle(i, j, 1, 1);

                    tableRectangles[val] = tableRectangles.TryGetValue(val, out var existingTileKey)
                        ? Rectangle.Union(thisTile, existingTileKey)
                        : thisTile;
                }
            }
        }

        foreach (var rect in tableRectangles)
        {
            string[] splitValues = rect.Key.Split(' ');
            var seats = new List<Vector2>();

            for (int i = 0; i < splitValues.Length; i += 2)
            {
                if (i + 1 >= splitValues.Length || !float.TryParse(splitValues[i], out float x) || !float.TryParse(splitValues[i + 1], out float y))
                {
                    Console.WriteLine($"Invalid values in Cafe Map's seats at {rect.Value.X}, {rect.Value.Y}", LogLevel.Warn);
                    return [];
                }

                Vector2 seatLocation = new(x, y);
                seats.Add(seatLocation);
            }

            if (seats.Count > 0)
            {
                mapTables.Add(rect.Value, seats);
            }
        }

        return mapTables;
    }

    internal void AddTable(Table table)
    {
        table.Free();
        this.netState.Tables.Add(table);
    }

    internal void RemoveTable(FurnitureTable table)
    {
        if (this.netState.Tables.Remove(table))
            this.Log.Debug("Table removed");
        else
            this.Log.Warn("Trying to remove a table that isn't registered");
    }

    internal static bool InteractWithTable(Table table, Farmer who)
    {
        switch (table.State.Value)
        {
            case TableState.CustomersDecidedOnOrder:
                table.State.Set(TableState.CustomersWaitingForFood);
                return true;
            case TableState.CustomersWaitingForFood:
                List<string?> itemsNeeded = table.Seats
                    .Where(s => s.ReservingCustomer != null)
                    .Select(c => c.ReservingCustomer!.get_OrderItem().Value?.ItemId)
                    .ToList();

                foreach (string? item in itemsNeeded)
                    if (!string.IsNullOrEmpty(item) && !who.Items.ContainsId(item, minimum: itemsNeeded.Count(x => x == item)))
                        return false;

                foreach (string? item in itemsNeeded)
                    who.removeFirstOfThisItemFromInventory(item);

                table.State.Set(TableState.CustomersEating);
                return true;
            default:
                return false;
        }
    }

    internal void OnTableStateChange(object sender, TableStateChangedEventArgs e)
    {
        if (!Context.IsMainPlayer || e.OldValue == e.NewValue)
            return;

        Table table = (Table)sender;
        CustomerGroup? group = this.Groups.FirstOrDefault(g => g.ReservedTable == table);

        if ((e.NewValue != TableState.Free && e.NewValue != TableState.CustomersComing) && group == null)
        {
            this.Log.Error("Table state changed, but the group reserving the table isn't in ActiveGroups");
            return;
        }

        switch (e.NewValue)
        {
            case TableState.CustomersThinkingOfOrder:
                this.Log.Debug("Table started");

                Game1.delayedActions.Add(new DelayedAction(200, () =>
                    table.State.Set(TableState.CustomersDecidedOnOrder)));

                break;

            case TableState.CustomersDecidedOnOrder:
                this.Log.Debug("Table decided");

                break;

            case TableState.CustomersWaitingForFood:
                this.Log.Debug("Table waiting for order");

                foreach (NPC member in group!.Members)
                    member.get_DrawOrderItem().Set(true);

                break;

            case TableState.CustomersEating:
                this.Log.Debug("Table eating");

                foreach (NPC member in group!.Members)
                    member.get_DrawOrderItem().Set(false);

                Game1.delayedActions.Add(new DelayedAction(200, () =>
                    table.State.Set(TableState.CustomersFinishedEating)));

                break;

            case TableState.CustomersFinishedEating:
                this.Log.Debug("Table finished meal");

                int money = group!.Members.Sum(m => m.get_OrderItem().Value?.salePrice() ?? 0);
                Game1.MasterPlayer.Money += money;
                this.MoneyForToday += money;
                ModUtility.DoEmojiSprite(table.Center, EmojiSprite.Money);
                Game1.stats.Increment(Values.STATS_MONEY_FROM_CAFE, money);

                this.EndCustomerGroup(group);

                break;
        }
    }

    internal bool IsRegisteredTable(Furniture furniture, [NotNullWhen(true)] out FurnitureTable? result)
    {
        foreach (Table t in this.netState.Tables)
        {
            if (t is FurnitureTable ft && ft.ActualTable.Value == furniture)
            {
                result = ft;
                return true;
            }
        }

        result = null;
        return false;
    }

    internal bool IsRegisteredChair(Furniture furniture, [NotNullWhen(true)] out FurnitureSeat? result)
    {
        foreach (Table t in this.netState.Tables)
        {
            if (t is FurnitureTable ft)
            {
                foreach (FurnitureSeat fs in ft.Seats.Cast<FurnitureSeat>())
                {
                    if (fs.ActualChair.Value == furniture)
                    {
                        result = fs;
                        return true;
                    }
                }
            }
        }

        result = null;
        return false;
    }

    internal bool IsFurnitureWithinRangeOfSignboard(Furniture furniture)
    {
        if (this.Signboard?.Location == null)
            return false;

        // Skip if the placed table is outside of the signboard's range
        if (!furniture.Location.Equals(this.Signboard.Location))
            return false;

        Point signboardTile = new Point((int)this.Signboard.TileLocation.X, (int)this.Signboard.TileLocation.Y);
        int distance = int.MaxValue;

        for (int x = (int)furniture.TileLocation.X; x <= furniture.TileLocation.X + furniture.getTilesWide(); x++)
        {
            for (int y = (int)furniture.TileLocation.Y; y <= furniture.TileLocation.Y + furniture.getTilesHigh(); y++)
            {
                distance = Math.Min(distance, (int)Vector2.Distance(new Vector2(x, y), signboardTile.ToVector2()));
            }
        }

        return distance <= Mod.Config.DistanceForSignboardToRegisterTables;
    }

    internal void OnFurniturePlaced(Furniture placed, GameLocation location)
    {
        this.Log.Trace($"Furniture placed at {placed.TileLocation}");

        if (ModUtility.IsChair(placed))
        {
            Furniture facingTable = location.GetFurnitureAt(placed.TileLocation + CommonHelper.DirectionIntToDirectionVector(placed.currentRotation.Value) * new Vector2(1, -1));

            if (facingTable == null
                || facingTable.IsTable() == false
                || facingTable.GetBoundingBox().Intersects(placed.boundingBox.Value)
                || !this.IsFurnitureWithinRangeOfSignboard(facingTable))
            {
                return;
            }

            if (!this.IsRegisteredTable(facingTable, out FurnitureTable? table))
            {
                table = new FurnitureTable(facingTable);
                this.AddTable(table);
            }
            else
            {
                table.AddChair(placed);
            }
        }
        else if (placed.IsTable())
        {
            if (this.IsFurnitureWithinRangeOfSignboard(placed) && !this.IsRegisteredTable(placed, out _))
            {
                FurnitureTable table = new(placed);
                if (table.Seats.Count > 0)
                {
                    this.AddTable(table);
                }
            }
        }
    }

    internal void OnFurnitureRemoved(Furniture removed, GameLocation location)
    {
        this.Log.Trace($"Furniture removed from {removed.TileLocation}");

        if (ModUtility.IsChair(removed))
        {
            FurnitureTable? existingTable = this.netState.Tables
                .OfType<FurnitureTable>()
                .FirstOrDefault(t => t.Seats.Any(seat => (seat as FurnitureSeat)?.ActualChair.Value.Equals(removed) is true));

            if (existingTable != null)
            {
                existingTable.RemoveChair(removed);
                if (existingTable.Seats.Count == 0)
                {
                    this.RemoveTable(existingTable);
                }
            }
        }
        else if (removed.IsTable())
        {
            if (this.IsRegisteredTable(removed, out FurnitureTable? trackedTable))
            {
                this.RemoveTable(trackedTable);
            }
        }
    }

    internal Table? GetFreeTable(int minSeats = 1)
    {
        return this.netState.Tables.PickRandomWhere(t => !t.IsReserved && t.Seats.Count >= minSeats && (t is not FurnitureTable ft || ft.Seats.Cast<FurnitureSeat>().All(s => !s.ActualChair.Value.HasSittingFarmers())));
    }

    internal Table? GetTableFromCustomer(NPC npc)
    {
        return this.netState.Tables.FirstOrDefault(t => t.Seats.Any(s => s.ReservingCustomer?.Equals(npc) ?? false));
    }

    internal Table? GetTableAt(GameLocation location, Point tile)
    {
        return this.netState.Tables.FirstOrDefault(table => table.Location == location.Name && table.BoundingBox.Value.Contains(tile.X * 64, tile.Y * 64));
    }

    internal IEnumerable<Furniture> GetValidFurnitureInCafeLocations()
    {
        if (this.Signboard?.Location is { } signboardLocation)
        {
            foreach (Furniture furniture in signboardLocation.furniture.Where(t => t.IsTable()))
            {
                if (signboardLocation.IsOutdoors && !this.IsFurnitureWithinRangeOfSignboard(furniture))
                    continue;

                yield return furniture;
            }
        }
    }
}
