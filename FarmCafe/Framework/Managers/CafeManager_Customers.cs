﻿using FarmCafe.Framework.Characters;
using FarmCafe.Framework.Objects;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using FarmCafe.Framework.Multiplayer;
using Microsoft.Xna.Framework;
using static FarmCafe.Framework.Utility;
using FarmCafe.Models;
using SUtility = StardewValley.Utility;
using StardewValley.Buildings;

namespace FarmCafe.Framework.Managers
{
    internal partial class CafeManager
    {
        internal bool TrySpawnCustomers()
        {
            var tables = GetFreeTables();
            int minutesTillCloses = SUtility.CalculateMinutesBetweenTimes(Game1.timeOfDay, ClosingTime);
            int minutesTillOpens = SUtility.CalculateMinutesBetweenTimes(Game1.timeOfDay, OpeningTime);
            int minutesSinceLastCustomers = SUtility.CalculateMinutesBetweenTimes(LastTimeCustomersArrived, Game1.timeOfDay);
            float percentageOfTablesFree = (float)tables.Count / Tables.Count;

            if (minutesTillOpens > 0
                || minutesTillCloses <= 30 // Don't spawn customers after 30 minutes before closing time
                || tables.Count == 0
                || !GetMenuItems().Any())
                return false;

            // get regular NPC customer visit
            TryVisitNpcCustomers(Game1.timeOfDay);
            
            float prob = 0f;

            // more chance if it's been a while since last customers
            prob += minutesSinceLastCustomers switch
            {
                <= 20 => 0f,
                <= 30 => Game1.random.Next(5) == 0 ? 0.2f : -0.1f,
                <= 60 => Game1.random.Next(2) == 0 ? 0.3f : 0f,
                _ => 0.6f
            };

            // more chance if a higher percent of tables are free
            prob += (percentageOfTablesFree) switch
            {
                <= 0.2f => 0.1f,
                <= 0.5f => 0.3f,
                <= 0.8f => 0.5f,
                _ => 0.7f
            };

            // slight chance to spawn if last hour of open time
            if (minutesTillCloses <= 60)
                prob += (Game1.random.Next(20 + (minutesTillCloses / 3)) >= 28) ? 0.2f : -0.5f;

            // proc the chance to spawn
            if ((float) Game1.random.NextDouble() < prob)
            {
                LastTimeCustomersArrived = Game1.timeOfDay;
                SpawnGroupAtBus();
                return true;
            }
            return false;
        }

        internal bool TryVisitNpcCustomers(int timeOfDay)
        {
            foreach (string name in NpcCustomerSchedules.Keys.OrderBy(_ => Game1.random.Next()))
            {
                if (CurrentNpcCustomers.Contains(name))
                    continue;
                NPC npc = Game1.getCharacterFromName(name);
                if (npc == null)
                {
                    Logger.Log("Checking NPC for spawn but NPC wasn't found in any location");
                    continue;
                }

                // What can stop an NPC from starting to visit? 
                // Being busy, sleeping, about to go to sleep, 
                if (npc.isSleeping.Value)
                    continue;
                
                ScheduleData scheduleData = NpcCustomerSchedules[npc.Name];
                bool canVisit = true;
                if (npc.ScheduleKey == null)
                    continue;
                scheduleData.BusyTimes.TryGetValue(npc.ScheduleKey, out var busyPeriods);
                if (busyPeriods != null)
                {
                    foreach (BusyPeriod busyPeriod in busyPeriods)
                    {
                        if (SUtility.CalculateMinutesBetweenTimes(timeOfDay, busyPeriod.From) <= 120
                            && SUtility.CalculateMinutesBetweenTimes(timeOfDay, busyPeriod.To) > 0)
                        {
                            if (
                                !(busyPeriod.Priority <= 3 && Game1.random.Next(6 * busyPeriod.Priority) == 0) &&
                                !(busyPeriod.Priority == 4 && Game1.random.Next(50) == 0))
                            {
                                canVisit = false;
                                break;
                            }                            
                        }
                    }
                }

                if (!canVisit)
                    continue;

                npc.ignoreScheduleToday = true;
                npc.currentLocation.characters.Remove(npc);

                Table table = GetFreeTables().OrderBy(t => t.Seats.Count).First();

                List<NPC> npcsToVisit = new List<NPC>() { npc };
                List<Customer> customers = new List<Customer>();

                foreach (var member in npcsToVisit)
                {
                    Logger.LogWithHudMessage($"{member.Name} is visiting the cafe");
                    CurrentNpcCustomers.Add(member.Name);

                    Customer customer = new Customer(member);
                    customers.Add(customer);
                    CurrentCustomers.Add(customer);

                    customer.OrderItem = GetRandomItemFromMenu();
                }


                CustomerGroup group = new CustomerGroup(customers, table);
                CurrentGroups.Add(group);

                foreach (var customer in customers)
                {
                    customer.GoToSeat();
                }

                return true;

            }

            return false;
        }

        internal void SpawnGroupAtBus()
        {
            // debugging
            if (!GetMenuItems().Any())
            {
                return;
            }
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
                // If memberCount not specified, calculate how many members it should be based on
                // the number of chairs on the table
                table = tables.First();
                int countSeats = table.Seats.Count;
                memberCount = countSeats switch
                {
                    1 => 1,
                    2 => Game1.random.Next(2) == 0 ? 2 : 1,
                    <= 4 => Game1.random.Next(countSeats) == 0 ? 2 : Game1.random.Next(3, countSeats + 1),
                    _ => Game1.random.Next(4, countSeats + 1)
                };
            }
            else
            {
                // If memberCount is specified, get the table that has 
                table = tables.OrderBy(t => t.Seats.Count).First();
            }

            // If not enough models free, adjust member count
            List<CustomerModel> modelsToUse = GetRandomCustomerModels(memberCount);
            //if (modelsToUse.Count < memberCount)
            //    memberCount = modelsToUse.Count;
            //if (memberCount == 0)
            //{
            //    Logger.Log("No more models to use for customers");
            //    return null;
            //}

            List<Customer> customers = new List<Customer>();
            for (var i = 0; i < memberCount; i++)
            {
                Customer c = SpawnCustomer(location, tilePosition, modelsToUse[0]);
                customers.Add(c);
                c.OrderItem = GetRandomItemFromMenu();
            }

            CustomerGroup group;
            try
            {
                group = new CustomerGroup(customers, table);
            }
            catch (Exception e)
            {
                Logger.Log("Error while creating a customer group: " + e.Message, LogLevel.Error);
                return null;
            }

            CurrentGroups.Add(group);
            Sync.AddCustomerGroup(group);
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

        internal List<CustomerModel> GetRandomCustomerModels(int count = 0)
        {
            List<CustomerModel> results = new List<CustomerModel>();
            if (count == 0)
                count = CustomerModels.Count;
            foreach (var model in CustomerModels)
            {
                if (CustomerModelsInUse.Contains(model.Name))
                    continue;
                if (count == 0)
                    break;

                count--;
                //CustomerModelsInUse.Add(model.Name);
                results.Add(model);
            }

            return results;
        }

        internal Customer SpawnCustomer(GameLocation location, Point tilePosition, CustomerModel model)
        {
            Customer customer = new Customer(
                name: $"CustomerNPC_{model.Name}{CurrentCustomers.Count + 1}",
                targetTile: tilePosition,
                location: location, 
                sprite: new AnimatedSprite(model.TilesheetPath, 0, 16, 32),
                portrait: Game1.content.Load<Texture2D>(model.Portrait));
            Logger.Log($"Customer {customer.Name} spawned");

            CurrentCustomers.Add(customer);
            return customer;
        }

        public void DeleteGroup(CustomerGroup group)
        {
            foreach (Customer c in group.Members)
            {
                CurrentCustomers.Remove(c);
                CurrentNpcCustomers.Remove(c.Name);
            }           
            CurrentGroups.Remove(group);
        }

        internal void HandleWarp(Customer customer, GameLocation location, Vector2 position)
        {
            foreach (var other in CurrentCustomers)
            {
                if (other.Equals(customer)
                    || !other.currentLocation.Equals(customer.currentLocation)
                    || !other.Tile.Equals(customer.Tile))
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
                c.currentLocation?.characters?.Remove(c);
            }

            Sync.RemoveAllCustomers();
            CurrentCustomers.Clear();
            CurrentNpcCustomers.Clear();
            CustomerModelsInUse?.Clear();
            CurrentGroups?.Clear();
            FreeAllTables();
        }

        internal static List<Customer> GetAllCustomersInGame()
        {
            var locationCustomers = Game1.locations
                .SelectMany(l => l.characters)
                .OfType<Customer>();

            
            var buildingCustomers = (Game1.getFarm().buildings
                    .Where(b => b.GetIndoors() != null)
                    .SelectMany(b => b.GetIndoors().characters))
                .OfType<Customer>();

            var list = locationCustomers.Concat(buildingCustomers).ToList();

            Logger.Log("Updating customers" + string.Join(' ', list));
            return list;
        }
    }
}
