using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

using System.Collections.Generic;
using static FarmCafe.Framework.Characters.Customer;
using System.Linq;
using static FarmCafe.Framework.Utilities.Utility;
using FarmCafe.Framework.Managers;
using System.Runtime.CompilerServices;
using StardewValley.Objects;
using StardewValley.Buildings;
using System;
using System.IO;
using FarmCafe.Framework.Characters;
using StardewValley.Locations;

namespace FarmCafe.Framework.Customers
{
	internal static class CustomerBehavior
	{
        
        internal static string GetCurrentPathStack(this Customer me)
        {
            return string.Join(" - ", me.controller.pathToEndPoint);
        }

        internal static string GetCurrentPathStackShort(this Customer me)
        {
            if (me.controller == null)
            {
                return "No controller";
            }

            if (me.controller.pathToEndPoint == null)
            {
                return "No path";
            }
            return $"{me.controller.pathToEndPoint.Count()} nodes: {me.controller.pathToEndPoint.First()} --> {me.controller.pathToEndPoint.Last()}";
        }

    }
}
