using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.Framework.Objects;
using Netcode;
using StardewValley;

namespace MyCafe.Framework.Customers
{
    internal class Customer : NPC
    {
        internal NPC OriginalNpc;

        public Customer(string name, Vector2 position, string location, AnimatedSprite sprite, Texture2D portrait)
        : base(sprite, position, location, 2, name, portrait, eventActor: true)
        {

        }

        public override void update(GameTime gameTime, GameLocation location)
        {
            base.update(gameTime, location);
            speed = 5;
        }

        internal Vector2 GetSeatPosition()
        {
            if (base.modData.TryGetValue("MonsoonSheep.MyCafe_ModDataSeatPos", out var result))
            {
                var split = result.Split(' ');
                Vector2 v = new Vector2(int.Parse(split[0]), int.Parse(split[1]));
                return v;
            }

            
            return Vector2.Zero;
        }

        internal string GetOrderItem()
        {
            return base.modData.TryGetValue("MonsoonSheep.MyCafe_ModDataOrderItem", out var result) ? result : null;
        }
    }
}
