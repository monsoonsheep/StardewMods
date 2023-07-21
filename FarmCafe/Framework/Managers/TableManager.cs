using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using StardewModdingAPI;
using static FarmCafe.Framework.Utilities.Utility;
using FarmCafe.Framework.Multiplayer;

namespace FarmCafe.Framework.Managers
{
	
	internal class TableManager
	{
        internal static Dictionary<Furniture, GameLocation> TrackedTables;

		internal static void PopulateTables()
		{
			foreach (var location in CafeManager.CafeLocations)
			{
                foreach (Furniture table in location.furniture)
                {
                    if (!IsTable(table)) continue;
                    // If we already have this table object registered
                    if (TrackedTables.Keys.Any(t => t == table)) continue;

                    AddTable(table, location);
                }
            }
        }

		internal static void AddTable(Furniture table, GameLocation location)
		{
			List<Furniture> foundChairs = FindChairsAroundTable(table, location);
			if (foundChairs.Count == 0) return;
            
			UpdateChairsForTable(table, foundChairs, location);

            table.modData["FarmCafeTable"] = "T";
            TrackedTables.Add(table, location);
            Messaging.SyncTables();
            Debug.Log($"Table added. {foundChairs.Count} chairs.");
        }

        internal static void TryRemoveTable(Furniture table)
        {
            if (!TrackedTables.Keys.Contains(table))
            {
                Debug.Log("Table removed, but it wasn't a valid table counted in the manager. Bug?", LogLevel.Warn);
                return;
            }

            if (TableIsReserved(table))
            {
                Debug.Log("You shouldn't have removed that table. It was reserved!", LogLevel.Error);
            }

            TrackedTables.Remove(table);
            Messaging.SyncTables();
			table.modData.Remove("FarmCafeTable");
			table.modData.Remove("FarmCafeTableChairs");
			table.modData.Remove("FarmCafeTableIsReserved");
        }

        internal static void UpdateChairsForTable(Furniture table, List<Furniture> chairs, GameLocation location)
		{
            string chairsString = "";
            foreach (Furniture chair in chairs)
            {
                chairsString += $"{chair.TileLocation.X} {chair.TileLocation.Y} ";
            }
            table.modData["FarmCafeTableChairs"] = chairsString;
        }

		internal static List<Furniture> FindChairsAroundTable(Furniture table, GameLocation location)
		{
            int sizeY = table.getTilesHigh();
            int sizeX = table.getTilesWide();
            Point pos = table.TileLocation.ToPoint();
			List<Furniture> chairs = new List<Furniture>();

            foreach (int i in new[] { -1, sizeY })
            {
                for (int j = 0; j < sizeX; j++)
                {
                    var chairAt = location.GetFurnitureAt(new Vector2(pos.X + j, pos.Y + i));
                    if (IsChair(chairAt))
                    {
                        int rotation = chairAt.currentRotation.Value;
                        if (rotation == 0 && i == -1 || rotation == 2 && i != -1)
                        {
							chairs.Add(chairAt);
                        }
                    }
                }
            }

            foreach (int i in new[] { -1, sizeX })
            {
                for (int j = 0; j < sizeY; j++)
                {
                    var chairAt = location.GetFurnitureAt(new Vector2(pos.X + i, pos.Y + j));
                    if (IsChair(chairAt))
                    {
                        int rotation = chairAt.currentRotation.Value;
                        if (rotation == 1 && i == -1 || rotation == 3 && i != -1)
                        {
							chairs.Add(chairAt);
                        }
                    }
                }
            }

			return chairs;
        }

		internal static bool AddChairToTable(Furniture chair, Furniture table, GameLocation location)
		{
            if (table == null)
            {
                table = GetTableFromChair(chair);
                if (table == null)
                {
                    Debug.Log("There is no table for the chair placed.");
                    return false;
                }
                if (GetChairsOfTable(table).Contains(chair))
                {
                    Debug.Log("Table already knows the chair that was placed?", LogLevel.Warn);
                    return false;
                }
                Debug.Log("Adding chair to table");
            }

            List<Furniture> chairs = GetChairsOfTable(table); 
			if (chairs.Contains(chair)) {
				Debug.Log("Table already contains chair to be added");
				return false;
			}
            chairs.Add(chair);
            UpdateChairsForTable(table, chairs, location);
			return true;
		}

		internal static void TryRemoveChairFromTable(Furniture chair, Furniture table, GameLocation location)
		{
            if (table == null)
                table = GetTableFromChair(chair);

            if (table == null)
            {
                Debug.Log("A chair was removed but there was no table assigned to it.", LogLevel.Debug);
                return;
            }
            List<Furniture> chairsOnTable = TableManager.GetChairsOfTable(table);

            if (!chairsOnTable.Contains(chair))
            {
                Debug.Log("Chair was removed but the table in front of it didn't recognize it before. Bug!", LogLevel.Error);
                return;
            }

            if (TableManager.TableIsReserved(table))
            {
                Debug.Log("You shouldn't remove that chair. It's table is reserved!", LogLevel.Warn);
            }

            Debug.Log("Removing chair from table");
			List<Furniture> chairs = GetChairsOfTable(table);
			chairs.Remove(chair);
			UpdateChairsForTable(table, chairs, location);
        }

		internal static List<Furniture> GetChairsOfTable(Furniture table) {
			List<Furniture> chairs = new List<Furniture>();
			if (!table.modData.ContainsKey("FarmCafeTableChairs"))
			{
				table.modData.Add("FarmCafeTableChairs", "");
			}
			foreach (Match match in Regex.Matches(table.modData["FarmCafeTableChairs"], @"\d+\s\d+"))
			{
				if (!match.Success)	continue;

				var tile = match.ToString().Split(' ').Select(x => Int32.Parse(x)).ToArray();
				Furniture chair = Game1.getFarm().GetFurnitureAt(new Vector2(tile[0], tile[1]));
				if (chair == null)
				{
					Debug.Log("Bad chair registered in table modData", LogLevel.Error);
					continue;
				}
				chairs.Add(chair);
			}

			return chairs;
		}

        internal static Furniture TryReserveTable(int minimumSeats = 1)
        {
			foreach (var table in TrackedTables.OrderBy(x => Game1.random.Next()))
			{
				if (TableIsReserved(table.Key))
					continue;
				List<Furniture> chairs = GetChairsOfTable(table.Key);

				if (chairs?.Count < minimumSeats)
					continue;

				return table.Key;
            }

			return null;
        }

        internal static Furniture GetTableFromChair(Furniture chair)
		{
			// Get position of chair
			Vector2 tablePos = chair.TileLocation;

			// Get position of table in front of the chair
			tablePos += DirectionIntToDirectionVector(chair.currentRotation.Value) * new Vector2(1, -1);

			// Get table Furniture object
			var table = Game1.getFarm().GetFurnitureAt(tablePos);

			// Get Table object
			if (table == null || !IsTable(table))
            {
                return null;
            }
            return table;
        }

        internal static bool TableIsReserved(Furniture table)
		{
			table.modData.TryGetValue("FarmCafeTableIsReserved", out var result);
			return result == "T";
		}

		public static void FreeTables()
		{
            foreach (var table in TrackedTables.Keys)
            {
				table.modData["FarmCafeTableIsReserved"] = "F";
            }
		}
	}
}
