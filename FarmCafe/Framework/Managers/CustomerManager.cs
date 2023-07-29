using FarmCafe.Framework.Customers;
using FarmCafe.Framework.Models;
using FarmCafe.Framework.Multiplayer;
using FarmCafe.Locations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FarmCafe.Framework.Characters;
using xTile.Dimensions;
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
			CustomerGroup group = CreateGroup(GetLocationFromName("BusStop"), BusPosition);
			if (group == null)
			{
				return;
			}
			int memberCount = group.Members.Count;

            List<Point> convenePoints = GetBusConvenePoints(memberCount);
			for (int i = 0; i < memberCount; i++)
			{
				group.Members[i].SetBusConvene(convenePoints[i], i * 800 + 1);
                group.Members[i].faceDirection(2);
            }

            Debug.Log($"{memberCount} customer(s) arriving");
		}

		internal static void SpawnCustomerGroup(GameLocation location, Point tilePosition, int count = 0)
		{
            
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

		internal static CustomerGroup CreateGroup(GameLocation location, Point tilePosition, int memberCount = 0)
		{
            Furniture newtable = TryReserveTable();

            if (newtable == null)
            {
                Debug.Log("No tables found to spawn customers", LogLevel.Error);
                return null;
            }

            CustomerGroup group = new CustomerGroup();
            group.TableLocation = FurnitureLocation(newtable);
			memberCount = (memberCount > 0) ? GetChairsOfTable(newtable).Count : HowManyCustomersOnTable(newtable);

            for (int i = 0; i < memberCount; i++)
            {
                Customer customer = SpawnCustomer(location, tilePosition);
                group.Add(customer);
                customer.Group = group;
            }

            if (group.ReserveTable(newtable) == false)
            {
                Debug.Log("ERROR: Couldn't reserve table (was supposed to be able to reserve)", LogLevel.Error);
            }

            CurrentGroups.Add(group);
            Messaging.AddCustomerGroup(group);
			return group;
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
			string name = $"CustomerNPC_{model.Name}{CurrentCustomers.Count + 1}";
			Customer customer = new Customer(model, name, tilePosition, location);
			CurrentCustomers.Add(customer);
            Debug.Log($"Customer {name} spawned");
            return customer;
		}

		public static void RemoveAllCustomers()
		{
			if (CurrentCustomers == null) return;
			Debug.Log("Removing customers");
			foreach (Customer c in CurrentCustomers)
			{
				c.currentLocation?.characters?.Remove(c);
				c.Seat.modData["FarmCafeChairIsReserved"] = "F";
			}
			
			Messaging.RemoveAllCustomers();
			CurrentCustomers.Clear();
			CustomerModelsInUse.Clear();
			CurrentGroups.Clear();
			FreeAllTables();
		}

		internal static void CacheBusPosition()
		{
			var tiles = GetLocationFromName("BusStop").Map.GetLayer("Back").Tiles.Array;
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

        internal static void HandleWarp(Customer customer, GameLocation location, Vector2 position)
		{
			if (location.Name.Contains("Cafe"))
			{
				position += new Vector2(0, -64);
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
			var points = AdjacentTiles(warpPosition).ToList();
			if (points.Count < group.Members.Count)
				return;
			for (int i = 0; i < group.Members.Count; i++)
			{
				Game1.warpCharacter(group.Members[i], location, points[i].ToVector2());
				group.Members[i].StartConvening();
			}
		}

		internal static List<Customer> GetAllCustomersInGame()
		{
			var list = new List<Customer>();

			var locationCustomers = Game1.locations
				.SelectMany(l => l.getCharacters())
				.Where(c => c is Customer)
				.Select((c) => c as Customer);

            var buildingCustomers = Game1.getFarm()?.buildings
				.Where(b => b.indoors.Value != null)
				.SelectMany(b => b.indoors.Value.characters)
				.Where(c => c is Customer)
				.Select(c => c as Customer);
			list = locationCustomers.Concat(buildingCustomers).ToList();

            Debug.Log("Updating customers" + string.Join(' ', list));
			return list;
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