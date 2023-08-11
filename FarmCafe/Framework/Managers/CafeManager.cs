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
using FarmCafe.Framework.Multiplayer;
using FarmCafe.Locations;
using StardewValley.Objects;
using xTile.Tiles;

namespace FarmCafe.Framework.Managers
{

    internal class CafeManager
    {
        private readonly TableManager tableManager;
        internal List<GameLocation> CafeLocations = new();
        public List<List<string>> RoutesToCafe;
        internal NPC HelperNpc;

        public IList<Item> MenuItems;
        public IList<Item> RecentlyAddedMenuItems;

        internal List<CustomerModel> CustomerModels;
        internal List<CustomerModel> CustomerModelsInUse;
        internal List<Customer> CurrentCustomers;
        internal List<CustomerGroup> CurrentGroups;
        public Point BusPosition;

        internal bool ClientShouldUpdateCustomers = false;

        public CafeManager(TableManager t)
        {
            tableManager = t;
            MenuItems = new List<Item>(new Item[27]);
            MenuItems[0] = new StardewValley.Object(746, 1).getOne();

            RecentlyAddedMenuItems = new List<Item>(new Item[9]);
            RecentlyAddedMenuItems[0] = new StardewValley.Object(746, 1).getOne();
        }

        internal void SetUpHelper(string name)
        {
            HelperNpc = Game1.getCharacterFromName(name);
        }

        internal void PopulateRoutesToCafe()
        {
            RoutesToCafe = new List<List<string>>();
            foreach (string loc in new[] { "BusStop", "Town", "Beach", "Farm" })
            {
                RoutesToCafe.Add(FindLocationRouteToCafe(GetLocationFromName(loc), CafeLocations.First()));
            }

            foreach (var route in RoutesToCafe)
            {
                Debug.Log(string.Join(" - ", route));
            }
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
                GameLocation current = GetLocationFromName(currentName);

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

        internal int HowManyCustomersOnTable(Furniture table)
        {
            var chairs = tableManager.GetChairsOfTable(table);
            var countSeats = chairs.Count;
            Debug.Log($"got table! with {countSeats} seats!");

            var num = countSeats switch
            {
                1 => 1,
                2 => Game1.random.Next(2) == 0 ? 2 : 1,
                <= 4 => Game1.random.Next(countSeats) == 0 ? 1 : Game1.random.Next(2, countSeats + 1),
                _ => Game1.random.Next(2, 5)
            };

            return num;
        }

        internal void SpawnGroupAtBus()
        {
            var group = SpawnGroup(GetLocationFromName("BusStop"), BusPosition);
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

            Debug.Show($"{memberCount} customer(s) arriving");
        }

        internal Item GetRandomItemFromMenu()
        {
            return MenuItems.Where(c => c != null).OrderBy((i) => Game1.random.Next()).FirstOrDefault();
        }

        internal GameLocation FurnitureLocation(Furniture table)
        {
            return CafeLocations.FirstOrDefault(location => location.furniture.Contains(table));
        }

        internal CustomerGroup SpawnGroup(GameLocation location, Point tilePosition, int memberCount = 0)
        {
            var newtable = tableManager.TryReserveTable();
            if (newtable == null)
            {
                Debug.Show("No tables to spawn customers");
                return null;
            }

            var group = new CustomerGroup
            {
                TableLocation = FurnitureLocation(newtable)
            };
            memberCount = memberCount > 0
                ? Math.Min(tableManager.GetChairsOfTable(newtable).Count, memberCount)
                : HowManyCustomersOnTable(newtable);

            for (var i = 0; i < memberCount; i++)
            {
                Customer c = SpawnCustomer(group, location, tilePosition);
                c.OrderItem = GetRandomItemFromMenu();
            }

            if (group.ReserveTable(newtable) == false)
                Debug.Log("ERROR: Couldn't reserve table (was supposed to be able to reserve)", LogLevel.Error);

            CurrentGroups.Add(group);
            Messaging.AddCustomerGroup(group);
            return group;
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

            var name = $"CustomerNPC_{model.Name}{CurrentCustomers.Count + 1}";
            var customer = new Customer(model, name, tilePosition, location);
            Debug.Log($"Customer {name} spawned");

            CurrentCustomers.Add(customer);
            group.Add(customer);
            customer.Group = group;
            return customer;
        }

        public void RemoveAllCustomers()
        {
            if (CurrentCustomers == null) return;
            Debug.Log("Removing customers");
            foreach (var c in CurrentCustomers)
            {
                Game1.removeThisCharacterFromAllLocations(c);
                c.currentLocation?.characters?.Remove(c);
                c.Seat.modData["FarmCafeChairIsReserved"] = "F";
            }

            Messaging.RemoveAllCustomers();
            CurrentCustomers.Clear();
            CustomerModelsInUse.Clear();
            CurrentGroups.Clear();
            tableManager.FreeAllTables();
        }

        public void EndGroup(CustomerGroup group)
        {
            foreach (Customer c in group.Members)
                CurrentCustomers.Remove(c);
            CurrentGroups.Remove(group);
        }

        internal void CacheBusPosition()
        {
            Tile[,] tiles = GetLocationFromName("BusStop").Map.GetLayer("Back").Tiles.Array;
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
                //var newPos = GetAdjacentTileCollision(customer.getTileLocationPoint(), location, character: customer);
                Debug.Log("Warping group, charging", LogLevel.Debug);

                //if (!newPos.Equals(Point.Zero))
                //{
                //	Debug.Log("Changing position to avoid collisions", LogLevel.Debug);
                //	customer.Position = new Vector2(newPos.X * 64, newPos.Y * 64);
                //	break;
                //}
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

        internal List<Customer> GetAllCustomersInGame()
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

        internal void Debug_ListCustomers()
        {
            Debug.Log("Characters in current");
            foreach (var ch in Game1.currentLocation.characters)
                if (ch is Customer)
                    Debug.Log(ch.ToString());
                else
                    Debug.Log("NPC: " + ch.Name);
            Debug.Log("Current customers: ");
            foreach (var customer in CurrentCustomers) Debug.Log(customer.ToString());

            Debug.Log("Current models: ");
            foreach (var model in CustomerModels) Debug.Log(model.ToString());
            foreach (var f in Game1.getFarm().furniture)
            {
                foreach (var pair in f.modData.Pairs) Debug.Log($"{pair.Key}: {pair.Value}");
                Debug.Log(f.modData.ToString());
            }
        }

        
        internal GameLocation GetLocationFromName(string name)
        {
            return Game1.getLocationFromName(name) ?? CafeLocations.FirstOrDefault(a => a.Name == name);
        }

    }
}
