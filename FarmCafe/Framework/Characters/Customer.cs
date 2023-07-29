﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using FarmCafe.Framework.Customers;
using FarmCafe.Framework.Managers;
using FarmCafe.Framework.Models;
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

	public enum CustomerState
	{
		ExitingBus,
		Convening,
		MovingToTable,
		GoingToSit,
		Sitting,
		ReadyToOrder,
		Eating,
		DoneEating,
		GettingUpFromSeat,
		Leaving,
		Free,
	}

    [InstanceStatics]
    [XmlInclude(typeof(Customer))]
	[XmlType(Namespace = "FarmCafe.Framework.Customers",
	TypeName = "Customer")]
	public class Customer : NPC
	{
		[XmlElement("CustomerModel")]
		public CustomerModel Model { get; set; }

		[XmlIgnore]
		internal CustomerGroup Group;

		[XmlIgnore]
		public Furniture Seat;


        protected NetEnum<CustomerState> State = new NetEnum<CustomerState>(CustomerState.ExitingBus);
		private int busDepartTimer = 0;
        private int conveneWaitingTimer = 0;
        private int lookAroundTimer = 0;
        private int orderTimer = 0;

        [XmlIgnore]
        internal Point busConvenePoint;

        

		private float lerpPosition = -1f;
		private float lerpDuration = 0f;
		private Vector2 lerpStartPosition;
		private Vector2 lerpEndPosition;

        protected bool emoteLoop = false;
		private NetVector2 drawOffsetForSeat = new NetVector2(new Vector2(0, 0));
		
		private NetVector2 tableCenterForEmote = new NetVector2(new Vector2(0, 0));

        [XmlIgnore]
        internal List<int> lookingDirections = new() { 0, 1, 3 };

		internal delegate void BehaviorFunction();

        [XmlIgnore]
		internal int OrderItem { get; set; }

        [XmlIgnore]
		internal bool FreezeMotion
		{
			get { return freezeMotion; }
			set { freezeMotion = value; }
		}

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
			speed = 2;
            displayName = "Customer";
            Portrait = FarmCafe.ModHelper.ModContent.Load<Texture2D>($"assets/Portraits/{model.PortraitName}.png");

            Model = model;
            State.Set(CustomerState.ExitingBus);

            currentLocation = location;
			location.addCharacter(this);

			this.modData["CustomerData"] = "T";
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
			if (conveneWaitingTimer > 0)
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

            // Lerping position for sitting
            if (lerpPosition >= 0f)
			{
				lerpPosition += (float)time.ElapsedGameTime.TotalSeconds;

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
				}
			}
			
			// Sitting at table, waiting to order
			if (orderTimer > 0)
			{
                orderTimer -= time.ElapsedGameTime.Milliseconds;

                if (orderTimer <= 0)
				{
                    this.OrderReady();
				}
			}
		}


		public override void draw(SpriteBatch b, float alpha = 1f)
		{
			b.Draw(Sprite.Texture,
				getLocalPosition(Game1.viewport) + new Vector2(GetSpriteWidthForPositioning() * 4 / 2, GetBoundingBox().Height / 2) + drawOffsetForSeat,
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
                Vector2 vector = new Vector2(Sprite.SpriteWidth * 4 / 2, 8f);
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

                float num = (float) Math.Max(0f, Math.Ceiling(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 600.0 + 200f)) / 4f);

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
				Vector2 localPosition2;
				float layer = getStandingY() / 10000f;
                localPosition2 = getLocalPosition(Game1.viewport);
                localPosition2.Y -= 32 + Sprite.SpriteHeight * 4;

                b.Draw(Game1.emoteSpriteSheet, localPosition2, new Rectangle(CurrentEmoteIndex * 16 % Game1.emoteSpriteSheet.Width, CurrentEmoteIndex * 16 / Game1.emoteSpriteSheet.Width * 16, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, layer);
			}

			if (State == CustomerState.ReadyToOrder)
			{
                float num2 = 4f * (float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0), 2);
                b.Draw(
                    Game1.mouseCursors,
                    Game1.GlobalToLocal(tableCenterForEmote),
                    new Rectangle(402, 495, 7, 16),
                    Color.White,
                    0f,
                    new Vector2(1f, 4f),
                    4f + Math.Max(0f, 0.25f - num2 / 16f),
                    SpriteEffects.None,
                    1f);
            }
		}

		public override void MovePosition(GameTime time, xTile.Dimensions.Rectangle viewport, GameLocation currentLocation)
		{
			//Debug.Log($"{moveUp}, {moveRight}, {moveDown}, {moveLeft}");
			if (moveUp)
			{
				if (currentLocation == null || !currentLocation.isCollidingPosition(nextPosition(0), viewport, isFarmer: false, 0, glider: false, this) || isCharging)
				{
					position.Y -= speed + addedSpeed;

					if (!ignoreMovementAnimation)
					{
						Sprite.AnimateUp(time, (speed - 2 + addedSpeed) * -25, Utility.isOnScreen(getTileLocationPoint(), 1, currentLocation) ? "Cowboy_Footstep" : "");
						faceDirection(0);
					}

					blockedInterval = 0;

				}
				else if (!currentLocation.isTilePassable(nextPosition(0), viewport))
				{
					Halt();
				}
				else
				{
					//blockedInterval += time.ElapsedGameTime.Milliseconds;
				}

			}
			else if (moveRight)
			{
				if (currentLocation == null || !currentLocation.isCollidingPosition(nextPosition(1), viewport, isFarmer: false, 0, glider: false, this) || isCharging)
				{
					position.X += speed + addedSpeed;

					if (!ignoreMovementAnimation)
					{
						Sprite.AnimateRight(time, (speed - 2 + addedSpeed) * -25, Utility.isOnScreen(getTileLocationPoint(), 1, currentLocation) ? "Cowboy_Footstep" : "");
						faceDirection(1);
					}

					blockedInterval = 0;
				}
				else if (!currentLocation.isTilePassable(nextPosition(1), viewport))
				{
					Halt();
				}
				else
				{
					//blockedInterval += time.ElapsedGameTime.Milliseconds;
				}
			}
			else if (moveDown)
			{
				if (currentLocation == null || !currentLocation.isCollidingPosition(nextPosition(2), viewport, isFarmer: false, 0, glider: false, this) || isCharging)
				{
					position.Y += speed + addedSpeed;

					if (!ignoreMovementAnimation)
					{
						Sprite.AnimateDown(time, (speed - 2 + addedSpeed) * -25, Utility.isOnScreen(getTileLocationPoint(), 1, currentLocation) ? "Cowboy_Footstep" : "");
						faceDirection(2);
					}

					blockedInterval = 0;

				}
				else if (!currentLocation.isTilePassable(nextPosition(2), viewport))
				{
					Halt();
				}
				else
				{
					//blockedInterval += time.ElapsedGameTime.Milliseconds;
				}

			}
			else if (moveLeft)
			{
				if (currentLocation == null || !currentLocation.isCollidingPosition(nextPosition(3), viewport, isFarmer: false, 0, glider: false, this) || isCharging)
				{
					position.X -= speed + addedSpeed;

					if (!ignoreMovementAnimation)
					{
						Sprite.AnimateLeft(time, (speed - 2 + addedSpeed) * -25, Utility.isOnScreen(getTileLocationPoint(), 1, currentLocation) ? "Cowboy_Footstep" : "");
						faceDirection(3);
					}

					blockedInterval = 0;
				}
				else if (!currentLocation.isTilePassable(nextPosition(3), viewport))
				{
					Halt();
				}
				else
				{
					//blockedInterval += time.ElapsedGameTime.Milliseconds;
				}
			}


			//if (blockedInterval > 5000)
			//{
			//    Halt();
			//}
		}

		public override void tryToReceiveActiveObject(Farmer who)
		{
			if (who.ActiveObject != null && who.ActiveObject.ParentSheetIndex == OrderItem)
			{
				this.OrderReceive();
                who.reduceActiveItemByOne();
            }
		}

        internal void LeaveBus()
        {
            if (Group.Members.Count == 1)
            {
               GoToCafe();
            }
            else
            {
               collidesWithOtherCharacters.Set(false);
               HeadTowards(GetLocationFromName("BusStop"),busConvenePoint, 2,StartConvening);
            }
        }

        internal void SetBusConvene(Point pos, int timer)
        {
            busDepartTimer = timer;
            busConvenePoint = pos;
        }

        internal void GoToCafe()
        {
           State.Set(CustomerState.MovingToTable);
           collidesWithOtherCharacters.Set(false);
           HeadTowards(Group.TableLocation,Seat.TileLocation.ToPoint(), -1,SitDown);
           controller.finalFacingDirection = DirectionIntFromPoints(controller.pathToEndPoint.Last(),Seat.TileLocation.ToPoint());
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
            foreach (Customer mate in Group.Members)
            {
                if (mate.State.Value != CustomerState.Convening)
                {
                    return;
                }
            }
            foreach (Customer mate in Group.Members)
                mate.GoToCafe();
        }

        internal void LookAround()
        {
           faceDirection(lookingDirections[Game1.random.Next(lookingDirections.Count)]);
        }

        internal void SitDown()
        {
           controller = null;
           isCharging = true;

            var mypos =Position;
            var seatpos =Seat.TileLocation * 64f;

           State.Set(CustomerState.GoingToSit);
           LerpPosition(mypos, seatpos, 0.15f);
           faceDirection(Seat.GetSittingDirection());

            Vector2 vec =facingDirection.Value switch
            {
                0 => new Vector2(0f, -24f), // up
                1 => new Vector2(12f, -8f), // right
                2 => new Vector2(0f, 0f), // down 
                3 => new Vector2(-12f, -8f), // left
                _ =>drawOffsetForSeat
            };

           drawOffsetForSeat.Set(vec);
           Breather = true;
           orderTimer = Game1.random.Next(300, 500);
        }

        internal void SitDownFinishLerping()
        {
           State.Set(CustomerState.Sitting);
        }

        internal void GetUp(int direction)
        {
           drawOffsetForSeat.Set(new Vector2(0, 0));
            var nextPos =Position + (DirectionIntToDirectionVector(direction) * 64f);
           LerpPosition(Position, nextPos, 0.15f);
        }

        internal void OrderReady()
        {
           State.Set(CustomerState.ReadyToOrder);
            foreach (Customer mate in Group.Members)
            {
                if (mate.State != CustomerState.ReadyToOrder)
                {
                    return;
                }
            }

           tableCenterForEmote.Set(GetTableCenter());
           emoteLoop = true;
           doEmote(16);
           CurrentDialogue.Push(new Dialogue("I am customer.", this));
           OrderItem = 746;
        }

        internal void OrderReceive()
        {
           emoteLoop = false;
           isEmoting = false;
           State.Set(CustomerState.Eating);
           doEmote(20);
        }

        internal void DoNothingAndWait()
        {
           State.Set(CustomerState.Free);
        }


        internal void GoHome()
        {

        }


        internal void HeadTowards(GameLocation targetLocation, Point targetTile, int finalFacingDirection = 0, BehaviorFunction endBehaviorFunction = null)
        {
           controller = null;
           FreezeMotion = false;
           isCharging = false;

            Stack<Point> path = PathTo(currentLocation,getTileLocationPoint(), targetLocation, targetTile);

            if (path == null || !path.Any())
            {
                Debug.Log("Customer couldn't find path.", LogLevel.Warn);
                GoHome();
                return;
            }


           controller = new PathFindController(path, currentLocation, this, path.Last())
            {
                NPCSchedule = true,
                nonDestructivePathing = true,
                endBehaviorFunction = endBehaviorFunction != null ? (c, loc) => endBehaviorFunction() : null,
                finalFacingDirection = finalFacingDirection
            };

            if (controller == null)
            {
                Debug.Log("Can't construct controller.", LogLevel.Warn);
               GoHome();
            }
        }


        internal Stack<Point> PathTo(GameLocation startingLocation, Point startTile, GameLocation targetLocation, Point targetTile)
        {
            Stack<Point> path = new Stack<Point>();
            Point locationStartPoint = startTile;
            if (startingLocation.Name.Equals(targetLocation.Name, StringComparison.Ordinal))
                return FindPath(locationStartPoint, targetTile, startingLocation);

            List<string> locationsRoute = CafeManager.getLocationRoute(startingLocation, targetLocation);

            if (locationsRoute == null)
            {
                Debug.Log("Route to cafe not found!", LogLevel.Error);
                return null;
            }

            for (int i = 0; i < locationsRoute.Count; i++)
            {
                GameLocation currentLocation = GetLocationFromName(locationsRoute[i]);
                if (i < locationsRoute.Count - 1)
                {
                    Point target = currentLocation.getWarpPointTo(locationsRoute[i + 1]);
                    var cafeloc = GetLocationFromName(locationsRoute[i + 1]);
                    if (target == Point.Zero && cafeloc != null && cafeloc.Name.Contains("Cafe") && currentLocation is BuildableGameLocation buildableLocation)
                    {
                        var building = buildableLocation.buildings.Where(b => b.indoors.Value != null && b.indoors.Value.Name.Contains("Cafe")).FirstOrDefault();
                        if (building == null || building.humanDoor == null)
                            throw new Exception("schedule pathing tried to find a warp point that doesn't exist.");
                        target = building.humanDoor.Value;
                    }
                    if (target.Equals(Point.Zero) || locationStartPoint.Equals(Point.Zero))
                        throw new Exception("schedule pathing tried to find a warp point that doesn't exist.");

                    path = combineStacks(path, FindPath(locationStartPoint, target, currentLocation));
                    locationStartPoint = currentLocation.getWarpPointTarget(target);
                    if (locationStartPoint == Point.Zero)
                    {
                        var building = (currentLocation as BuildableGameLocation).getBuildingAt(target.ToVector2());
                        if (building != null && building.indoors.Value != null)
                        {
                            Warp w = building.indoors.Value.warps.FirstOrDefault();
                            locationStartPoint = new Point(w.X, w.Y - 1);
                        }
                    }
                }
                else
                {
                    path = combineStacks(path, FindPath(locationStartPoint, targetTile, currentLocation));
                }
            }

            return path;
        }

        internal Stack<Point> FindPath(Point startTile, Point targetTile, GameLocation location, int iterations = 600)
        {
            if (IsChair(location.GetFurnitureAt(targetTile.ToVector2())))
            {
                return PathToChair(location, startTile, targetTile, location.GetFurnitureAt(targetTile.ToVector2()));
            }
            else if (location.Name.Equals("Farm"))
            {
                return PathFindController.FindPathOnFarm(startTile, targetTile, location, iterations);
            }
            else
            {
                return PathFindController.findPath(startTile, targetTile, new PathFindController.isAtEnd(PathFindController.isAtEndPoint), location, this, iterations);
            }
        }

        internal Stack<Point> PathToChair(GameLocation location, Point startTile, Point targetTile, Furniture chair)
        {
            var directions = new List<sbyte[]>
            {
                new sbyte[] { 0, -1 }, // up
			    new sbyte[] { -1, 0 }, // left
			    new sbyte[] { 0, 1 }, // down
			    new sbyte[] { 1, 0 }, // right
		    };

            if (!chair.Name.ToLower().Contains("stool"))
                directions.RemoveAt(chair.currentRotation.Value);

            Stack<Point> shortestPath = null;
            int shortestPathLength = 99999;

            foreach (var direction in directions)
            {
                Furniture obstructionChair = location.GetFurnitureAt((targetTile + new Point(direction[0], direction[1])).ToVector2());
                if (IsChair(obstructionChair))
                    continue;

                var pathRightNextToChair = FindPath(
                    startTile,
                    targetTile + new Point(direction[0], direction[1]),
                    location,
                    1500
                );

                if (pathRightNextToChair == null || pathRightNextToChair.Count >= shortestPathLength)
                    continue;

                shortestPath = pathRightNextToChair;
                shortestPathLength = pathRightNextToChair.Count;
            }
            if (shortestPath == null || shortestPath.Count == 0)
            {
                Debug.Log("path to chair can't be found");
            }
            return shortestPath;
        }


        internal void UpdatePathingTarget(Point targetTile)
        {
            //Debug.Log($"repathing to {targetTile.X}, {targetTile.Y}");
           HeadTowards(currentLocation, targetTile);
        }

        internal static Stack<Point> combineStacks(Stack<Point> original, Stack<Point> toAdd)
        {
            if (toAdd == null)
            {
                return original;
            }
            original = new Stack<Point>(original);
            while (original.Count > 0)
            {
                toAdd.Push(original.Pop());
            }
            return toAdd;
        }

        internal void LerpPosition(Vector2 startPos, Vector2 endPos, float duration)
		{
			lerpStartPosition = startPos;
			lerpEndPosition = endPos;
			lerpPosition = 0f;
			lerpDuration = duration;
		}

		//protected override void updateSlaveAnimation(GameTime time)
		//{
		//	return;
		//}

		internal Vector2 GetTableCenter()
		{
			Furniture table = this.Group.ReservedTable;
			if (table == null)
				return Vector2.Zero;
			return table.boundingBox.Center.ToVector2() + new Vector2(-8, -64);
		}

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
				   + $"Active path: " + this.GetCurrentPathStackShort() + "\n"
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