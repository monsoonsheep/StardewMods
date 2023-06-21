using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Buildings;
using StardewValley.Locations;
using System.Collections.Generic;

namespace FarmCafe.Framework.Interfaces
{
	public interface ISolidFoundationsApi
	{
		public void AddBuildingFlags(Building building, List<string> flags, bool isTemporary = true);
		public void RemoveBuildingFlags(Building building, List<string> flags);
		public bool DoesBuildingHaveFlag(Building building, string flag);
		public KeyValuePair<bool, string> PlaceBuilding(string modelIdCaseSensitive, BuildableGameLocation location, Vector2 tileLocation);
		public KeyValuePair<bool, string> GetBuildingTexturePath(string modelIdCaseInsensitive);
		public KeyValuePair<bool, Texture2D> GetBuildingTexture(string modelIdCaseSensitive);
	}
}
