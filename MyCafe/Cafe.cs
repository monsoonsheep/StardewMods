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
using SObject = StardewValley.Object;
using MyCafe.Inventories;
using MyCafe.Data.Customers;
using MyCafe.Netcode;
using MyCafe.Data.Models;
using System.Text.RegularExpressions;
using StardewValley.Pathfinding;
using StardewValley.SpecialOrders.Objectives;
using MyCafe.Characters.Factory;
using MyCafe.Data;
using xTile.Layers;
using xTile.Tiles;
using Microsoft.Xna.Framework.Graphics;
using xTile.Dimensions;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using MyCafe.Game;
using MyCafe.UI;

#pragma warning disable IDE0060

namespace MyCafe;

public class Cafe
{
    private RandomCustomerBuilder randomCustomerBuilder = null!;

    internal VillagerCustomerBuilder villagerCustomerBuilder = null!;

    internal readonly List<CustomerGroup> Groups = [];

    private int LastTimeCustomersArrived;

    private int MoneyForToday;

    internal int OpeningTime;

    internal int ClosingTime;

    private CafeNetObject Fields
        => Game1.player.team.get_CafeNetFields().Value;

    internal byte Enabled
    {
        get => this.Fields.CafeEnabled.Value;
        set => this.Fields.CafeEnabled.Set(value);
    }

    internal SObject? Signboard
    {
        get => this.Fields.Signboard.Value;
        set => this.Fields.Signboard.Set(value);
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
        this.randomCustomerBuilder = new RandomCustomerBuilder();
        this.villagerCustomerBuilder = new VillagerCustomerBuilder();
    }

    internal void DayUpdate()
    {
        this.LastTimeCustomersArrived = 0;
        this.Groups.Clear();
        this.MoneyForToday = 0;
        this.UpdateLocations();
    }

    internal void TenMinuteUpdate()
    {
        if (this.Enabled == 0)
            return;

        if (Game1.timeOfDay >= this.OpeningTime && Game1.timeOfDay <= this.ClosingTime)
            this.Enabled = 2;
        else
            this.Enabled = 1;

        // Customers waiting for too long are forced to leave
        for (int i = this.Groups.Count - 1; i >= 0; i--)
        {
            this.Groups[i].MinutesSitting += 10;
            Table? table = this.Groups[i].ReservedTable;
            if (table is { State.Value: not TableState.CustomersEating } && this.Groups[i].MinutesSitting > Mod.Config.MinutesBeforeCustomersLeave)
            {
                this.EndCustomerGroup(this.Groups[i]);
            }
        }

        // Update unbreakability of signboard when the cafe opens and closes
        if (this.Signboard != null)
        {
            // Fragility 2 is unbreakable, we set it when cafe is operating
            this.Signboard.Fragility = this.Enabled == 2 ? 2 : 0;
        }

        if (this.Enabled == 2)
        {
            if (Game1.activeClickableMenu is CafeMenu cafeMenu)
                cafeMenu.Locked = true;

            // If cafe open, try spawn customers
            this.CustomerSpawningUpdate();
        }
        else
        {
            if (Game1.activeClickableMenu is CafeMenu cafeMenu)
                cafeMenu.Locked = false;
        }
    }

    #region Tables

    private void PopulateTables()
    {
        this.Tables.Clear(); 

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

        Log.Debug($"{count} furniture tables found in cafe locations.");

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

        Log.Debug($"{count} map-based tables found in cafe locations.");
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

                if (tile.TileIndexProperties.TryGetValue(ModKeys.MAPPROPERTY_TABLE, out var val)
                    || tile.Properties.TryGetValue(ModKeys.MAPPROPERTY_TABLE, out val))
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

                int money = group!.Members.Sum(m => m.get_OrderItem().Value?.salePrice() ?? 0);
                Game1.MasterPlayer.Money += money;
                this.MoneyForToday += money;
                ModUtility.DoEmojiSprite(table.Center, EmojiSprite.Money);
                Game1.stats.Increment(ModKeys.STATS_MONEY_FROM_CAFE, money);

                this.EndCustomerGroup(group);

                break;
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

    internal bool IsFurnitureWithinRangeOfSignboard(Furniture furniture)
    {
        if (this.Signboard?.Location == null)
            return false;

        // Skip if the placed table is outside of the signboard's range
        if (!furniture.Location.Equals(this.Signboard.Location))
            return false;

        Point signboardTile = new Point((int) this.Signboard.TileLocation.X, (int) this.Signboard.TileLocation.Y);
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

    internal void OnFurniturePlaced(Furniture placed, GameLocation location)
    {
        Log.Trace($"Furniture placed at {placed.TileLocation}");

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
        Log.Trace($"Furniture removed from {removed.TileLocation}");

        if (ModUtility.IsChair(removed))
        {
            FurnitureTable? existingTable = this.Tables
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
        return this.Tables.PickRandomWhere(t => !t.IsReserved && t.Seats.Count >= minSeats && (t is not FurnitureTable ft || ft.Seats.Cast<FurnitureSeat>().All(s => !s.ActualChair.Value.HasSittingFarmers())));
    }

    internal Table? GetTableFromCustomer(NPC npc)
    {
        return this.Tables.FirstOrDefault(t => t.Seats.Any(s => s.ReservingCustomer?.Equals(npc) ?? false));
    }

    internal Table? GetTableAt(GameLocation location, Point tile)
    {
        return this.Tables.FirstOrDefault(table => table.Location == location.Name && table.BoundingBox.Value.Contains(tile.X * 64, tile.Y * 64));
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

    #endregion

    #region Locations

    internal void UpdateLocations()
    {
        if ((this.Signboard = GetSignboardObject()) != null)
        {
            if (this.Signboard?.Location.GetContainingBuilding()?.parentLocationName?.Value == "Farm")
                Pathfinding.AddRoutesToBuildingInFarm(this.Signboard.Location);

            this.Enabled = 1;
            this.PopulateTables();
        }
        else
        {
            this.Enabled = 0;
            this.Tables.Clear();
        }
    }

    internal void OnPlacedDownSignboard(SObject signboard)
    {
        if (this.Signboard != null)
        {
            Log.Error($"There is already a signboard registered in {this.Signboard.Location.DisplayName}");
            return;
        }

        if (this.Enabled != 2)
        {
            this.UpdateLocations();
        }
    }

    internal void OnRemovedSignboard(SObject signboard)
    {
        if (this.Signboard != null)
        {
            if (this.Enabled == 2)
            {
                Log.Warn("Player broke the signboard while the cafe was open");
            }

            Game1.delayedActions.Add(new DelayedAction(500, () =>
            {
                this.UpdateLocations();
                this.RemoveAllCustomers();
            }));
        }
    }

    internal static SObject? GetSignboardObject()
    {
        SObject? found = null;

        SUtility.ForEachLocation(delegate(GameLocation loc)
        {
            foreach (SObject obj in loc.Objects.Values)
            {
                if (obj.QualifiedItemId.Equals($"(BC){ModKeys.CAFE_SIGNBOARD_OBJECT_ID}"))
                {
                    found = obj;
                    return false;
                }
            }

            return true;
        });

        return found;
    }

    #endregion

    #region Customers

    internal void CustomerSpawningUpdate()
    {
        // Choose between villager spawn and non-villager spawn
        float weightForVillagers = Mod.Config.EnableNpcCustomers / 5f;
        float weightForRandom = Math.Max(Mod.Config.EnableCustomCustomers, Mod.Config.EnableRandomlyGeneratedCustomers) / 5f;

        float prob = this.ProbabilityToSpawnCustomers(Math.Max(weightForVillagers, weightForRandom));

        #if DEBUG
        prob += Debug.ExtraProbabilityToSpawn;
        #endif

        Log.Trace($"(chance was {prob})");

        // Try chance
        float random;

        while ((random = Game1.random.NextSingle()) <= prob)
        {
            if ((random * (weightForRandom + weightForVillagers)) < weightForVillagers)
                this.SpawnVillagerCustomers();
            else
                this.SpawnRandomCustomers();

            prob -= 0.25f;
        }
    }

    private float ProbabilityToSpawnCustomers(float baseProb)
    {
        int totalTimeIntervalsDuringDay = (this.ClosingTime - this.OpeningTime) / 10;
        int minutesTillCloses = SUtility.CalculateMinutesBetweenTimes(Game1.timeOfDay, this.ClosingTime);
        int minutesSinceLastVisitors = SUtility.CalculateMinutesBetweenTimes(this.LastTimeCustomersArrived, Game1.timeOfDay);
        if (minutesTillCloses <= 20)
            return -10.00f;

        float prob = 5f / (float) Math.Pow(Math.Max(totalTimeIntervalsDuringDay - 55, 0), 0.5) * 0.30f;

        // more chance if it's been a while since last Visitors
        // TODO maybe mult this at the end
        prob += minutesSinceLastVisitors switch
        {
            <= 30 => -0.1f,
            <= 60 => Game1.random.Next(5) == 0 ? 0.05f : -0.10f,
            <= 100 => Game1.random.Next(2) == 0 ? -0.08f : 0.08f,
            <= 120 => 0.2f,
            >= 130 => 0.4f,
            _ => 0.05f
        };

        // slight chance to spawn if last hour of open time
        if (minutesTillCloses <= 60)
            prob += Game1.random.Next(20 + Math.Max(0, minutesTillCloses / 3)) >= 28
                ? 0.10f
                : -0.20f;


        prob *= ((float)Math.Pow(baseProb, 0.5) * 1.50f);

        prob += (Game1.random.NextSingle() * 0.1f);

#if DEBUG
        prob += Debug.ExtraProbabilityToSpawn;
#endif

        return prob;
    }

    internal void SpawnRandomCustomers()
    {
        Table? table = this.GetFreeTable();
        if (table != null)
        {
            CustomerGroup? group = this.randomCustomerBuilder.TrySpawn(table);
            if (group != null)
                this.AddGroup(group);
        }
    }

    internal void SpawnVillagerCustomers()
    {
        Table? table = this.GetFreeTable();
        if (table != null)
        {
            CustomerGroup? group = this.villagerCustomerBuilder.TrySpawn(table);
            if (group != null)
                this.AddGroup(group);
        }
    }

    internal void RequestNpcCustomer(string name)
    {
        Table? table = this.GetFreeTable();

        if (table != null)
        {
            VillagerCustomerData data = Mod.Instance.VillagerData[name];
            NPC npc = data.GetNpc();

            CustomerGroup group = new CustomerGroup(GroupType.Villager);
            group.AddMember(npc);

            if (this.villagerCustomerBuilder.TrySpawn(table, group) != null)
                this.AddGroup(group);
        }
    }

    internal void AddGroup(CustomerGroup group)
    {
        this.Groups.Add(group);
        this.LastTimeCustomersArrived = Game1.timeOfDay;
    }

    internal Item? GetMenuItemForCustomer(NPC npc)
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

        group.ReservedTable!.Free();
        this.Groups.Remove(group);

        if (group.Type != GroupType.Villager)
        {
            foreach (NPC c in group.Members)
            {
                if (Mod.Instance.CustomerData.TryGetValue(c.Name, out CustomerData? data))
                {
                    data.LastVisitedData = Game1.Date;
                }
            }

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
                c.faceTowardFarmerTimer = 0;
                c.faceTowardFarmer = false;
                c.movementPause = 0;

            }

            try
            {
                GameLocation tableLocation = Game1.getLocationFromName(group.ReservedTable!.Location);
                if (tableLocation is Farm || tableLocation.GetContainingBuilding()?.parentLocationName.Value.Equals("Farm") is true)
                {
                    group.MoveTo(
                        Game1.getLocationFromName("BusStop"),
                        new Point(12, 23),
                        (c, _) => this.ReturnVillagerToSchedule((c as NPC)!));
                }
                else
                {
                    foreach (NPC c in group.Members)
                        this.ReturnVillagerToSchedule(c);
                }
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
        npc.set_Seat(null);

        List<int> activityTimes = npc.Schedule.Keys.OrderBy(i => i).ToList();
        int timeOfCurrent = activityTimes.LastOrDefault(t => t <= Game1.timeOfDay);
        int timeOfNext = activityTimes.FirstOrDefault(t => t > Game1.timeOfDay);
        int minutesSinceCurrentStarted = SUtility.CalculateMinutesBetweenTimes(timeOfCurrent, Game1.timeOfDay);
        int minutesTillNextStarts = SUtility.CalculateMinutesBetweenTimes(Game1.timeOfDay, timeOfNext);
        int timeOfActivity;

        if (timeOfCurrent == 0) // Means it's the start of the day
            timeOfActivity = activityTimes.First();
        else if (timeOfNext == 0) // Means it's the end of the day
            timeOfActivity = activityTimes.Last();
        else
        {
            // If we're very close to the next item, 
            if (minutesTillNextStarts < minutesSinceCurrentStarted && minutesTillNextStarts <= 30)
                timeOfActivity = timeOfNext;
            else
                timeOfActivity = timeOfCurrent;
        }

        SchedulePathDescription originalPathDescription = npc.Schedule[timeOfActivity];

        Log.Trace($"[{Game1.timeOfDay}] Returning {npc.Name} to schedule for key {npc.ScheduleKey}. Time of current activity is {timeOfCurrent}, next activity is at {timeOfNext}.\n" +
                  $"Choosing {timeOfActivity}.\n" +
                  $"Schedule description is {originalPathDescription.targetLocationName}: {originalPathDescription.targetTile}, behavior: {originalPathDescription.endOfRouteBehavior}");

        SchedulePathDescription? route;
        try
        {
            if (npc.currentLocation.Equals(this.Signboard?.Location))
            {
                npc.PathTo(Game1.getLocationFromName(originalPathDescription.targetLocationName), originalPathDescription.targetTile,
                    originalPathDescription.facingDirection);
            
                route = new SchedulePathDescription(npc.controller.pathToEndPoint, originalPathDescription.facingDirection,
                    originalPathDescription.endOfRouteBehavior, originalPathDescription.endOfRouteMessage,
                    originalPathDescription.targetLocationName, originalPathDescription.targetTile)
                {
                    time = Game1.timeOfDay
                };
                npc.controller = null;
                npc.set_AfterLerp((c) => Mod.Cafe.NpcCustomers.Remove(c.Name));
            }
            else
            {
                route = npc.pathfindToNextScheduleLocation(npc.ScheduleKey, npc.currentLocation.Name, npc.TilePoint.X, npc.TilePoint.Y,
                    originalPathDescription.targetLocationName,
                    originalPathDescription.targetTile.X, originalPathDescription.targetTile.Y, originalPathDescription.facingDirection,
                    originalPathDescription.endOfRouteBehavior,
                    originalPathDescription.endOfRouteMessage);
                this.NpcCustomers.Remove(npc.Name);
            }
        }
        catch (PathNotFoundException e)
        {
            Log.Error($"{e.Message}\n{e.StackTrace}");
            route = null;
        }
        
        if (route == null)
        {
            this.NpcCustomers.Remove(npc.Name);
            Log.Error("Couldn't return NPC to schedule");
            // TODO warp them
            return;
        }

        npc.Schedule.Remove(timeOfActivity);
        npc.lastAttemptedSchedule = Game1.timeOfDay - 10;
        npc.queuedSchedulePaths.Clear();
        npc.Schedule[Game1.timeOfDay] = route;
        npc.checkSchedule(Game1.timeOfDay);

        if (npc.controller == null)
        {
            Log.Error("checkSchedule didn't set the controller");
            // TODO WARP
        }
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
        for (int i = this.Groups.Count - 1; i >= 0; i--)
        {
            var g = this.Groups[i];
            this.EndCustomerGroup(g);
        }
    }

    #endregion

    #region Spouse

    internal void UpdateSpouse()
    {
        // TODO some design decisions to be made
    }

    #endregion
}
