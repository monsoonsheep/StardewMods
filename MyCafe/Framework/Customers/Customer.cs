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
        internal NPC OriginalNpc = null;
        internal Seat ReservedSeat = null;

        private Vector2 lerpStartPosition;
        private Vector2 lerpEndPosition;
        private float lerpPosition = -1f;
        private float lerpDuration = -1f;


        public Customer(string name, Vector2 position, string location, AnimatedSprite sprite, Texture2D portrait)
        : base(sprite, position, location, 2, name, portrait, eventActor: true)
        {
            portraitOverridden = true;
        }

        public override void update(GameTime gameTime, GameLocation location)
        {
            base.update(gameTime, location);
            speed = 5;

            if (lerpPosition >= 0f)
            {
                lerpPosition += (float) gameTime.ElapsedGameTime.TotalSeconds;
                if (lerpPosition >= lerpDuration)
                {
                    lerpPosition = lerpDuration;
                }
                base.Position = new Vector2(StardewValley.Utility.Lerp(lerpStartPosition.X, lerpEndPosition.X, lerpPosition / lerpDuration), StardewValley.Utility.Lerp(lerpStartPosition.Y, lerpEndPosition.Y, lerpPosition / lerpDuration));
                if (lerpPosition >= lerpDuration)
                {
                    lerpPosition = -1f;
                }
            }
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

        internal void SitDown(int direction) {
            Vector2 sitPosition = base.Position + (Utility.DirectionIntToDirectionVector(direction) * 64f);
            LerpPosition(base.Position, sitPosition, 0.2f);
        }

        public void LerpPosition(Vector2 start_position, Vector2 end_position, float duration)
        {
            lerpStartPosition = start_position;
            lerpEndPosition = end_position;
            lerpPosition = 0f;
            lerpDuration = duration;
        }
    }
}
