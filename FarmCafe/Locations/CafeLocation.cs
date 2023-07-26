using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarmCafe.Locations
{
    public class CafeLocation : GameLocation
    {
        List<Furniture> Tables;
        public CafeLocation()
        {
           
        }

        public CafeLocation(string mapPath, string name) : base(mapPath, name)
        {
            Tables = new List<Furniture>();
        }
    }
}
