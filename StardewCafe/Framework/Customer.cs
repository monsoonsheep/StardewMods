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
using FarmCafe.Framework;
using FarmCafe.Framework.Characters;

namespace StardewCafe.Framework
{
    internal class Customer : Visitor
    {
        internal Item OrderItem { get; set; }
        internal event Action<Visitor> OnFinishedDined;

        [XmlIgnore] 
        internal Seat Seat;

        protected int orderTimer;
        protected int eatingTimer;
        
        public override void update(GameTime time, GameLocation location)
        {
            base.update(time, location);

            if (!Context.IsWorldReady || !Context.IsMainPlayer) return;

            if (orderTimer > 0)
            {
                orderTimer -= time.ElapsedGameTime.Milliseconds;

                if (orderTimer <= 0)
                {
                    // READY TO ORDER
                }
            }

            else if (eatingTimer > 0)
            {
                eatingTimer -= time.ElapsedGameTime.Milliseconds;

                if (eatingTimer <= 0)
                {
                    this.GetUpFromSeat();
                }
            }
        }

    }
}
