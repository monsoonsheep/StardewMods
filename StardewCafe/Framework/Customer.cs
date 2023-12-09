using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewCafe.Framework.Objects;
using VisitorFramework.Framework.Visitors;

namespace StardewCafe.Framework
{
    internal class Customer : Visitor
    {
        internal Item OrderItem { get; set; }
        internal Seat Seat;

        public override void update(GameTime time, GameLocation location)
        {
            base.update(time, location);
            if (!Context.IsWorldReady || !Context.IsMainPlayer) 
                return;
        }

    }
}
