﻿using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using FarmCafe.Framework.Objects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;
using static FarmCafe.Framework.Utility;
using SUtility = StardewValley.Utility;
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

    internal struct LocationPathDescription
    {
        public int time;
        public string locationName;
        public int steps;

        public LocationPathDescription(int time, string locationName, int steps)
        {
            (this.time, this.locationName, this.steps) = (time, locationName, steps);
        }
    }

    public partial class Customer : NPC
    {
        [XmlIgnore] 
        internal NPC OriginalNpc;

        [XmlIgnore] 
        internal List<LocationPathDescription> OriginalScheduleLocations;

        [XmlIgnore] 
        internal CustomerGroup Group;

        [XmlIgnore] 
        internal NetBool IsGroupLeader = new NetBool();

        [XmlIgnore] 
        internal Seat Seat;

        [XmlIgnore] 
        internal NetBool IsSitting = new NetBool();

        private readonly NetVector2 drawOffsetForSeat = new NetVector2(new Vector2(0, 0));

        [XmlIgnore] 
        internal NetEnum<CustomerState> State = new(ExitingBus);

        private int busDepartTimer;
        private int conveneWaitingTimer;
        private int lookAroundTimer;
        private int orderTimer;
        private int eatingTimer;

        [XmlIgnore] 
        internal Point BusConvenePoint;

        internal delegate void LerpEnd();

        private float lerpPosition = -1f;
        private float lerpDuration = 0f;
        private Vector2 lerpStartPosition;
        private Vector2 lerpEndPosition;
        private LerpEnd lerpEndBehavior;

        [XmlIgnore] 
        internal List<int> LookingDirections = new() { 0, 1, 3 };

        internal Item OrderItem { get; set; }

        internal event Action<Customer> OnFinishedDined;

        [XmlIgnore]
        internal bool FreezeMotion
        {
            get => freezeMotion;
            set => freezeMotion = value;
        }

        public Customer() : base()
        {
        }

        public Customer(string name, Point targetTile, GameLocation location, AnimatedSprite sprite, Texture2D portrait)
            : base(sprite, targetTile.ToVector2() * 64, 1, name)
        {
            willDestroyObjectsUnderfoot = true;
            collidesWithOtherCharacters.Value = false;
            eventActor = false;
            speed = 3;

            Portrait = portrait;
            if (name.StartsWith("Customer"))
                base.displayName = "Customer";

            currentLocation = location;
            location.addCharacter(this);
            
        }

        public Customer(NPC npc) : base(npc.Sprite, npc.Position, npc.DefaultMap, npc.FacingDirection, npc.Name, npc.datable.Value, null, npc.Portrait)
        {
            IsInvisible = false;
            followSchedule = false;
            ignoreScheduleToday = true;
            eventActor = true;
            isSleeping.Set(false);
            base.Sprite.StopAnimation();

            this.lastSeenMovieWeek.Set(npc.lastSeenMovieWeek.Value);
            this.Portrait = npc.Portrait;
            this.Breather = npc.Breather;
            base.CurrentDialogue = npc.CurrentDialogue;
            this.TryLoadSchedule();
            this.currentLocation = npc.currentLocation;
            this.Position = npc.Position;
            base.faceDirection(npc.FacingDirection);

            base.reloadData();

            this.currentLocation.addCharacter(this);

            npc.currentLocation.characters.Remove(npc);

            this.OriginalNpc = npc;
            this.OriginalScheduleLocations = GetLocationRouteFromSchedule(npc);

            // Problem: We can't create a good enough copy of an NPC in the form of a customer. 
            // We'd have to copy over a bunch of fields and props, so the player can interact with them the normal way,
            // like give gifts and propose and even get custom modded behavior.
        }

        public override bool shouldCollideWithBuildingLayer(GameLocation location) => true;

        public override bool canPassThroughActionTiles() => true;

        protected override void initNetFields()
        {
            base.initNetFields();
            NetFields.AddField(drawOffsetForSeat).AddField(State).AddField(IsGroupLeader).AddField(IsSitting);
        }

        public override void update(GameTime time, GameLocation location)
        {
            base.update(time, location);

            if (!Context.IsWorldReady || !Context.IsMainPlayer) return;
            //if (moveUp)
            //    Logger.Log($"up ");
            //if (moveRight)
            //    Logger.Log($"right");
            //if (moveDown)
            //    Logger.Log($"down");
            //if (moveLeft)
            //    Logger.Log($"left");

            // Regular NPCs turned into customers wouldn't open their room doors
            
            speed = 5; // For debug

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
                    SUtility.Lerp(lerpStartPosition.X, lerpEndPosition.X, lerpPosition / lerpDuration),
                    SUtility.Lerp(lerpStartPosition.Y, lerpEndPosition.Y, lerpPosition / lerpDuration)
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
            
            b.Draw(
                Sprite.Texture,
                getLocalPosition(Game1.viewport) +
                new Vector2(GetSpriteWidthForPositioning() * 4 / 2, GetBoundingBox().Height / 2) + drawOffsetForSeat.Value,
                Sprite.SourceRect,
                Color.White * alpha,
                rotation,
                new Vector2(Sprite.SpriteWidth / 2, Sprite.SpriteHeight * 3f / 4f),
                Math.Max(0.2f, scale.Value) * 4f,
                SpriteEffects.None,
                Math.Max(0f, base.StandingPixel.Y / 10000f + ((IsSitting.Value) ? 0.005f : 0.0001f)));

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
                    getLocalPosition(Game1.viewport) + vector + drawOffsetForSeat.Value,
                    sourceRect,
                    Color.White * alpha,
                    rotation,
                    new Vector2(sourceRect.Width / 2, sourceRect.Height / 2 + 1),
                    Math.Max(0.2f, scale.Value) * 4f + num,
                    flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                    Math.Max(0f, drawOnTop ? 0.992f : (base.StandingPixel.Y / 10000f + 0.001f)));
            }

            if (IsEmoting)
            {
                float layer = base.StandingPixel.Y / 10000f;
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
                        Sprite.AnimateUp(time, (speed - 2 + addedSpeed) * -25, SUtility.isOnScreen(base.TilePoint, 1, location) ? "Cowboy_Footstep" : "");
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
                        Sprite.AnimateRight(time, (speed - 2 + addedSpeed) * -25, SUtility.isOnScreen(base.TilePoint, 1, location) ? "Cowboy_Footstep" : "");
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
                        Sprite.AnimateDown(time, (speed - 2 + addedSpeed) * -25, SUtility.isOnScreen(base.TilePoint, 1, location) ? "Cowboy_Footstep" : "");
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
                        Sprite.AnimateLeft(time, (speed - 2 + addedSpeed) * -25, SUtility.isOnScreen(base.TilePoint, 1, location) ? "Cowboy_Footstep" : "");
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
            return $"[Customer] \n"
                   + $"Name: {Name} \n"
                   + $"Location: {currentLocation}, Position: {Position}, Tile: {base.Tile} \n"
                   + $"Bus depart timer: {busDepartTimer}, Convene timer: {conveneWaitingTimer} \n"
                   + $"State: {State} \n";
        }
    }
}