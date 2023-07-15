using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static FarmCafe.Framework.Utilities.Utility;

namespace FarmCafe.Framework.Managers
{
	internal class TableManager
	{
		internal static List<Furniture> TablesOnFarm;

		internal static void PopulateTables()
		{
			GameLocation farm = Game1.getFarm();

			foreach (Furniture table in farm.furniture)
			{
				if (!IsTable(table)) continue;
				// If we already have this table object registered
				if (TablesOnFarm.Any(t => t == table)) continue;

				AddTable(table);
            }
        }

		internal static void AddTable(Furniture table)
		{
			List<Furniture> chairs = FindChairsAroundTable(table);
			if (chairs.Count > 0)
            {
				UpdateChairsForTable(table, chairs);
                TablesOnFarm.Add(table);
                table.modData["FarmCafeTable"] = "T";
				Debug.Log($"Table added. {chairs.Count} chairs.");
            }
        }

		internal static void UpdateChairsForTable(Furniture table, List<Furniture> chairs = null)
		{
			if (chairs == null)
			{
				chairs = FindChairsAroundTable(table);
			}
            string chairsString = "";
            foreach (Furniture chair in chairs)
            {
                chairsString += $"{chair.TileLocation.X} {chair.TileLocation.Y} ";
            }
            if (!table.modData.ContainsKey("FarmCafeTableChairs"))
            {
                table.modData.Add("FarmCafeTableChairs", "");
            }
            table.modData["FarmCafeTableChairs"] = chairsString;
        }

		internal static List<Furniture> FindChairsAroundTable(Furniture table)
		{
            GameLocation farm = Game1.getFarm();
            int sizeY = table.getTilesHigh();
            int sizeX = table.getTilesWide();
            Point pos = table.TileLocation.ToPoint();
			List<Furniture> chairs = new List<Furniture>();

            foreach (int i in new[] { -1, sizeY })
            {
                for (int j = 0; j < sizeX; j++)
                {
                    var chairAt = farm.GetFurnitureAt(new Vector2(pos.X + j, pos.Y + i));
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
                    var chairAt = farm.GetFurnitureAt(new Vector2(pos.X + i, pos.Y + j));
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

		internal static bool AddChairToTable(Furniture chair, Furniture table)
		{
			List<Furniture> chairs = GetChairsOfTable(table); 
			if (chairs.Contains(chair)) {
				Debug.Log("Table already contains chair to be added");
				return false;
			}
			var chairPos = chair.TileLocation;
            if (!table.modData.ContainsKey("FarmCafeTableChairs"))
            {
                table.modData.Add("FarmCafeTableChairs", "");
            }
            table.modData["FarmCafeTableChairs"] += $"{chairPos.X} {chairPos.Y} ";
			//Debug.Log($"{table.modData["FarmCafeTableChairs"]}");
			return true;
		}

		internal static void RemoveTable(Furniture table)
		{
            TablesOnFarm.Remove(table);
            table.modData["FarmCafeTable"] = "F";
            table.modData["FarmCafeTableChairs"] = "";
            table.modData["FarmCafeTableIsReserved"] = "F";
        }
		internal static void RemoveChairFromTable(Furniture chair, Furniture table)
		{
			List<Furniture> chairs = GetChairsOfTable(table);
			chairs.Remove(chair);
			UpdateChairsForTable(table, chairs);

        }
		internal static List<Furniture> GetChairsOfTable(Furniture table, GameLocation location = null) {
			if (location == null) 
				location = Game1.getFarm();

			List<Furniture> chairs = new List<Furniture>();
			if (!table.modData.ContainsKey("FarmCafeTableChairs"))
			{
				table.modData.Add("FarmCafeTableChairs", "");
			}
			foreach (Match match in Regex.Matches(table.modData["FarmCafeTableChairs"], @"\d+\s\d+"))
			{
				if (match.Success)
				{
					var tile = match.ToString().Split(' ').Select(x => Int32.Parse(x)).ToArray();
					Furniture chair = location.GetFurnitureAt(new Vector2(tile[0], tile[1]));
					chairs.Add(chair);
				}
			}

			return chairs;
		}

		internal static Furniture TryReserveTable()
		{
            return TablesOnFarm.Where(x => !TableIsReserved(x) && GetChairsOfTable(x).Count() > 0).OrderBy(x => Game1.random.Next()).FirstOrDefault();
        }

		internal static Furniture GetTableFromChair(Furniture chair)
		{
			// Get position of chair
			Vector2 tablePos = chair.TileLocation;

			// Get position of table in front of the chair
			tablePos += DirectionIntToDirectionVector(chair.currentRotation.Value) * new Vector2(1, -1);

			// Get table Furniture object
			var table = Game1.getFarm().getObjectAtTile((int)tablePos.X, (int)tablePos.Y) as Furniture;

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
            foreach (var table in TablesOnFarm)
            {
				table.modData["FarmCafeTableIsReserved"] = "F";
            }
		}

	}
}
