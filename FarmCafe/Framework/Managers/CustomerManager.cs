﻿using FarmCafe.Framework.Customers;
using FarmCafe.Framework.Models;
using FarmCafe.Framework.Multiplayer;
using FarmCafe.Locations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
		public static List<List<string>> routesToCafe;

		internal static int HowManyCustomersOnTable(Furniture table)
		{
			int n;
            List<Furniture> chairs = GetChairsOfTable(table);
            int countSeats = chairs.Count;
            Debug.Log($"got table! with {countSeats} seats!");


            if (countSeats == 1)
            {
                n = 1;
            }
            else if (countSeats == 2)
            {
                n = (Game1.random.Next(2) == 0) ? 2 : 1;
            }
            else if (countSeats <= 4)
            {
                if (Game1.random.Next(countSeats) == 0)
                {
                    n = 1;
                }
                else
                {
                    n = Game1.random.Next(2, countSeats + 1);
                }
            }
            else
            {
                n = Game1.random.Next(2, 5);
            }

			return n;
        }

        internal static void SpawnGroupBus()
		{
			Furniture newtable = TryReserveTable();

			if (newtable == null)
			{
				Debug.Log("No tables found to spawn customers", LogLevel.Error);
				return;
			}

			int countMembers = HowManyCustomersOnTable(newtable);

			CustomerGroup group = CreateGroup(countMembers, Game1.getLocationFromName("BusStop"), BusPosition, newtable);

            List<Point> convenePoints = GetBusConvenePoints(countMembers);
			for (int i = 0; i < countMembers; i++)
			{
				group.Members[i].busDepartTimer = i * 800 + 1;
				group.Members[i].busConvenePoint = convenePoints[i];
                group.Members[i].faceDirection(2);
				group.TableLocation = FurnitureLocation(newtable);
            }
            Debug.Log($"{countMembers} customer(s) arriving");
		}

		internal static GameLocation FurnitureLocation(Furniture table)
		{
			foreach (var location in CafeManager.CafeLocations)
			{
				if (location.furniture.Contains(table))
				{
					return location;
				}
			}
			return null;
		}

		internal static CustomerGroup CreateGroup(int memberCount, GameLocation location, Point tilePosition, Furniture tableToReserve = null)
		{
            CustomerGroup group = new CustomerGroup();
            for (int i = 0; i < memberCount; i++)
            {
                Customer customer = SpawnCustomer(location, tilePosition);
                group.Add(customer);
                customer.Group = group;
            }

			if (tableToReserve != null)
			{
				if (group.ReserveTable(tableToReserve) == false)
				{
					Debug.Log("ERROR: Couldn't reserve table (was supposed to be able to reserve)", LogLevel.Error);
				}
			}

            CurrentGroups.Add(group);
            Messaging.AddCustomerGroup(group);
			return group;
        }

		internal static void UpdateCustomerList(List<string> names)
		{
            if (names == null) return;

            if (names.Count == 0)
            {
                CurrentCustomers.Clear();
                Debug.Log("Removing all tracked customers.");
                return;
            }

            foreach (var name in names)
            {
                Customer c = Game1.getCharacterFromName(name) as Customer;

                if (c == null)
                {
                    Debug.Log("Updating client's customers but received bad customer information.", LogLevel.Error);
                    continue;
                }

                if (CurrentCustomers.Contains(c))
                {
                    Debug.Log("Updating client's customers but customer already tracked for client.", LogLevel.Error);
                    continue;
                }

                CurrentCustomers.Add(c);
            }
        }

		internal static List<Point> GetBusConvenePoints(int count)
		{
			Point startingPoint = BusPosition + new Point(0, 3);
			var points = new List<Point>();

			for (int i = 1; i <= 4; i++)
			{
				points.Add(new Point(startingPoint.X, startingPoint.Y + i));
				points.Add(new Point(startingPoint.X + 1, startingPoint.Y + i));
			}
			return points.OrderBy(x => Game1.random.Next()).Take(count).OrderBy(p => -p.Y).ToList();
		}

		internal static CustomerModel GetRandomCustomerModel()
		{
			if (CustomerModels.Any())
			{
				return CustomerModels[Game1.random.Next(CustomerModels.Count)];
			}
			else
			{
				return CustomerModels.FirstOrDefault();
			}
		}

		internal static Customer SpawnCustomer(GameLocation location, Point tilePosition)
		{
			CustomerModel model = GetRandomCustomerModel();
			string name = $"Customer_{model.Name}{CurrentCustomers.Count + 1}";
			Customer customer = new Customer(model, name, tilePosition, location);
			CurrentCustomers.Add(customer);
			return customer;
		}

		public static void RemoveAllCustomers()
		{
			Debug.Log("Removing customers");
			foreach (Customer c in CurrentCustomers)
			{
				c.currentLocation.characters.Remove(c);
				c.Seat.modData["FarmCafeChairIsReserved"] = "F";
			}

			CurrentCustomers.Clear();
			CustomerModelsInUse.Clear();
			CurrentGroups.Clear();
			FreeAllTables();
		}

		internal static void CacheBusPosition()
		{
			var tiles = Game1.getLocationFromName("BusStop").Map.GetLayer("Back").Tiles.Array;
			for (int i = 0; i < tiles.GetLength(0); i++)
			{
				for (int j = 0; j < tiles.GetLength(1); j++)
				{
					if (tiles[i, j].Properties.ContainsKey("TouchAction") && tiles[i, j].Properties["TouchAction"] == "Bus")
					{
						BusPosition = new Point(i, j + 1);
						//Debug.Log($"bus position is {BusPosition}");
						return;
					}
				}
			}

			Debug.Log("Couldn't find Bus position in Bus Stop", LogLevel.Warn);
			BusPosition = new Point(12, 10);
		}

		internal static Point GetBusWarpToFarm()
		{
			return BusToFarmWarps[new Random().Next(BusToFarmWarps.Count)];
		}

		internal static void CacheBusWarpsToFarm()
		{
			BusToFarmWarps = new List<Point>();

			foreach (var warp in Game1.getLocationFromName("BusStop").warps)
			{
				if (warp.TargetName == "Farm")
				{
					BusToFarmWarps.Add(new Point(warp.X, warp.Y));
				}
			}
		}

        internal static void populateRoutesToCafe()
        {
            routesToCafe = new List<List<string>>();
            foreach (string loc in new[] { "BusStop", "Town", "Beach" })
            {
                FindLocationRouteToCafe(Game1.getLocationFromName(loc), CafeManager.CafeLocations.First());
            }
            foreach (var route in routesToCafe)
            {
                Debug.Log(string.Join(" - ", route));
            }
        }

        public static void FindLocationRouteToCafe(GameLocation startLocation, GameLocation endLocation)
        {
            Queue<string> frontier = new Queue<string>();
            frontier.Enqueue(startLocation.Name);

            Dictionary<string, string> cameFrom = new Dictionary<string, string>();
            cameFrom[startLocation.Name] = null;

            while (frontier.Count > 0)
            {
                GameLocation current = Game1.getLocationFromName(frontier.Dequeue());
                if (current.Name == endLocation.Name)
                    break;

                foreach (Warp warp in current.warps)
                {
                    if (!cameFrom.ContainsKey(warp.TargetName))
                    {
                        frontier.Enqueue(warp.TargetName);
                        cameFrom[warp.TargetName] = current.Name;
                    }
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
                    return;
                }

            }
            path.Reverse();
            routesToCafe.Add(path);
        }

        internal static void HandleWarp(Customer customer, GameLocation location, Vector2 position)
		{
			if (location is Farm && customer.Group != null)
			{
				customer.arriveAt(location);
				//customer.ArriveAtFarm();
			}
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
			foreach (Customer customer in group.Members)
			{
				Game1.warpCharacter(customer, location, warpPosition.ToVector2());
			}
		}

		internal static void Debug_ListCustomers()
		{
			Debug.Log("Characters in current");
			foreach (var ch in Game1.currentLocation.characters)
			{
				if (ch is Customer)
				{
					Debug.Log(ch.ToString());
				}
				else
				{
					Debug.Log("NPC: " + ch.Name);
				}
			}
			Debug.Log("Current customers: ");
			foreach (var customer in CurrentCustomers)
			{
				Debug.Log(customer.ToString());
			}

			Debug.Log("Current models: ");
			foreach (var model in CustomerModels)
			{
				Debug.Log(model.ToString());
			}
			foreach (var f in Game1.getFarm().furniture)
			{
				foreach (var pair in f.modData.Pairs)
				{
					Debug.Log($"{pair.Key}: {pair.Value}");

				}
                Debug.Log(f.modData.ToString());
            }
        }

	}
}