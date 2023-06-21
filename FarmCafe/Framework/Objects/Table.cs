using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using System.Collections.Generic;
using System.Linq;
using static FarmCafe.Framework.Utilities.Utility;


namespace FarmCafe.Framework.Objects
{
	internal class Table
	{
		internal List<Furniture> Chairs;
		internal Furniture TableObject;
		internal bool isReserved;

		public Table(Furniture tableObject)
		{
			Chairs = new List<Furniture>();
			TableObject = tableObject;
		}

		public void UpdateChairs()
		{
			var farm = Game1.getFarm();
			List<Furniture> chairsOnTable = new List<Furniture>();

			int sizeY = TableObject.getTilesHigh();
			int sizeX = TableObject.getTilesWide();
			Point pos = TableObject.TileLocation.ToPoint();

			Chairs = new List<Furniture>();


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
							Chairs.Add(chairAt);
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
							Chairs.Add(chairAt);
						}
					}
				}
			}

			// shuffle list
			Chairs = Chairs.OrderBy(x => Game1.random.Next()).ToList();
		}

		internal void AddChair(Furniture chair)
		{
			Chairs.Add(chair);
		}

		internal void RemoveChair(Furniture chair)
		{
			Chairs.Remove(chair);
		}

	}
}
