using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using FarmCafe.Framework.Characters;
using FarmCafe.Framework.Managers;
using FarmCafe.Framework.Models;
using FarmCafe.Locations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using static FarmCafe.Framework.Utilities.Utility;

namespace FarmCafe.Framework.Characters
{
    public enum CustomerState : byte
    {
        ExitingBus,
        Convening,
        MovingToTable,
        Sitting,
        ReadyToOrder,
        WaitingForOrder,
        Eating,
        DoneEating,
        GettingUpFromSeat,
        Leaving,
        Free,
    }

    public class Customer : NPC
    {
        [XmlElement("CustomerModel")] 
        public CustomerModel Model { get; set; }

        [XmlIgnore] 
        internal CustomerGroup Group;

        [XmlIgnore] 
        public Furniture Seat;

        [XmlIgnore]
        internal bool IsGroupLeader;
        [XmlIgnore]
        internal NetEnum<CustomerState> State = new(CustomerState.ExitingBus);
        private int busDepartTimer = 0;
        private int conveneWaitingTimer = 0;
        private int lookAroundTimer = 0;
        private int orderTimer = 0;
        private int eatingTimer = 0;
        [XmlIgnore] internal Point BusConvenePoint;

        internal delegate void LerpEnd();
        private float lerpPosition = -1f;
        private float lerpDuration = 0f;
        private Vector2 lerpStartPosition;
        private Vector2 lerpEndPosition;
        private LerpEnd lerpEndBehavior;


        private readonly NetVector2 drawOffsetForSeat = new NetVector2(new Vector2(0, 0));
        private readonly NetVector2 tableCenterForEmote = new NetVector2(new Vector2(0, 0));

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

        private bool emoteLoop;
      
        public override bool canPassThroughActionTiles() => false;

        public Customer() : base()
        {
        }

        protected override void initNetFields()
        {
            NetFields.AddFields(drawOffsetForSeat, State);
            base.initNetFields();
        }

        public Customer(CustomerModel model, string name, Point targetTile, GameLocation location)
            : base(new AnimatedSprite(model.TilesheetPath, 0, 16, 32), targetTile.ToVector2() * 64, 1, name)
        {
            willDestroyObjectsUnderfoot = true;
            collidesWithOtherCharacters.Value = false;
            eventActor = false;
            speed = 3;
            base.displayName = "Customer";
            Portrait = FarmCafe.ModHelper.ModContent.Load<Texture2D>($"assets/Portraits/{model.PortraitName}.png");

            Model = model;

            currentLocation = location;
            location.addCharacter(this);

            base.modData["CustomerData"] = "T";
        }


        public override void update(GameTime time, GameLocation location)
        {
            speed = 3; // For debug
            //Debug.Log($"Position = {getTileLocation()}");

            if (!Context.IsWorldReady) return;

            base.update(time, location);

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
                Math.Max(0f, getStandingY() / 10000f + 0.0001f));

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

            if (State == CustomerState.ReadyToOrder && IsGroupLeader)
            {
                Vector2 offset = new Vector2(0,
                    (float) Math.Round(4f * Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0)));

                OrderItem?.drawInMenu(
                    b, 
                    Game1.GlobalToLocal(tableCenterForEmote - new Vector2(18, 56) + offset), 
                    0.8f, 1f, 1f, StackDrawType.Hide, Color.White, false);

                b.Draw(
                    Game1.mouseCursors,
                    Game1.GlobalToLocal(tableCenterForEmote - new Vector2(28, 64) + offset),
                    new Rectangle(141, 465, 20, 24),
                    Color.White,
                    0f,
                    Vector2.Zero,
                    4f,
                    SpriteEffects.None,
                    0.991f);
                //b.Draw(
                //    Game1.mouseCursors,
                //    Game1.GlobalToLocal(tableCenterForEmote),
                //    new Rectangle(402, 495, 7, 16),
                //    Color.White,
                //    0f,
                //    new Vector2(1f, 4f),
                //    4f + Math.Max(0f, 0.25f - scale / 16f),
                //    SpriteEffects.None,
                //    1f);
            }
        }

        public override void tryToReceiveActiveObject(Farmer who)
        {
            if (who.ActiveObject == null || who.ActiveObject.ParentSheetIndex != OrderItem.ParentSheetIndex) 
                return;

            this.OrderReceive();
            who.reduceActiveItemByOne();
        }

        internal void LeaveBus()
        {
            if (Group.Members.Count == 1)
            {
                GoToSeat();
            }
            else
            {
                collidesWithOtherCharacters.Set(false);
                this.HeadTowards(FarmCafe.cafeManager.GetLocationFromName("BusStop"), BusConvenePoint, 2, StartConvening);
            }
        }

        internal void SetBusConvene(Point pos, int timer)
        {
            busDepartTimer = timer;
            BusConvenePoint = pos;
        }

        internal void GoToSeat()
        {
            State.Set(CustomerState.MovingToTable);
            collidesWithOtherCharacters.Set(false);
            this.HeadTowards(
                Group.TableLocation, 
                Seat.TileLocation.ToPoint(), 
                -1, 
                SitDown);
            
        }

        internal void StartConvening()
        {
            controller = null;
            conveneWaitingTimer = Game1.random.Next(500, 3000);
            State.Set(CustomerState.Convening);
            Group.GetLookingDirections();
        }


        public void FinishConvening()
        {
            State.Set(CustomerState.MovingToTable);
            if (Group.Members.Any(c => c.State.Value != CustomerState.MovingToTable))
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
            State.Set(CustomerState.Sitting);
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
            State.Set(CustomerState.ReadyToOrder);
            if (IsGroupLeader)
                tableCenterForEmote.Set(this.Group.ReservedTable.boundingBox.Center.ToVector2() + new Vector2(-8, -64));

            //OrderItem = new StardewValley.Object(746, 1).getOne();
        }
      
        internal void OrderReceive()
        {
            State.Set(CustomerState.Eating);
            if (IsGroupLeader)
                doEmote(20);
            this.eatingTimer = 2000;
        }

        internal void StartWaitForOrder()
        {
            State.Set(CustomerState.WaitingForOrder);
        }

        internal void FinishEating()
        {
            State.Set(CustomerState.Leaving);
            int direction = Game1.random.Next(2) == 0 ? (FacingDirection + 1) % 4 : (FacingDirection + 3) % 4;
            this.GetUpFromSeat(direction);
        }

        internal void DoNothingAndWait()
        {
            State.Set(CustomerState.Free);
        }

        internal void GoHome()
        {
            FarmCafe.tableManager.FreeTable(Group.ReservedTable);
            this.HeadTowards(Game1.getLocationFromName("BusStop"), FarmCafe.cafeManager.BusPosition, 0, ReachHome);
        }

        internal void ReachHome()
        {
            IsInvisible = true;
            Game1.removeCharacterFromItsLocation(this.Name);
            if (Group.Members.All(c => c.IsInvisible))
            {
                FarmCafe.cafeManager.EndGroup(Group);
            }
        }

        internal void LerpPosition(Vector2 startPos, Vector2 endPos, float duration, LerpEnd action)
        {
            lerpStartPosition = startPos;
            lerpEndPosition = endPos;
            lerpPosition = 0f;
            lerpDuration = duration;
            lerpEndBehavior = action;
        }

        //protected override void updateSlaveAnimation(GameTime time)
        //{
        //	return;
        //}

        internal void Reset()
        {
            isCharging = false;
            freezeMotion = false;
            controller = null;
            State.Set(CustomerState.Free);
        }

        internal void Debug_ShowInfo()
        {
            Debug.Log(ToString());
        }

        public new string ToString()
        {
            return $"[Customer]\n"
                   + $"Name: {Name}\n"
                   //+ $"Active path: " + this.GetCurrentPathStackShort() + "\n"
                   + $"Location: {currentLocation}, Position: {Position}, Tile: {getTileLocation()}\n"
                   + $"Bus depart timer: {busDepartTimer}, Convene timer: {conveneWaitingTimer}\n"
                   + $"State: {State}\n"
                   + $"Model: [Name: {Model?.Name}, Tilesheet: {Model?.TilesheetPath}]\n";

            //+ $"Group members: {Group.Members.Count}\n"
            //+ $"Animation: {Model.Animation.NumberOfFrames} frames, {Model.Animation.Duration}ms each, Starting {Model.Animation.StartingFrame}\n"
            //+ $"Sprite info: {Sprite} current frame = {Sprite.CurrentFrame}, "
            //+ $"Texture name = {Sprite.Texture.Name}\n"
            //+ $"Facing direction {FacingDirection}, IsMoving = {isMoving()}";
        }
    }
}