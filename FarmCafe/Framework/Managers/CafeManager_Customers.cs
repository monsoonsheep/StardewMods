using FarmCafe.Framework.Characters;
using FarmCafe.Framework.Models;
using FarmCafe.Framework.Objects;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using static FarmCafe.Framework.Utilities.Utility;

namespace FarmCafe.Framework.Managers
{
    internal partial class CafeManager
    {
        internal bool CheckSpawnCustomers()
        {
            int minutesTillCloses = Utility.CalculateMinutesBetweenTimes(Game1.timeOfDay, ClosingTime);
            int minutesTillOpens = Utility.CalculateMinutesBetweenTimes(Game1.timeOfDay, OpeningTime);
            int minutesSinceLastCustomers = Utility.CalculateMinutesBetweenTimes(LastTimeCustomersArrived, Game1.timeOfDay);
            int freeTablesCount = Tables.Count(t => !t.IsReserved);

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

            float percentageOfTablesFree = (float) freeTablesCount / (float) CafeManager.Tables.Count();
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
            var tables = GetFreeTables(memberCount);
            if (tables.Count == 0)
            {
                Logger.LogWithHudMessage("No tables to spawn customers");
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
                Logger.Log("ERROR: Couldn't reserve table (was supposed to be able to reserve)", LogLevel.Error);

            CurrentGroups.Add(group);
            Multiplayer.Sync.AddCustomerGroup(group);
            return group;
        }

        internal Customer VisitRegularNpc(NPC npc)
        {
            npc.ignoreScheduleToday = true;
            npc.currentLocation.characters.Remove(npc);
            Game1.removeThisCharacterFromAllLocations(npc);
            Customer customer = new Customer(npc);
            CustomerGroup group = new CustomerGroup();
            group.Add(customer);
            CurrentCustomers.Add(customer);
            customer.OrderItem = GetRandomItemFromMenu();
            CurrentGroups.Add(group);
            //group.ReserveTable(TableManager.GetFreeTables().First());
            customer.Group = group;
            //customer.GoToSeat();
            customer.HeadTowards(npc.currentLocation, npc.getTileLocationPoint() + new Point(3, 0), 3, customer.ConvertBack);

            return null;
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

            Logger.LogWithHudMessage($"{memberCount} customer(s) arriving");
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
                sprite: new AnimatedSprite(model.TilesheetPath, 0, 16, 32),
                portrait: Game1.content.Load<Texture2D>(model.Portrait));
            Logger.Log($"Customer {customer.Name} spawned");

            CurrentCustomers.Add(customer);
            group.Add(customer);
            customer.Group = group;
            return customer;
        }

        public void DeleteGroup(CustomerGroup group)
        {
            foreach (Customer c in group.Members)
                CurrentCustomers.Remove(c);
            CurrentGroups.Remove(group);
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
                Logger.Log("Warping group, charging", LogLevel.Debug);
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

        public void RemoveAllCustomers()
        {
            Logger.Log("Removing customers");
            foreach (var c in CurrentCustomers)
            {
                Game1.removeThisCharacterFromAllLocations(c);
                c.currentLocation?.characters?.Remove(c);
            }

            Multiplayer.Sync.RemoveAllCustomers();
            CurrentCustomers.Clear();
            CustomerModelsInUse?.Clear();
            CurrentGroups?.Clear();
            FreeAllTables();
        }

    }
}
