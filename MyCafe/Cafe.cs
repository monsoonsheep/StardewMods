using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MonsoonSheep.Stardew.Common;
using MyCafe.Characters;
using MyCafe.Enums;
using MyCafe.Locations.Objects;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Inventories;
using StardewValley.Locations;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.Pathfinding;
using SUtility = StardewValley.Utility;
using MyCafe.Inventories;
using MyCafe.Data.Customers;
using System.Diagnostics.Metrics;
using MyCafe.Characters.Spawning;
using MyCafe.Netcode;

namespace MyCafe;

public class Cafe
{
    internal CafeNetObject Fields
    {
        get => Game1.player.team.get_CafeNetFields().Value;
    }

    internal RandomCustomerSpawner RandomCustomers = null!;
    internal VillagerCustomerSpawner VillagerCustomers = null!;
    internal RandomCustomerSpawner? ChatCustomers = null;

    internal int LastTimeCustomersArrived = 0;

    internal bool Enabled
    {
        get => this.Fields.CafeEnabled.Value;
        set => this.Fields.CafeEnabled.Set(value);
    }

    internal CafeLocation? Indoor
    {
        get => this.Fields.CafeIndoor.Value as CafeLocation;
        set => this.Fields.CafeIndoor.Set(value);
    }

    internal GameLocation? Outdoor
    {
        get => this.Fields.CafeOutdoor.Value;
        set => this.Fields.CafeOutdoor.Set(value);
    }

    internal IList<Table> Tables
        => this.Fields.NetTables as IList<Table>;

    internal int OpeningTime
    {
        get => this.Fields.OpeningTime.Value;
        set => this.Fields.OpeningTime.Set(value);
    }

    internal int ClosingTime
    {
        get => this.Fields.ClosingTime.Value;
        set => this.Fields.ClosingTime.Set(value);
    }

    internal MenuInventory Menu
        => this.Fields.NetMenu.Value;

    internal NetStringDictionary<GeneratedSpriteData, NetRef<GeneratedSpriteData>> GeneratedSprites
        => this.Fields.GeneratedSprites;

    internal ICollection<string> NpcCustomers
        => this.Fields.NpcCustomers;

    internal void InitializeForHost(IModHelper helper)
    {
        this.Fields.GeneratedSprites.OnValueRemoved += (id, data) => data.Dispose();
        this.Fields.NetTables.OnValueAdded += table =>
            table.State.fieldChangeVisibleEvent += (_, oldValue, newValue) => this.OnTableStateChange(table, new TableStateChangedEventArgs()
            {
                OldValue = oldValue,
                NewValue = newValue
            });

        this.RandomCustomers = new RandomCustomerSpawner(getModelFunc: Mod.CharacterFactory.CreateRandomCustomer);
        this.VillagerCustomers = new VillagerCustomerSpawner();

        this.RandomCustomers.Initialize(helper);
        this.VillagerCustomers.Initialize(helper);
    }

    internal void DayUpdate()
    {
        if (this.UpdateCafeLocations() is true)
        {
            this.Enabled = true;
            this.PopulateTables();
        }
        else
        {
            this.Enabled = false;
            this.Tables.Clear();
        }

        if (!this.Menu.Inventories.Any())
            this.Menu.AddCategory("Menu");
    }

    private void PopulateTables()
    {
        this.Tables.Clear();
        var locations = new List<GameLocation>();

        if (this.Indoor != null)
            locations.Add(this.Indoor);
        if (this.Outdoor != null)
            locations.Add(this.Outdoor);

        int count = 0;
        foreach (GameLocation location in locations)
        {
            foreach (Furniture furniture in location.furniture.Where(t => Utility.IsTable((t))))
            {
                FurnitureTable newTable = new FurnitureTable(furniture, location.Name);
                if (newTable.Seats.Count > 0)
                {
                    this.AddTable(newTable);
                    count++;
                }
            }
        }

        if (count > 0)
        {
            Log.Debug($"{count} furniture tables found in cafe locations.");
            count = 0;
        }

        // Populate Map tables for cafe indoors
        if (this.Indoor != null)
        {
            foreach (var pair in this.Indoor.GetMapTables())
            {
                Rectangle newRectangle = new Rectangle(pair.Key.X * 64, pair.Key.Y * 64, pair.Key.Width * 64, pair.Key.Height * 64);
                LocationTable locationTable = new LocationTable(newRectangle, this.Indoor.Name, pair.Value);
                if (locationTable.Seats.Count > 0)
                {
                    this.AddTable(locationTable);
                    count++;
                }
            }
            Log.Debug($"{count} map-based tables found in cafe locations.");
        }
    }

    private IEnumerable<CustomerGroup> ActiveGroups()
    {
        foreach (CustomerGroup g in this.RandomCustomers._groups)
            yield return g;
        foreach (CustomerGroup g in this.VillagerCustomers._groups)
            yield return g;

        if (this.ChatCustomers != null)
            foreach (CustomerGroup g in this.ChatCustomers._groups)
                yield return g;
    }

    internal void RemoveAllCustomers()
    {
        foreach (CustomerSpawner spawner in this.Spawners())
        {
            for (int i = spawner._groups.Count - 1; i >= 0; i--)
            {
                spawner.EndCustomers(spawner._groups[i]);
            }
        }
    }

    private IEnumerable<CustomerSpawner> Spawners()
    {
        yield return this.RandomCustomers;
        yield return this.VillagerCustomers;

        if (this.ChatCustomers != null)
            yield return this.ChatCustomers;
    }

    internal void TenMinuteUpdate()
    {
        int minutesTillCloses = SUtility.CalculateMinutesBetweenTimes(Game1.timeOfDay, this.ClosingTime);
        int minutesTillOpens = SUtility.CalculateMinutesBetweenTimes(Game1.timeOfDay, this.OpeningTime);
        int minutesSinceLastVisitors = SUtility.CalculateMinutesBetweenTimes(this.LastTimeCustomersArrived, Game1.timeOfDay);
        float percentageOfTablesFree = (float) this.Tables.Count(t => !t.IsReserved) / this.Tables.Count;

        if (minutesTillCloses <= 20)
            return;

        float prob = 0f;

        // more chance if it's been a while since last Visitors
        prob += minutesSinceLastVisitors switch
        {
            <= 20 => 0f,
            <= 30 => Game1.random.Next(5) == 0 ? 0.05f : -0.1f,
            <= 60 => Game1.random.Next(2) == 0 ? 0.1f : 0f,
            _ => 0.25f
        };

        // more chance if a higher percent of tables are free
        prob += percentageOfTablesFree switch
        {
            <= 0.2f => 0.0f,
            <= 0.5f => 0.1f,
            <= 0.8f => 0.15f,
            _ => 0.2f
        };

        // slight chance to spawn if last hour of open time
        if (minutesTillCloses <= 60)
            prob += Game1.random.Next(20 + Math.Max(0, minutesTillCloses / 3)) >= 28 ? 0.2f : -0.5f;
    }

    internal void AddTable(Table table)
    {
        table.Free();
        this.Tables.Add(table);
    }

    internal void RemoveTable(FurnitureTable table)
    {
        if (this.Tables.Remove(table))
            Log.Debug($"Table removed");
        else
            Log.Warn("Trying to remove a table that isn't registered");
    }

    internal bool InteractWithTable(Table table, Farmer who)
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

    internal bool IsRegisteredTable(Furniture furniture, [NotNullWhen(true)] out FurnitureTable? result)
    {
        foreach (Table t in this.Tables)
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
        foreach (Table t in this.Tables)
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

    internal void OnFurniturePlaced(Furniture placed, GameLocation location)
    {
        if (Utility.IsChair(placed))
        {
            Furniture facingFurniture = location.GetFurnitureAt(placed.TileLocation + CommonHelper.DirectionIntToDirectionVector(placed.currentRotation.Value) * new Vector2(1, -1));
            if (facingFurniture == null
                || Utility.IsTable(facingFurniture) == false
                || facingFurniture.GetBoundingBox().Intersects(placed.boundingBox.Value))
                return;

            if (!this.IsRegisteredTable(facingFurniture, out FurnitureTable? table))
            {
                table = new FurnitureTable(facingFurniture, location.Name);
                this.AddTable(table);
            }

            table.AddChair(placed);
        }
        else if (Utility.IsTable(placed))
        {
            if (!this.IsRegisteredTable(placed, out _))
            {
                FurnitureTable table = new(placed, location.Name);
                if (table.Seats.Count > 0)
                    this.AddTable(table);
            }
        }
    }

    internal void OnFurnitureRemoved(Furniture f, GameLocation location)
    {
        if (Utility.IsChair(f))
        {
            FurnitureTable? existingTable = this.Tables
                .OfType<FurnitureTable>()
                .FirstOrDefault(t => t.Seats.Any(seat => (seat as FurnitureSeat)?.ActualChair.Value.Equals(f) is true));

            if (existingTable != null)
            {
                existingTable.RemoveChair(f);
                if (existingTable.Seats.Count == 0)
                    this.RemoveTable(existingTable);
            }
        }
        else if (Utility.IsTable(f))
        {
            if (this.IsRegisteredTable(f, out FurnitureTable? trackedTable))
                this.RemoveTable(trackedTable);
        }
    }

    internal void OnTableStateChange(object sender, TableStateChangedEventArgs e)
    {
        if (!Context.IsMainPlayer || e.OldValue == e.NewValue)
            return;

        Table table = (Table) sender;
        CustomerGroup? group = this.ActiveGroups().FirstOrDefault(g => g.ReservedTable == table);

        if ((e.NewValue != TableState.Free && e.NewValue != TableState.CustomersComing) && group == null)
        {
            Log.Error("Table state changed, but the group reserving the table isn't in ActiveGroups");
            return;
        }

        switch (e.NewValue)
        {
            case TableState.CustomersThinkingOfOrder:
                Log.Debug("Table started");

                Game1.delayedActions.Add(new DelayedAction(2000, () =>
                    table.State.Set(TableState.CustomersDecidedOnOrder)));
                break;

            case TableState.CustomersDecidedOnOrder:
                Log.Debug("Table decided");

                break;

            case TableState.CustomersWaitingForFood:
                Log.Debug("Table waiting for order");

                foreach (NPC member in group!.Members)
                    member.get_DrawOrderItem().Set(true);
                break;

            case TableState.CustomersEating:
                Log.Debug("Table eating");

                foreach (NPC member in group!.Members)
                    member.get_DrawOrderItem().Set(false);

                Game1.delayedActions.Add(new DelayedAction(2000, () => 
                    table.State.Set(TableState.CustomersFinishedEating)));
                break;

            case TableState.CustomersFinishedEating:
                Log.Debug("Table finished meal");

                group!._spawner.EndCustomers(group);
                break;
        }
    }

    internal Table? GetFreeTable(int minSeats = 1)
    {
        return this.Tables.PickRandomWhere(t => !t.IsReserved && t.Seats.Count >= minSeats);
    }

    internal Table GetTableOfSeat(Seat seat)
    {
        Log.Warn("Should refactor this method away");
        return this.Tables.FirstOrDefault(t => t.Seats.Contains(seat))!;
    }

    internal bool UpdateCafeLocations()
    {
        if (this.Outdoor != null && this.Indoor != null && this.Outdoor.buildings.Contains(this.Indoor.GetContainingBuilding()))
            return true;
        
        bool foundIndoor = false, foundSignboard = false;
        SUtility.ForEachLocation(delegate (GameLocation loc)
        {
            foreach (Building b in loc.buildings)
            {
                if (b.GetIndoors() is CafeLocation indoor)
                {
                    foundIndoor = true;
                    this.Indoor = indoor;
                    this.Outdoor = loc;
                    Game1._locationLookup.TryAdd(indoor.Name, indoor);
                    Game1._locationLookup.TryAdd(loc.Name, loc);
                    if (b.parentLocationName.Value == "Farm")
                        Pathfinding.AddRoutesToBuildingInFarm(this.Indoor);
                    return false;
                }
            }

            return true;
        });

        if (foundIndoor == false)
        {
            SUtility.ForEachLocation(delegate (GameLocation loc)
            {
                if (loc.buildings.Any(b => b.GetData() is { DefaultAction: ModKeys.SIGNBOARD_BUILDING_CLICK_EVENT_KEY }))
                {
                    foundSignboard = true;
                    this.Outdoor = loc;
                    Game1._locationLookup.TryAdd(loc.Name, loc);
                    return false;
                }

                return true;
            });
        }

        return foundSignboard || foundIndoor;
    }
}

