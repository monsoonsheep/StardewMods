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
using StardewValley.SpecialOrders.Objectives;
using MyCafe.Characters.Factory;
using MyCafe.Data;

#pragma warning disable IDE0060

namespace MyCafe;

public class Cafe
{
    private RandomCustomerBuilder randomCustomerBuilder = null!;

    private VillagerCustomerBuilder villagerCustomerBuilder = null!;

    private readonly List<CustomerGroup> Groups = [];

    private readonly int LastTimeCustomersArrived = 0;

    private CafeNetObject Fields
        => Game1.player.team.get_CafeNetFields().Value;

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

    internal IList<Table> Tables
        => this.Fields.NetTables as IList<Table>;

    internal FoodMenuInventory Menu
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
        this.Menu.InitializeForHost();

        this.randomCustomerBuilder = new RandomCustomerBuilder(this.GetMenuItemForCustomer);
        this.villagerCustomerBuilder = new VillagerCustomerBuilder(this.GetMenuItemForCustomer);
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
    }

    #region Tables

    private void PopulateTables()
    {
        this.Tables.Clear(); 
        List<GameLocation> locations = [];

        if (this.Indoor != null)
            locations.Add(this.Indoor);
        if (this.Outdoor != null)
            locations.Add(this.Outdoor);

        int count = 0;
        foreach (GameLocation location in locations)
        {
            foreach (Furniture furniture in location.furniture.Where(t => t.IsTable()))
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

        if (ModUtility.IsChair(placed))
        {
            Furniture facingTable = location.GetFurnitureAt(placed.TileLocation + CommonHelper.DirectionIntToDirectionVector(placed.currentRotation.Value) * new Vector2(1, -1));
            if (facingTable == null
                || facingTable.IsTable() == false
                || facingTable.GetBoundingBox().Intersects(placed.boundingBox.Value))
            {
                return;
            }

            if (!this.IsRegisteredTable(facingTable, out FurnitureTable? table))
            {
                if (location.Equals(this.Outdoor) && IsFurnitureWithinRangeOfSignboard(facingTable, location) == false)
                    return;

                table = new FurnitureTable(facingTable, location.Name);
                this.AddTable(table);
            }

            table.AddChair(placed);
        }
        else if (placed.IsTable())
        {
            if (location.Equals(this.Outdoor) && IsFurnitureWithinRangeOfSignboard(placed, location) == false)
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

    private static bool IsFurnitureWithinRangeOfSignboard(Furniture furniture, GameLocation location)
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
        if (ModUtility.IsChair(f))
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
        else if (f.IsTable())
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

                Game1.delayedActions.Add(new DelayedAction(200, () =>
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

                Game1.delayedActions.Add(new DelayedAction(200, () => 
                    table.State.Set(TableState.CustomersFinishedEating)));

                break;

            case TableState.CustomersFinishedEating:
                Log.Debug("Table finished meal");
                
                group!.PayForFood();
                this.EndCustomerGroup(group);

                break;
        }
    }

    internal Table? GetFreeTable(int minSeats = 1)
    {
        return this.Tables.PickRandomWhere(t => !t.IsReserved && t.Seats.Count >= minSeats);
    }

    #endregion

    #region Locations

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

    #endregion

    #region Customers

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

    internal void SpawnCustomers(GroupType type)
    {
        Table? table = this.GetFreeTable();
        if (table == null)
            return;

        CustomerGroup? group = type switch
        {
            GroupType.Random => this.randomCustomerBuilder.TrySpawn(table),
            GroupType.Villager => this.villagerCustomerBuilder.TrySpawn(table),
            _ => null
        };

        if (group == null)
            return;

        this.Groups.Add(group);
    }

    private Item? GetMenuItemForCustomer(NPC npc)
    {
        SortedDictionary<int, List<Item>> tastesItems = [];
        int count = 0;

        foreach (Item i in this.Menu.ItemDictionary.Values.SelectMany(i => i))
        {
            int tasteLevel;
            switch (npc.getGiftTasteForThisItem(i))
            {
                case 6:
                    tasteLevel = (int) GiftObjective.LikeLevels.Hated;
                    break;
                case 4:
                    tasteLevel = (int) GiftObjective.LikeLevels.Disliked;
                    break;
                case 8:
                    tasteLevel = (int) GiftObjective.LikeLevels.Neutral;
                    break;
                case 2:
                    tasteLevel = (int) GiftObjective.LikeLevels.Liked;
                    break;
                case 0:
                    tasteLevel = (int) GiftObjective.LikeLevels.Loved;
                    break;
                default:
                    continue;
            }

            if (!tastesItems.ContainsKey(tasteLevel))
                tastesItems[tasteLevel] = [];

            tastesItems[tasteLevel].Add(i);
            count++;
        }

        // Null if either no items on menu or the best the menu can do for the npc is less than neutral taste for them
        if (count == 0 || tastesItems.Keys.Max() <= (int)GiftObjective.LikeLevels.Disliked)
            return null;

        return tastesItems[tastesItems.Keys.Max()].PickRandom();
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
            foreach (NPC c in group.Members)
            {
                Mod.Instance.VillagerData[c.Name].LastAteFood = c.get_OrderItem().Value.QualifiedItemId;
                Mod.Instance.VillagerData[c.Name].LastVisitedDate = Game1.Date;
            }

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

    internal void ReturnVillagerToSchedule(NPC npc)
    {
        npc.ignoreScheduleToday = false;
        npc.get_OrderItem().Set(null!);
        npc.get_IsSittingDown().Set(false);
        npc.set_Seat(null);

        if (this.NpcCustomers.Remove(npc.Name) == false)
            return;
        
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

    internal void DeleteNpcFromExistence(NPC npc)
    {
        npc.currentLocation?.characters.Remove(npc);
        Match findRandomGuid = new Regex($@"{ModKeys.CUSTOMER_NPC_NAME_PREFIX}Random(.*)").Match(npc.Name);
        if (findRandomGuid.Success)
        {
            Log.Trace("Deleting random customer and its generated sprite");
            string guid = findRandomGuid.Groups[1].Value;
            if (this.GeneratedSprites.Remove(guid) == false)
                Log.Trace("Tried to remove GUID for random customer but it wasn't registered.");
        }
    }

    internal void RemoveAllCustomers()
    {
        foreach (CustomerGroup g in this.Groups)
        {
            this.EndCustomerGroup(g);
        }
    }

    #endregion
}
