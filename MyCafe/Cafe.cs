using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Xna.Framework;
using MonsoonSheep.Stardew.Common;
using MyCafe.Characters;
using MyCafe.Enums;
using MyCafe.Locations.Objects;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Network;
using StardewValley.Objects;
using SUtility = StardewValley.Utility;
using MyCafe.Inventories;
using MyCafe.Data.Customers;
using MyCafe.Netcode;
using MyCafe.Data.Models;
using System.Text.RegularExpressions;
using StardewValley.Pathfinding;

namespace MyCafe;

public class Cafe
{
    internal RandomCustomerBuilder RandomCustomers = new RandomCustomerBuilder();
    internal VillagerCustomerBuilder VillgersCustomers = new VillagerCustomerBuilder();

    internal List<CustomerGroup> Groups = [];

    internal readonly Dictionary<string, VillagerCustomerData> VillagerData = new();

    internal int LastTimeCustomersArrived = 0;

    private CafeNetObject _fields
        => Game1.player.team.get_CafeNetFields().Value;

    internal bool Enabled
    {
        get => this._fields.CafeEnabled.Value;
        set => this._fields.CafeEnabled.Set(value);
    }

    internal CafeLocation? Indoor
    {
        get => this._fields.CafeIndoor.Value as CafeLocation;
        set => this._fields.CafeIndoor.Set(value);
    }

    internal GameLocation? Outdoor
    {
        get => this._fields.CafeOutdoor.Value;
        set => this._fields.CafeOutdoor.Set(value);
    }

    internal int OpeningTime
    {
        get => this._fields.OpeningTime.Value;
        set => this._fields.OpeningTime.Set(value);
    }

    internal int ClosingTime
    {
        get => this._fields.ClosingTime.Value;
        set => this._fields.ClosingTime.Set(value);
    }

    internal IList<Table> Tables
        => this._fields.NetTables as IList<Table>;

    internal MenuInventory Menu
        => this._fields.NetMenu.Value;

    internal NetStringDictionary<GeneratedSpriteData, NetRef<GeneratedSpriteData>> GeneratedSprites
        => this._fields.GeneratedSprites;

    internal ICollection<string> NpcCustomers
        => this._fields.NpcCustomers;

    internal void InitializeForHost(IModHelper helper)
    {
        this._fields.GeneratedSprites.OnValueRemoved += (id, data) => data.Dispose();
        this._fields.NetTables.OnValueAdded += table =>
            table.State.fieldChangeVisibleEvent += (_, oldValue, newValue) => this.OnTableStateChange(table, new TableStateChangedEventArgs()
            {
                OldValue = oldValue,
                NewValue = newValue
            });
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
        List<GameLocation> locations = new List<GameLocation>();

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

    internal void RemoveAllCustomers()
    {
        foreach (CustomerGroup g in this.Groups)
        {
            this.EndCustomerGroup(g);
        }
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
            Log.Debug("Table removed");
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
        Log.Trace("Placed furniture");

        if (Utility.IsChair(placed))
        {
            Furniture facingTable = location.GetFurnitureAt(placed.TileLocation + CommonHelper.DirectionIntToDirectionVector(placed.currentRotation.Value) * new Vector2(1, -1));
            if (facingTable == null
                || Utility.IsTable(facingTable) == false
                || facingTable.GetBoundingBox().Intersects(placed.boundingBox.Value))
            {
                return;
            }

            if (!this.IsRegisteredTable(facingTable, out FurnitureTable? table))
            {
                if (location.Equals(this.Outdoor) && this.IsFurnitureWithinRangeOfSignboard(facingTable, location) == false)
                    return;

                table = new FurnitureTable(facingTable, location.Name);
                this.AddTable(table);
            }

            table.AddChair(placed);
        }
        else if (Utility.IsTable(placed))
        {
            if (location.Equals(this.Outdoor) && this.IsFurnitureWithinRangeOfSignboard(placed, location) == false)
                return;


            if (!this.IsRegisteredTable(placed, out _))
            {
                FurnitureTable table = new(placed, location.Name);
                if (table.Seats.Count > 0)
                {
                    this.AddTable(table);
                }
            }
        }
    }

    private bool IsFurnitureWithinRangeOfSignboard(Furniture furniture, GameLocation location)
    {
        // Skip if the placed table is outside of the signboard's range
        Building signboard = location.buildings.First(b => b.buildingType.Value == ModKeys.CAFE_SIGNBOARD_BUILDING_ID);
        Point signboardTile = new Point(signboard.tileX.Value, signboard.tileY.Value);
        int distance = int.MaxValue;

        for (int x = (int) furniture.TileLocation.X; x <= furniture.TileLocation.X + furniture.getTilesWide(); x++)
        {
            for (int y = (int) furniture.TileLocation.Y; y <= furniture.TileLocation.Y + furniture.getTilesHigh(); y++)
            {
                distance = Math.Min(distance, (int) Vector2.Distance(new Vector2(x, y), signboardTile.ToVector2()));
            }
        }

        return distance <= Mod.Config.DistanceForSignboardToRegisterTables;
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
                {
                    this.RemoveTable(existingTable);
                }
            }
        }
        else if (Utility.IsTable(f))
        {
            if (this.IsRegisteredTable(f, out FurnitureTable? trackedTable))
            {
                this.RemoveTable(trackedTable);
            }
        }
    }

    internal void OnTableStateChange(object sender, TableStateChangedEventArgs e)
    {
        if (!Context.IsMainPlayer || e.OldValue == e.NewValue)
            return;

        Table table = (Table) sender;
        CustomerGroup? group = this.Groups.FirstOrDefault(g => g.ReservedTable == table);

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

                if (group == null)
                {
                    Log.Error("Group not found for table");
                    return;
                }

                this.EndCustomerGroup(group);
                break;
        }
    }

    internal Table? GetFreeTable(int minSeats = 1)
    {
        return this.Tables.PickRandomWhere(t => !t.IsReserved && t.Seats.Count >= minSeats);
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

    internal void TryRemoveRandomNpcData(string name)
    {
        Match findRandomGuid = new Regex($@"{ModKeys.CUSTOMER_NPC_NAME_PREFIX}Random(.*)").Match(name);
        if (findRandomGuid.Success)
        {
            Log.Trace("Deleting random customer and its generated sprite");
            string guid = findRandomGuid.Groups[1].Value;
            if (this.GeneratedSprites.Remove(guid) == false)
                Log.Trace("Tried to remove GUID for random customer but it wasn't registered.");
        }
    }

    internal void SpawnCustomers(GroupType type)
    {
        Table? table = this.GetFreeTable();
        if (table == null)
            return;

        CustomerGroup? group = type switch
        {
            GroupType.Random => this.RandomCustomers.TrySpawn(table),
            GroupType.Villager => this.VillgersCustomers.TrySpawn(table),
            _ => null
        };

        if (group == null)
            return;

        this.Groups.Add(group);
    }

  
    internal void EndCustomerGroup(CustomerGroup group)
    {
        Log.Debug($"Removing customers");
        
        group.ReservedTable?.Free();
        this.Groups.Remove(group);

        if (group.Type != GroupType.Villager)
        {
            try
            {
                group.MoveTo(
                    Game1.getLocationFromName("BusStop"),
                    new Point(33, 9),
                    (c, loc) => this.DeleteNpcFromExistence((c as NPC)!));
            }
            catch (PathNotFoundException e)
            {
                Log.Error($"Couldn't return customers to bus stop\n{e.Message}\n{e.StackTrace}");
                foreach (NPC c in group.Members)
                {
                    this.DeleteNpcFromExistence(c);
                }
            }
        }
        else
        {
            try
            {
                group.MoveTo(Game1.getLocationFromName("BusStop"), new Point(12, 23), (c, _) => this.ReturnVillagerToSchedule((c as NPC)!));
            }
            catch (PathNotFoundException e)
            {
                Log.Error($"Villager NPCs can't find path out of cafe\n{e.Message}\n{e.StackTrace}");
                foreach (NPC npc in group.Members)
                {
                    // TODO warp to their home
                }
            }
        }

    }

    internal void DeleteNpcFromExistence(NPC npc)
    {
        npc.currentLocation?.characters.Remove(npc);
        this.TryRemoveRandomNpcData(npc.Name);
    }

    internal void ReturnVillagerToSchedule(NPC npc)
    {
        this.NpcCustomers.Remove(npc.Name);
        npc.ignoreScheduleToday = false;
        npc.get_OrderItem().Set(null!);
        npc.get_IsSittingDown().Set(false);
        npc.set_Seat(null);

        List<int> activityTimes = npc.Schedule.Keys.OrderBy(i => i).ToList();
        int timeOfCurrent = activityTimes.LastOrDefault(t => t <= Game1.timeOfDay);
        int timeOfNext = activityTimes.FirstOrDefault(t => t > Game1.timeOfDay);
        int minutesSinceCurrentStarted = SUtility.CalculateMinutesBetweenTimes(timeOfCurrent, Game1.timeOfDay);
        int minutesTillNextStarts = SUtility.CalculateMinutesBetweenTimes(Game1.timeOfDay, timeOfNext);
        int timeOfActivity;

        Log.Trace($"[{Game1.timeOfDay}] Returning {npc.Name} to schedule for key \"{npc.ScheduleKey}\". Time of current activity is {timeOfCurrent}, next activity is at {timeOfNext}.");

        if (timeOfCurrent == 0) // Means it's the start of the day
        {
            timeOfActivity = activityTimes.First();
        }
        else if (timeOfNext == 0) // Means it's the end of the day
        {
            timeOfActivity = activityTimes.Last();
        }
        else
        {
            if (minutesTillNextStarts < minutesSinceCurrentStarted && minutesTillNextStarts <= 30)
                // If we're very close to the next item, 
                timeOfActivity = timeOfNext;
            else
                timeOfActivity = timeOfCurrent;
        }

        Log.Trace($"Time of selected activity is {timeOfActivity}");

        SchedulePathDescription originalPathDescription = npc.Schedule[timeOfActivity];

        Log.Trace($"Schedule description is {originalPathDescription.targetLocationName}: {originalPathDescription.targetTile}, behavior: {originalPathDescription.endOfRouteBehavior}");

        GameLocation targetLocation = Game1.getLocationFromName(originalPathDescription.targetLocationName);
        Stack<Point>? routeToScheduleItem = Pathfinding.PathfindFromLocationToLocation(
            npc.currentLocation,
            npc.TilePoint,
            targetLocation,
            originalPathDescription.targetTile,
            npc);

        if (routeToScheduleItem == null)
        {
            Log.Trace("Can't find route back");
            // TODO: Warp them to their home
            return;
        }

        // Can this return null?
        SchedulePathDescription toInsert = npc.pathfindToNextScheduleLocation(
            npc.ScheduleKey,
            npc.currentLocation.Name,
            npc.TilePoint.X,
            npc.TilePoint.Y,
            originalPathDescription.targetLocationName,
            originalPathDescription.targetTile.X,
            originalPathDescription.targetTile.Y,
            originalPathDescription.facingDirection,
            originalPathDescription.endOfRouteBehavior,
            originalPathDescription.endOfRouteMessage);

        npc.queuedSchedulePaths.Clear();
        npc.Schedule[Game1.timeOfDay] = toInsert;
        npc.checkSchedule(Game1.timeOfDay);
    }

    
    internal List<VillagerCustomerData> GetAvailableVillagerCustomers(int count)
    {
        List<VillagerCustomerData> list = [];

        foreach (KeyValuePair<string, VillagerCustomerData> data in this.VillagerData.OrderBy(_ => Game1.random.Next()))
        {
            if (list.Count == count)
                break;

            if (this.CanVillagerVisit(data.Value, Game1.timeOfDay))
                list.Add(data.Value);
        }

        return list;
    }

    private bool CanVillagerVisit(VillagerCustomerData data, int timeOfDay)
    {
        NPC npc = data.GetNpc();
        VillagerCustomerModel model = Mod.Assets.VillagerCustomerModels[data.NpcName];

        int daysSinceLastVisit = Game1.Date.TotalDays - data.LastVisitedDate.TotalDays;
        int daysAllowed = model.VisitFrequency switch
        {
            0 => 200,
            1 => 27,
            2 => 13,
            3 => 7,
            4 => 3,
            5 => 1,
            _ => 9999999
        };

        if (Mod.Cafe.NpcCustomers.Contains(data.NpcName) ||
            npc.isSleeping.Value == true ||
            npc.ScheduleKey == null ||
            daysSinceLastVisit < daysAllowed)
            return false;

        // If no busy period for today, they're free all day
        if (!model.BusyTimes.TryGetValue(npc.ScheduleKey, out List<BusyPeriod>? busyPeriods))
            return false;
        if (busyPeriods.Count == 0)
            return true;

        // Check their busy periods for their current schedule key
        foreach (BusyPeriod busyPeriod in busyPeriods)
        {
            if (SUtility.CalculateMinutesBetweenTimes(timeOfDay, busyPeriod.From) <= 120
                && SUtility.CalculateMinutesBetweenTimes(timeOfDay, busyPeriod.To) > 0)
            {
                if (!(busyPeriod.Priority <= 3 && Game1.random.Next(6 * busyPeriod.Priority) == 0) &&
                    !(busyPeriod.Priority == 4 && Game1.random.Next(50) == 0))
                {
                    return false;
                }
            }
        }

        return true;
    }
}
