using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using FarmCafe.Framework.Characters;
using FarmCafe.Framework.Managers;
using FarmCafe.Framework.Models;
using FarmCafe.Framework.Objects;
using FarmCafe.Locations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using static FarmCafe.Framework.Utilities.Utility;
using static FarmCafe.Framework.Characters.CustomerState;

namespace FarmCafe.Framework.Characters
{
    public enum CustomerState : byte
    {
        ExitingBus,
        Convening,
        MovingToTable,
        Sitting,
        OrderReady,
        WaitingForOrder,
        Eating,
        DoneEating,
        GettingUpFromSeat,
        Leaving,
        Free,
    }

    public class Customer : NPC
    {
        [XmlIgnore] 
        internal CustomerGroup Group;

        [XmlIgnore] 
        public ISeat Seat;

        [XmlIgnore]
        internal NetBool IsGroupLeader = new NetBool();

        [XmlIgnore]
        internal NetEnum<CustomerState> State = new(ExitingBus);
        private int busDepartTimer = 0;
        private int conveneWaitingTimer = 0;
        private int lookAroundTimer = 0;
        private int orderTimer = 0;
        private int eatingTimer = 0;

        [XmlIgnore] 
        internal Point BusConvenePoint;

        internal delegate void LerpEnd();
        private float lerpPosition = -1f;
        private float lerpDuration = 0f;
        private Vector2 lerpStartPosition;
        private Vector2 lerpEndPosition;
        private LerpEnd lerpEndBehavior;


        private readonly NetVector2 drawOffsetForSeat = new NetVector2(new Vector2(0, 0));

        internal Vector2 tableCenterForEmote = new Vector2(0, 0);

        [XmlIgnore] 
        internal List<int> LookingDirections = new() { 0, 1, 3 };

        [XmlIgnore] 
        internal Item OrderItem { get; set; }

        [XmlIgnore]
        internal bool FreezeMotion
        {
            get => freezeMotion;
            set => freezeMotion = value;
        }

        public Customer() : base()
        {
        }

        public Customer(string name, Point targetTile, GameLocation location, Texture2D portrait, string tileSheetPath)
            : base(new AnimatedSprite(tileSheetPath, 0, 16, 32), targetTile.ToVector2() * 64, 1, name)
        {
            willDestroyObjectsUnderfoot = true;
            collidesWithOtherCharacters.Value = false;
            eventActor = false;
            speed = 3;
            base.displayName = "Customer";
            Portrait = portrait;

            currentLocation = location;
            location.addCharacter(this);

            base.modData["CustomerData"] = "T";
        }

        #region Overrides

        public override bool canPassThroughActionTiles() => false;

        protected override void initNetFields()
        {
            NetFields.AddFields(drawOffsetForSeat, State, IsGroupLeader);
            base.initNetFields();
        }

        public override void update(GameTime time, GameLocation location)
        {
            base.update(time, location);
            if (!Context.IsWorldReady || !Context.IsMainPlayer) return;

            speed = 3; // For debug

            // Spawning and waiting to leave the bus
            if (busDepartTimer > 0)
            {
                busDepartTimer -= time.ElapsedGameTime.Milliseconds;
                if (busDepartTimer <= 0)
                    this.LeaveBus();
            }

            // Convening with group members
            else if (conveneWaitingTimer > 0)
            {
                conveneWaitingTimer -= time.ElapsedGameTime.Milliseconds;
                lookAroundTimer -= time.ElapsedGameTime.Milliseconds;
                if (lookAroundTimer <= 0)
                {
                    this.LookAround();
                    lookAroundTimer += Game1.random.Next(400, 1000);
                }

                if (conveneWaitingTimer <= 0)
                {
                    this.FinishConvening();
                    conveneWaitingTimer = 0;
                    lookAroundTimer = 0;
                }
            }

            // Sitting at table, waiting to order
            else if (orderTimer > 0)
            {
                orderTimer -= time.ElapsedGameTime.Milliseconds;

                if (orderTimer <= 0)
                {
                    this.ReadyToOrder();
                }
            }

            else if (eatingTimer > 0)
            {
                eatingTimer -= time.ElapsedGameTime.Milliseconds;

                if (eatingTimer <= 0)
                {
                    this.FinishEating();
                }
            }

            // Lerping position for sitting
            if (lerpPosition >= 0f)
            {
                lerpPosition += (float)time.ElapsedGameTime.TotalSeconds;

                // Clamping
                if (lerpPosition >= lerpDuration)
                {
                    lerpPosition = lerpDuration;
                }

                Position = new Vector2(
                    Utility.Lerp(lerpStartPosition.X, lerpEndPosition.X, lerpPosition / lerpDuration),
                    Utility.Lerp(lerpStartPosition.Y, lerpEndPosition.Y, lerpPosition / lerpDuration)
                );

                if (lerpPosition >= lerpDuration)
                {
                    lerpPosition = -1f;
                    lerpEndBehavior?.Invoke();
                }
            }
        }

        public override void draw(SpriteBatch b, float alpha = 1f)
        {
            if (IsInvisible)
                return;

            b.Draw(Sprite.Texture,
                getLocalPosition(Game1.viewport) +
                new Vector2(GetSpriteWidthForPositioning() * 4 / 2, GetBoundingBox().Height / 2) + drawOffsetForSeat,
                Sprite.SourceRect,
                Color.White * alpha,
                rotation,
                new Vector2(Sprite.SpriteWidth / 2, Sprite.SpriteHeight * 3f / 4f),
                Math.Max(0.2f, scale) * 4f,
                SpriteEffects.None,
                Math.Max(0f, getStandingY() / 10000f + ((getTileLocation() == Seat.TileLocation) ? 0.0035f : 0.0001f)));

            if (Breather && shakeTimer <= 0 && !swimming && Sprite.currentFrame < 16 && !farmerPassesThrough)
            {
                Rectangle sourceRect = Sprite.SourceRect;
                sourceRect.Y += Sprite.SpriteHeight / 2 + Sprite.SpriteHeight / 32;
                sourceRect.Height = Sprite.SpriteHeight / 4;
                sourceRect.X += Sprite.SpriteWidth / 4;
                sourceRect.Width = Sprite.SpriteWidth / 2;
                var vector = new Vector2(Sprite.SpriteWidth * 4 / 2, 8f);
                if (Age == 2)
                {
                    sourceRect.Y += Sprite.SpriteHeight / 6 + 1;
                    sourceRect.Height /= 2;
                    vector.Y += Sprite.SpriteHeight / 8 * 4;
                }
                else if (Gender == 1)
                {
                    sourceRect.Y++;
                    vector.Y -= 4f;
                    sourceRect.Height /= 2;
                }

                float num = (float) Math.Max(0f,
                    Math.Ceiling(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 600.0 + 200f)) / 4f);

                b.Draw(Sprite.Texture,
                    getLocalPosition(Game1.viewport) + vector + drawOffsetForSeat,
                    sourceRect,
                    Color.White * alpha,
                    rotation,
                    new Vector2(sourceRect.Width / 2, sourceRect.Height / 2 + 1),
                    Math.Max(0.2f, scale) * 4f + num,
                    flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                    Math.Max(0f, drawOnTop ? 0.992f : (getStandingY() / 10000f + 0.001f)));
            }

            if (IsEmoting)
            {
                float layer = getStandingY() / 10000f;
                Vector2 localPosition = getLocalPosition(Game1.viewport);
                localPosition.Y -= 32 + Sprite.SpriteHeight * 4;

                b.Draw(Game1.emoteSpriteSheet, localPosition,
                    new Rectangle(CurrentEmoteIndex * 16 % Game1.emoteSpriteSheet.Width,
                        CurrentEmoteIndex * 16 / Game1.emoteSpriteSheet.Width * 16, 16, 16), Color.White, 0f,
                    Vector2.Zero, 4f, SpriteEffects.None, layer);
            }

            if (State == OrderReady && IsGroupLeader.Value)
            {
                Vector2 offset = new Vector2(0,
                    (float) Math.Round(4f * Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0)));

                b.Draw(
                    Game1.mouseCursors,
                    Game1.GlobalToLocal(tableCenterForEmote) + offset,
                    new Rectangle(402, 495, 7, 16),
                    Color.Crimson,
                    0f,
                    new Vector2(1f, 4f),
                    4f + Math.Max(0f, 0.25f - scale / 16f),
                    SpriteEffects.None,
                    1f);
            }
        }

        public override void tryToReceiveActiveObject(Farmer who)
        {
            if (who.ActiveObject == null || who.ActiveObject.ParentSheetIndex != OrderItem.ParentSheetIndex) 
                return;

            this.OrderReceive();
            who.reduceActiveItemByOne();
        }

        #endregion

        #region Behavior
        internal void LeaveBus()
        {
            if (Group.Members.Count == 1)
            {
                GoToSeat();
            }
            else
            {
                collidesWithOtherCharacters.Set(false);
                this.HeadTowards(FarmCafe.GetLocationFromName("BusStop"), BusConvenePoint, 2, StartConvening);
            }
        }

        internal void SetBusConvene(Point pos, int timer)
        {
            busDepartTimer = timer;
            BusConvenePoint = pos;
        }

        internal void GoToSeat()
        {
            State.Set(MovingToTable);
            collidesWithOtherCharacters.Set(false);
            this.HeadTowards(
                Group.ReservedTable.CurrentLocation, 
                Seat.TileLocation.ToPoint(), 
                -1, 
                SitDown);
            
        }

        internal void StartConvening()
        {
            controller = null;
            conveneWaitingTimer = Game1.random.Next(500, 3000);
            State.Set(Convening);
            Group.GetLookingDirections();
        }

        internal void FinishConvening()
        {
            State.Set(MovingToTable);
            if (Group.Members.Any(c => c.State.Value != MovingToTable))
                return;

            foreach (Customer mate in Group.Members)
                mate.GoToSeat();
        }

        internal void LookAround()
        {
            faceDirection(LookingDirections[Game1.random.Next(LookingDirections.Count)]);
        }

        internal void SitDown()
        {
            State.Set(Sitting);
            controller = null;
            isCharging = true;

            LerpPosition(
                Position, 
                Seat.TileLocation * 64f, 
                0.15f, 
                () => this.orderTimer = Game1.random.Next(300, 500));

            int sittingDirection = Seat.GetSittingDirection();
            faceDirection(sittingDirection);
           
            Vector2 vec = sittingDirection switch
            {
                0 => new Vector2(0f, -24f), // up
                1 => new Vector2(12f, -8f), // right
                2 => new Vector2(0f, 0f), // down 
                3 => new Vector2(-12f, -8f), // left
                _ => drawOffsetForSeat
            };

            drawOffsetForSeat.Set(vec);
            Breather = true;
        }

        internal void GetUpFromSeat(int direction)
        {
            drawOffsetForSeat.Set(new Vector2(0, 0));
            var nextPos = Position + (DirectionIntToDirectionVector(direction) * 64f);
            LerpPosition(
                Position,
                nextPos,
                0.15f, 
                GoHome);
        }

        internal void ReadyToOrder()
        {
            State.Set(OrderReady);
            if (IsGroupLeader)
                tableCenterForEmote = this.Group.ReservedTable.GetCenter() + new Vector2(-8, -64);

            Multiplayer.UpdateCustomerInfo(this, nameof(OrderItem), OrderItem.ParentSheetIndex);
            Multiplayer.UpdateCustomerInfo(this, nameof(tableCenterForEmote), tableCenterForEmote.ToString());

        }
      
        internal void OrderReceive()
        {
            State.Set(Eating);
            if (IsGroupLeader)
                doEmote(20);
            this.eatingTimer = 2000;
        }

        internal void StartWaitForOrder()
        {
            State.Set(WaitingForOrder);
        }

        internal void FinishEating()
        {
            State.Set(Leaving);
            int direction = Game1.random.Next(2) == 0 ? (FacingDirection + 1) % 4 : (FacingDirection + 3) % 4;
            this.GetUpFromSeat(direction);
        }

        internal void DoNothingAndWait()
        {
            State.Set(Free);
        }

        internal void GoHome()
        {
            Group.ReservedTable.Free();
            this.HeadTowards(Game1.getLocationFromName("BusStop"), FarmCafe.CafeManager.BusPosition, 0, ReachHome);
        }

        internal void ReachHome()
        {
            IsInvisible = true;
            Game1.removeCharacterFromItsLocation(this.Name);
            if (Group.Members.All(c => c.IsInvisible))
                FarmCafe.CafeManager.EndGroup(Group);
            
        }

        #endregion

        internal void LerpPosition(Vector2 startPos, Vector2 endPos, float duration, LerpEnd action)
        {
            lerpStartPosition = startPos;
            lerpEndPosition = endPos;
            lerpPosition = 0f;
            lerpDuration = duration;
            lerpEndBehavior = action;
        }

        internal void Reset()
        {
            isCharging = false;
            freezeMotion = false;
            controller = null;
            State.Set(Free);
        }

        public override string ToString()
        {
            return $"[Customer]\n"
                   + $"Name: {Name}\n"
                   //+ $"Active path: " + this.GetCurrentPathStackShort() + "\n"
                   + $"Location: {currentLocation}, Position: {Position}, Tile: {getTileLocation()}\n"
                   + $"Bus depart timer: {busDepartTimer}, Convene timer: {conveneWaitingTimer}\n"
                   + $"State: {State}\n";

            //+ $"Group members: {Group.Members.Count}\n"
            //+ $"Animation: {Model.Animation.NumberOfFrames} frames, {Model.Animation.Duration}ms each, Starting {Model.Animation.StartingFrame}\n"
            //+ $"Sprite info: {Sprite} current frame = {Sprite.CurrentFrame}, "
            //+ $"Texture name = {Sprite.Texture.Name}\n"
            //+ $"Facing direction {FacingDirection}, IsMoving = {isMoving()}";
        }
    }
}