using System;
using System.Collections.Generic;
using System.Linq;
using FarmCafe.Framework.Characters;
using FarmCafe.Framework.Models;
using FarmCafe.Framework.Multiplayer;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using xTile.Tiles;
using static FarmCafe.Framework.Managers.TableManager;
using static FarmCafe.Framework.Utilities.Utility;

namespace FarmCafe.Framework.Managers
{
    internal class CustomerManager
    {
        internal static List<CustomerModel> CustomerModels;
        internal static List<CustomerModel> CustomerModelsInUse;

        internal static List<Customer> CurrentCustomers;
        internal static List<CustomerGroup> CurrentGroups;

        public static Point BusPosition;
        public static List<Point> BusToFarmWarps;
        public static Stack<Dictionary<GameLocation, Vector2>> PathToCafe;

        internal static bool ClientShouldUpdateCustomers = false;

        internal static int HowManyCustomersOnTable(Furniture table)
        {
            var chairs = GetChairsOfTable(table);
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

        internal static void SpawnGroupAtBus()
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
            }

            Debug.Show($"{memberCount} customer(s) arriving");
        }

        internal static GameLocation FurnitureLocation(Furniture table)
        {
            foreach (var location in CafeManager.CafeLocations)
                if (location.furniture.Contains(table))
                    return location;
            return null;
        }

        internal static CustomerGroup SpawnGroup(GameLocation location, Point tilePosition, int memberCount = 0)
        {
            var newtable = TryReserveTable();
            if (newtable == null)
            {
                Debug.Show("No tables to spawn customers");
                return null;
            }

            var group = new CustomerGroup
            {
                TableLocation = FurnitureLocation(newtable)
            };
            memberCount = memberCount > 0 ? Math.Min(GetChairsOfTable(newtable).Count, memberCount) : HowManyCustomersOnTable(newtable);

            for (var i = 0; i < memberCount; i++)
            {
                SpawnCustomer(group, location, tilePosition);
            }

            if (group.ReserveTable(newtable) == false)
                Debug.Log("ERROR: Couldn't reserve table (was supposed to be able to reserve)", LogLevel.Error);

            CurrentGroups.Add(group);
            Messaging.AddCustomerGroup(group);
            return group;
        }

        internal static List<Point> GetBusConvenePoints(int count)
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

        internal static CustomerModel GetRandomCustomerModel()
        {
            return CustomerModels.Any() ? CustomerModels[Game1.random.Next(CustomerModels.Count)] : null;
        }

        internal static Customer SpawnCustomer(CustomerGroup group, GameLocation location, Point tilePosition)
        {
            var model = GetRandomCustomerModel();
            if (model == null) 
                throw new Exception("Customer model not found.");

            var name = $"CustomerNPC_{model.Name}{CurrentCustomers.Count + 1}";
            var customer = new Customer(model, name, tilePosition, location);
            Debug.Log($"Customer {name} spawned");

            CurrentCustomers.Add(customer);
            group.Add(customer);
            customer.Group = group;
            return customer;
        }

        public static void RemoveAllCustomers()
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
            FreeAllTables();
        }

        public static void EndGroup(CustomerGroup group)
        {
            foreach (Customer c in group.Members)
                CurrentCustomers.Remove(c);
            CurrentGroups.Remove(group);
        }

        internal static void CacheBusPosition()
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

        internal static void HandleWarp(Customer customer, GameLocation location, Vector2 position)
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

        internal static void WarpGroup(CustomerGroup group, GameLocation location, Point warpPosition)
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

        internal static void Debug_ListCustomers()
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
    }
}