using StardewValley;
using StardewValley.Locations;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FarmCafe.Framework.Utilities.Utility;
using StardewModdingAPI;
using FarmCafe.Framework.Characters;
using FarmCafe.Framework.Models;
using FarmCafe.Framework.Objects;
using FarmCafe.Locations;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Objects;
using xTile.Layers;
using xTile.ObjectModel;
using xTile.Tiles;

namespace FarmCafe.Framework.Managers
{
    internal class CafeManager
    {
        internal readonly TableManager TableManager;

        internal List<GameLocation> CafeLocations;
        internal IList<Item> MenuItems;
        internal List<Customer> CurrentCustomers;
        internal NPC HelperNpc;


        internal List<List<string>> RoutesToCafe;

        internal List<CustomerModel> CustomerModels;
        internal List<CustomerModel> CustomerModelsInUse;
        internal List<CustomerGroup> CurrentGroups;

        public Point BusPosition;

        internal int openingTime = 0800;
        internal int closingTime = 2100;
        internal int LastTimeCustomersArrived;
        internal short CustomerGroupsDinedToday;
        internal short NumberOfCustomerGroupsPresentRightNow;

        public CafeManager(ref TableManager t, ref List<GameLocation> cafeLocationsList, ref IList<Item> menuItemsList, ref List<Customer> customersList, NPC helperNpc)
        {
            TableManager = t;
            CafeLocations = cafeLocationsList;
            MenuItems = menuItemsList;
            CurrentCustomers = customersList;
            HelperNpc = helperNpc;

            CurrentGroups = new List<CustomerGroup>();
            CacheBusPosition();
        }

        internal bool CheckSpawnCustomers()
        {
            int minutesTillCloses = Utility.CalculateMinutesBetweenTimes(Game1.timeOfDay, closingTime);
            int minutesTillOpens = Utility.CalculateMinutesBetweenTimes(Game1.timeOfDay, closingTime);
            int minutesSinceLastCustomers = Utility.CalculateMinutesBetweenTimes(LastTimeCustomersArrived, Game1.timeOfDay);
            int freeTablesCount = TableManager.Tables.Count(t => !t.IsReserved);

            if (minutesTillOpens > 0
                || minutesTillCloses <= 30 // Don't spawn customers after 30 minutes before closing time
                || freeTablesCount == 0)
                return false;
            
            float prob = 0f;

            if (minutesTillCloses <= 60)
                prob += (Game1.random.Next(20 + (minutesTillCloses / 3)) >= 28) ? 0.2f : -0.5f;

            prob += minutesSinceLastCustomers switch
            {
                <= 20 => 0f,
                <= 30 => Game1.random.Next(5) == 0 ? 0.2f : -0.1f,
                <= 60 => Game1.random.Next(2) == 0 ? 0.3f : 0f,
                _ => 0.6f
            };

            float percentageOfTablesFree = (float) freeTablesCount / (float) TableManager.Tables.Count();
            prob += (percentageOfTablesFree) switch
            {
                <= 0.2f => 0.1f,
                <= 0.5f => 0.3f,
                <= 0.8f => 0.5f,
                _ => 0.7f
            };

            if ((float) Game1.random.NextDouble() < prob)
            {
                LastTimeCustomersArrived = Game1.timeOfDay;
                SpawnGroupAtBus();
                return true;
            }
            return false;
        }

        internal CustomerGroup SpawnGroup(GameLocation location, Point tilePosition, int memberCount = 0)
        {
            var tables = TableManager.GetFreeTables(memberCount);
            if (tables.Count == 0)
            {
                Debug.LogWithHudMessage("No tables to spawn customers");
                return null;
            }

            Table table;

            if (memberCount == 0)
            {
                table = tables.First();
                int countSeats = table.Seats.Count;
                memberCount = table.Seats.Count switch
                {
                    1 => 1,
                    2 => Game1.random.Next(2) == 0 ? 2 : 1,
                    <= 4 => Game1.random.Next(countSeats) == 0 ? 2 : Game1.random.Next(3, countSeats + 1),
                    _ => Game1.random.Next(4, countSeats + 1)
                };
            }
            else
            {
                table = tables.OrderBy(t => t.Seats.Count).First();
            }

            var group = new CustomerGroup();
            for (var i = 0; i < memberCount; i++)
            {
                Customer c = SpawnCustomer(group, location, tilePosition);
                c.OrderItem = GetRandomItemFromMenu();
            }

            if (group.ReserveTable(table) is false)
                Debug.Log("ERROR: Couldn't reserve table (was supposed to be able to reserve)", LogLevel.Error);

            CurrentGroups.Add(group);
            Multiplayer.AddCustomerGroup(group);
            return group;
        }

        internal void SpawnGroupAtBus()
        {
            var group = SpawnGroup(FarmCafe.GetLocationFromName("BusStop"), BusPosition);
            if (group == null)
                return;

            var memberCount = group.Members.Count;
            var convenePoints = GetBusConvenePoints(memberCount);
            for (var i = 0; i < memberCount; i++)
            {
                group.Members[i].SetBusConvene(convenePoints[i], i * 800 + 1);
                group.Members[i].faceDirection(2);
                group.Members[i].State.Set(CustomerState.ExitingBus);
            }

            Debug.LogWithHudMessage($"{memberCount} customer(s) arriving");
        }

        internal void PopulateRoutesToCafe()
        {
            RoutesToCafe = new List<List<string>>();
            foreach (string loc in new[] { "BusStop", "Farm" })
            {
                RoutesToCafe.Add(FindLocationRouteToCafe(FarmCafe.GetLocationFromName(loc), CafeLocations.First()));
            }

            RoutesToCafe.ForEach((route) => Debug.Log(string.Join(" - ", route)));
        }

        public List<string> FindLocationRouteToCafe(GameLocation startLocation, GameLocation endLocation)
        {
            var frontier = new Queue<string>();
            frontier.Enqueue(startLocation.Name);

            var cameFrom = new Dictionary<string, string>
            {
                [startLocation.Name] = null
            };

            while (frontier.Count > 0)
            {
                string currentName = frontier.Dequeue();
                GameLocation current = FarmCafe.GetLocationFromName(currentName);

                if (current == null)
                    continue;
                if (current.Name == endLocation.Name)
                    break;

                foreach (var name in current.warps.Select(warp => warp.TargetName)
                             .Where(name => !cameFrom.ContainsKey(name)))
                {
                    frontier.Enqueue(name);
                    cameFrom[name] = current.Name;
                }

                foreach (var name in current.doors.Keys.Select(p => current.doors[p])
                             .Where(name => !cameFrom.ContainsKey(name)))
                {
                    frontier.Enqueue(name);
                    cameFrom[name] = current.Name;
                }

                if (current is not BuildableGameLocation buildableCurrent) continue;

                foreach (var building in buildableCurrent.buildings.Where(b => b.indoors.Value != null))
                {
                    string name = building.indoors.Value.Name;
                    if (cameFrom.ContainsKey(name)) continue;

                    frontier.Enqueue(name);
                    cameFrom[name] = current.Name;
                }
            }

            List<string> path = new List<string>() { endLocation.Name };
            string point = endLocation.Name;
            while (true)
            {
                if (cameFrom.ContainsKey(point))
                {
                    path.Add(cameFrom[point]);
                    point = cameFrom[point];
                    if (point == startLocation.Name) break;
                }
                else
                {
                    return null;
                }
            }

            path.Reverse();
            return path;
        }

        internal List<string> GetLocationRoute(GameLocation start, GameLocation end)
        {
            List<string> route = RoutesToCafe.FirstOrDefault(
                r => r.First() == start.Name && r.Last() == end.Name
            )?.ToList();
            if (route == null)
            {
                route = RoutesToCafe.FirstOrDefault(
                    r => r.First() == end.Name && r.Last() == start.Name
                )?.ToList();
                route?.Reverse();
            }

            return route;
        }

        internal List<Point> GetBusConvenePoints(int count)
        {
            var startingPoint = BusPosition + new Point(0, 3);
            var points = new List<Point>();

            for (var i = 1; i <= 4; i++)
            {
                points.Add(new Point(startingPoint.X, startingPoint.Y + i));
                points.Add(new Point(startingPoint.X + 1, startingPoint.Y + i));
            }

            return points.OrderBy(x => Game1.random.Next()).Take(count).OrderBy(p => -p.Y).ToList();
        }

        internal CustomerModel GetRandomCustomerModel()
        {
            return CustomerModels.Any() ? CustomerModels[Game1.random.Next(CustomerModels.Count)] : null;
        }

        internal Customer SpawnCustomer(CustomerGroup group, GameLocation location, Point tilePosition)
        {
            CustomerModel model;
            if ((model = GetRandomCustomerModel()) == null)
                throw new Exception("Customer model not found.");

            Customer customer = new Customer(
                name: $"CustomerNPC_{model.Name}{CurrentCustomers.Count + 1}",
                targetTile: tilePosition,
                location: location, 
                portrait: FarmCafe.ModHelper.ModContent.Load<Texture2D>($"assets/Portraits/{model.PortraitName}.png"), 
                tileSheetPath: model.TilesheetPath);
            Debug.Log($"Customer {customer.Name} spawned");

            CurrentCustomers.Add(customer);
            group.Add(customer);
            customer.Group = group;
            return customer;
        }

        public void EndGroup(CustomerGroup group)
        {
            foreach (Customer c in group.Members)
                CurrentCustomers.Remove(c);
            CurrentGroups.Remove(group);
        }

        internal void CacheBusPosition()
        {
            Tile[,] tiles = FarmCafe.GetLocationFromName("BusStop").Map.GetLayer("Back").Tiles.Array;
            for (var i = 0; i < tiles.GetLength(0); i++)
            for (var j = 0; j < tiles.GetLength(1); j++)
                if (tiles[i, j].Properties.ContainsKey("TouchAction") && tiles[i, j].Properties["TouchAction"] == "Bus")
                {
                    BusPosition = new Point(i, j + 1);
                    //Debug.Log($"bus position is {BusPosition}");
                    return;
                }

            Debug.Log("Couldn't find Bus position in Bus Stop", LogLevel.Warn);
            BusPosition = new Point(12, 10);
        }

        internal void HandleWarp(Customer customer, GameLocation location, Vector2 position)
        {
            foreach (var other in CurrentCustomers)
            {
                if (other.Equals(customer)
                    || !other.currentLocation.Equals(customer.currentLocation)
                    || !other.getTileLocation().Equals(customer.getTileLocation()))
                    continue;

                other.isCharging = true;
                Debug.Log("Warping group, charging", LogLevel.Debug);
            }
        }

        internal void WarpGroup(CustomerGroup group, GameLocation location, Point warpPosition)
        {
            var points = AdjacentTiles(warpPosition).ToList();
            if (points.Count < group.Members.Count)
                return;
            for (var i = 0; i < group.Members.Count; i++)
            {
                Game1.warpCharacter(group.Members[i], location, points[i].ToVector2());
                group.Members[i].StartConvening();
            }
        }

        // For multiplayer
        internal static List<Customer> GetAllCustomersInGame()
        {
            var locationCustomers = Game1.locations
                .SelectMany(l => l.getCharacters())
                .OfType<Customer>();

            var buildingCustomers = (Game1.getFarm().buildings
                    .Where(b => b.indoors.Value != null)
                    .SelectMany(b => b.indoors.Value.characters))
                .OfType<Customer>();

            var list = locationCustomers.Concat(buildingCustomers).ToList();

            Debug.Log("Updating customers" + string.Join(' ', list));
            return list;
        }

        public bool AddToMenu(Item itemToAdd)
        {
            if (MenuItems.Contains(itemToAdd, new ItemEqualityComparer()))
                return false;
            
            for (int i = 0; i < MenuItems.Count; i++)
            {
                if (MenuItems[i] == null)
                {
                    MenuItems[i] = itemToAdd.getOne();
                    MenuItems[i].Stack = 1;
                    return true;
                }
            }

            return false;
        }

        public Item RemoveFromMenu(int slotNumber)
        {
            Item tmp = MenuItems[slotNumber];
            if (tmp == null)
                return null;
            
            MenuItems[slotNumber] = null;
            int firstEmpty = slotNumber;
            for (int i = slotNumber + 1; i < MenuItems.Count; i++)
            {
                if (MenuItems[i] != null)
                {
                    MenuItems[firstEmpty] = MenuItems[i];
                    MenuItems[i] = null;
                    firstEmpty += 1;
                }
            }

            return tmp;
        }

        internal Item GetRandomItemFromMenu()
        {
            return MenuItems.Where(c => c != null).OrderBy((i) => Game1.random.Next()).FirstOrDefault();
        }

        internal static void FarmerClickTable(Table table, Farmer who)
        {
            CustomerGroup groupOnTable =
                FarmCafe.CurrentCustomers.FirstOrDefault(c => c.Group.ReservedTable == table)?.Group;

            if (groupOnTable == null)
            {
                Debug.Log("Didn't get group from table");
                return;
            }

            if (groupOnTable.Members.All(c => c.State.Value == CustomerState.OrderReady))
            {
                foreach (Customer customer in groupOnTable.Members)
                {
                    customer.StartWaitForOrder();
                }
            }
            else if (groupOnTable.Members.All(c => c.State.Value == CustomerState.WaitingForOrder))
            {
                foreach (Customer customer in groupOnTable.Members)
                {
                    if (customer.OrderItem != null && who.hasItemInInventory(customer.OrderItem.ParentSheetIndex, 1))
                    {
                        Debug.Log($"Customer item = {customer.OrderItem.ParentSheetIndex}, inventory = {who.hasItemInInventory(customer.OrderItem.ParentSheetIndex, 1)}");
                        customer.OrderReceive();
                        who.removeFirstOfThisItemFromInventory(customer.OrderItem.ParentSheetIndex);
                    }
                }
            }
            return;
        }

        public void RemoveAllCustomers()
        {
            Debug.Log("Removing customers");
            foreach (var c in CurrentCustomers)
            {
                Game1.removeThisCharacterFromAllLocations(c);
                c.currentLocation?.characters?.Remove(c);
            }

            Multiplayer.RemoveAllCustomers();
            CurrentCustomers.Clear();
            CustomerModelsInUse?.Clear();
            CurrentGroups?.Clear();
            TableManager.FreeAllTables();
        }
    }

    public class ItemEqualityComparer : IEqualityComparer<Item>
    {
        public bool Equals(Item x, Item y)
        {
            return x != null && y != null && x.ParentSheetIndex == y.ParentSheetIndex;
        }

        public int GetHashCode(Item obj) => (obj != null) ? obj.ParentSheetIndex * 900 : -1;
    }

}