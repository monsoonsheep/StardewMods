using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using FarmCafe.Framework.Characters;
using SolidFoundations.Framework.Models.ContentPack;
using StardewValley.Characters;
using FarmCafe.Framework.Customers;

namespace FarmCafe.Locations
{
    public class CafeLocation : DecoratableLocation
    {
        public CafeLocation()
        {

        }

        public CafeLocation(string mapPath, string name)
            : base(mapPath, name)
        {
        }

        public override void cleanupBeforeSave()
        {
            for (var i = characters.Count - 1; i >= 0; i--)
            {
                if (characters[i] is Customer) 
                {
                    Debug.Log("Removing character");
                    characters.RemoveAt(i);
                }
            }
        }

        public override void DayUpdate(int dayOfMonth)
        {
            base.DayUpdate(dayOfMonth);
            foreach (var item in this.furniture)
            {
                item.updateDrawPosition();
            }
        }
    }
}
