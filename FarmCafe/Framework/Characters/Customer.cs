using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using FarmCafe.Framework.Characters;
using FarmCafe.Framework.Managers;
using FarmCafe.Framework.Models;
using FarmCafe.Framework.Objects;
using FarmCafe.Framework.Locations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using xTile.Dimensions;
using xTile.ObjectModel;
using static FarmCafe.Framework.Utilities.Utility;
using static FarmCafe.Framework.Characters.CustomerState;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

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

    public partial class Customer : NPC
    {
        [XmlIgnore] internal NPC OriginalNpc;

        [XmlIgnore] internal CustomerGroup Group;

        [XmlIgnore] internal Seat Seat;

        [XmlIgnore] internal NetBool IsGroupLeader = new NetBool();

        [XmlIgnore] internal NetBool IsSitting = new NetBool();
        [XmlIgnore] internal NetEnum<CustomerState> State = new(ExitingBus);

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

        internal Vector2 TableCenterForEmote = new Vector2(0, 0);

        [XmlIgnore] internal List<int> LookingDirections = new() { 0, 1, 3 };

        [XmlIgnore] internal Item OrderItem { get; set; }

        [XmlIgnore]
        internal bool FreezeMotion
        {
            get => freezeMotion;
            set => freezeMotion = value;
        }

        public Customer() : base()
        {
        }

        public Customer(string name, Point targetTile, GameLocation location, AnimatedSprite sprite)
            : base(sprite, targetTile.ToVector2() * 64, 1, name)
        {
            willDestroyObjectsUnderfoot = true;
            collidesWithOtherCharacters.Value = false;
            eventActor = false;
            speed = 3;

            if (name.StartsWith("Customer"))
                base.displayName = "Customer";

            currentLocation = location;
            location.addCharacter(this);
        }

        public Customer(NPC npc) : base(npc.Sprite, npc.getTileLocation(), npc.DefaultMap, npc.FacingDirection, npc.Name, npc.datable.Value, null, npc.Portrait)
        {
            IsInvisible = false;
            followSchedule = false;
            ignoreScheduleToday = true;
            isSleeping.Set(false);

            this.syncedPortraitPath.Set(npc.syncedPortraitPath.Value);
            this.lastSeenMovieWeek.Set(npc.lastSeenMovieWeek.Value);
            this.Portrait = npc.Portrait;
            this.Breather = npc.Breather;
            this.Schedule = npc.Schedule;

            this.currentLocation = npc.currentLocation;
            this.Position = npc.Position;
            base.faceDirection(npc.FacingDirection);

            this.currentLocation.addCharacter(this);

            base.reloadData();

            this.OriginalNpc = npc;


            // Problem: We can't create a good enough copy of an NPC in the form of a customer. 
            // We'd have to copy over a bunch of fields and props, so the player can interact with them the normal way,
            // like give gifts and propose and even get custom modded behavior.
        }

        #region Overrides

        public override bool shouldCollideWithBuildingLayer(GameLocation location) => true;

        public override bool canPassThroughActionTiles() => false;

        protected override void initNetFields()
        {
            NetFields.AddFields(drawOffsetForSeat, State, IsGroupLeader, IsSitting);
            base.initNetFields();
        }

        public override void update(GameTime time, GameLocation location)
        {
            base.update(time, location);
            if (!Context.IsWorldReady || !Context.IsMainPlayer) return;

            PropertyValue propertyValue = null;
            location.map.GetLayer("Buildings").PickTile(nextPositionPoint(), Game1.viewport.Size)?.Properties.TryGetValue("Action", out propertyValue);
            var strArray = propertyValue?.ToString().Split(Utility.CharSpace);
            if (strArray == null)
            {
                var standingXy2 = getStandingXY();
                location.map.GetLayer("Buildings").PickTile(new Location(standingXy2.X, standingXy2.Y), Game1.viewport.Size)?.Properties
                    .TryGetValue("Action", out propertyValue);
                strArray = propertyValue?.ToString().Split(Utility.CharSpace);
            }
            if (strArray is { Length: >= 1 } && strArray[0].Contains("Door"))
            {
                location.openDoor(new Location(nextPositionPoint().X / 64, nextPositionPoint().Y / 64), Game1.player.currentLocation.Equals(location));
            }

            speed = 4; // For debug

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
                Math.Max(0f, getStandingY() / 10000f + ((IsSitting.Value is true) ? 0.0035f : 0.0001f)));

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

                float num = (float)Math.Max(0f,
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
        }

        public override void MovePosition(GameTime time, xTile.Dimensions.Rectangle viewport, GameLocation location)
        {
            if (location == null)
                return;

            if (moveUp)
            {
                if (location.isCollidingPosition(nextPosition(0), viewport, isFarmer: false, 0, glider: false, this) && !isCharging)
                {
                    if (!location.isTilePassable(nextPosition(0), viewport))
                        // TODO: Repath
                        Halt();
                    else
                    {
                        if (location.characterDestroyObjectWithinRectangle(nextPosition(0), showDestroyedObject: true))
                        {
                            doEmote(12);
                            position.Y -= speed + addedSpeed;
                        }
                        else
                        {
                            blockedInterval += time.ElapsedGameTime.Milliseconds;
                        }
                    }
                }
                else
                {
                    position.Y -= speed + addedSpeed;
                    if (!ignoreMovementAnimation)
                    {
                        Sprite.AnimateUp(time, (speed - 2 + addedSpeed) * -25, Utility.isOnScreen(getTileLocationPoint(), 1, location) ? "Cowboy_Footstep" : "");
                        faceDirection(0);
                    }
                }
            }
            else if (moveRight)
            {
                if (location.isCollidingPosition(nextPosition(1), viewport, isFarmer: false, 0, glider: false, this) && !isCharging)
                {
                    if (!location.isTilePassable(nextPosition(1), viewport))
                        Halt();
                    else
                    {
                        if (location.characterDestroyObjectWithinRectangle(nextPosition(1), showDestroyedObject: true))
                        {
                            doEmote(12);
                            position.X += speed + addedSpeed;
                        }
                        else
                        {
                            blockedInterval += time.ElapsedGameTime.Milliseconds;
                        }
                    }
                }
                else
                {
                    position.X += speed + addedSpeed;
                    if (!ignoreMovementAnimation)
                    {
                        Sprite.AnimateRight(time, (speed - 2 + addedSpeed) * -25, Utility.isOnScreen(getTileLocationPoint(), 1, location) ? "Cowboy_Footstep" : "");
                        faceDirection(1);
                    }
                }
            }
            else if (moveDown)
            {
                if (location.isCollidingPosition(nextPosition(2), viewport, isFarmer: false, 0, glider: false, this) && !isCharging)
                {
                    if (!location.isTilePassable(nextPosition(2), viewport))
                        Halt();
                    else
                    {
                        if (location.characterDestroyObjectWithinRectangle(nextPosition(2), showDestroyedObject: true))
                        {
                            doEmote(12);
                            position.Y += speed + addedSpeed;
                        }
                        else
                        {
                            blockedInterval += time.ElapsedGameTime.Milliseconds;
                        }
                    }
                }
                else
                {
                    position.Y += speed + addedSpeed;
                    if (!ignoreMovementAnimation)
                    {
                        Sprite.AnimateDown(time, (speed - 2 + addedSpeed) * -25, Utility.isOnScreen(getTileLocationPoint(), 1, location) ? "Cowboy_Footstep" : "");
                        faceDirection(2);
                    }
                }
            }
            else if (moveLeft)
            {
                if (location.isCollidingPosition(nextPosition(3), viewport, isFarmer: false, 0, glider: false, this) && !isCharging)
                {
                    if (!location.isTilePassable(nextPosition(3), viewport))
                        Halt();
                    else
                    {
                        if (location.characterDestroyObjectWithinRectangle(nextPosition(3), showDestroyedObject: true))
                        {
                            doEmote(12);
                            position.X -= speed + addedSpeed;
                        }
                        else
                        {
                            blockedInterval += time.ElapsedGameTime.Milliseconds;
                        }
                    }
                }
                else
                {
                    position.X -= speed + addedSpeed;
                    if (!ignoreMovementAnimation)
                    {
                        Sprite.AnimateLeft(time, (speed - 2 + addedSpeed) * -25, Utility.isOnScreen(getTileLocationPoint(), 1, location) ? "Cowboy_Footstep" : "");
                        faceDirection(3);
                    }
                }
            }
            else
            {
                Sprite.animateOnce(time);
            }
            if (blockedInterval >= 3000 && blockedInterval <= 3750)
            {
                doEmote((Game1.random.NextDouble() < 0.5) ? 8 : 40);
                blockedInterval = 3750;
            }
            else if (blockedInterval >= 5000)
            {
                isCharging = true;
                blockedInterval = 0;
            }
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