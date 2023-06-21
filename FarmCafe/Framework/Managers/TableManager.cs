using FarmCafe.Framework.Objects;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using System.Collections.Generic;
using System.Linq;
using static FarmCafe.Framework.Utilities.Utility;

namespace FarmCafe.Framework.Managers
{
	internal class TableManager
	{
		internal static List<Table> TablesOnFarm;

		internal static void PopulateTables()
		{
			GameLocation farm = Game1.getFarm();

			foreach (Furniture furniture in farm.furniture)
			{
				if (!IsTable(furniture)) continue;
				// If we already have this table object registered
				if (TablesOnFarm.Any(t => t.TableObject == furniture)) continue;

				AddTable(furniture);
			}
		}

		internal static void AddTable(Furniture furniture)
		{
			var table = new Table(furniture);
			table.UpdateChairs();

			if (table.Chairs.Count == 0)
			{
				return;
			}

			Debug.Log($"Got table at {table.TableObject.TileLocation}. Has {table.Chairs.Count} chairs.");
			TablesOnFarm.Add(table);
			TablesOnFarm = TablesOnFarm.OrderBy(x => Game1.random.Next()).ToList();
		}


		internal static Table TryReserveTable()
		{
			return TablesOnFarm.Where(x => !x.isReserved && x.Chairs.Count > 0).OrderBy(x => Game1.random.Next()).FirstOrDefault();
		}

		internal static Table GetTableFromChair(Furniture chair)
		{

			// Get position of chair
			Vector2 tablePos = chair.TileLocation;

			// Get position of table in front of the chair
			tablePos += DirectionIntToDirectionVector(chair.currentRotation.Value) * new Vector2(1, -1);

			// Get table Furniture object
			var tableFurniture = Game1.getFarm().getObjectAtTile((int)tablePos.X, (int)tablePos.Y) as Furniture;

			// Get Table object
			if (tableFurniture != null && IsTable(tableFurniture))
			{
				return GetTableFromTableObject(tableFurniture);
			}

			Table table = new Table(tableFurniture);
			table.AddChair(chair);
			return table;
		}

		internal static Table GetTableFromTableObject(Furniture tableObj)
		{
			foreach (Table table in TablesOnFarm)
			{
				if (table.TableObject == tableObj)
					return table;
			}

			return null;
		}


		public static void FreeTables()
		{
			foreach (var table in TablesOnFarm)
			{
				table.isReserved = false;
			}
		}

	}
}
