using FarmCafe.Framework.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Objects;
using Object = StardewValley.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using static FarmCafe.Framework.Utilities.Utility;
using Manager = FarmCafe.Framework.Managers.CustomerManager;
using Pathfinder = StardewValley.PathFindController;
using Pathing = FarmCafe.Framework.Customers.CustomerPathing;
namespace FarmCafe.Framework.Customers
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

	public class Customer : NPC
	{
		
		public CustomerModel Model { get; set; }
		internal CustomerGroup Group { get; set; }

		public Furniture Seat;

        internal CustomerState State;
		internal int busDepartTimer = 0;
		internal Point busConvenePoint;

        internal int conveneWaitingTimer = 0;
        internal int lookAroundTimer = 300;
		internal int orderTimer = 0;

		private float lerpPosition = -1f;
		private float lerpDuration = 0f;
		private Vector2 lerpStartPosition;
		private Vector2 lerpEndPosition;

		internal bool emoteLoop = false;
		internal new Vector2 drawOffset;
		internal Vector2 tableCenterForEmote;
		internal List<int> lookingDirections = new() { 0, 1, 3 };

		internal delegate void BehaviorFunction();

		internal bool FreezeMotion
		{
			get { return freezeMotion; }
			set { freezeMotion = value; }
		}

        public override bool canPassThroughActionTiles() => false;

        public Customer()
		{

		}

		protected override void initNetFields()
		{
			base.initNetFields();
		}

		public Customer(CustomerModel model, int frameSizeWidth, int frameSizeHeight, Point targetTile, GameLocation location)
			: base(new AnimatedSprite(model.TilesheetPath, 0, frameSizeWidth, frameSizeHeight), targetTile.ToVector2() * 64, 1, model.Name)
		{
            //Debug.Log("Creating a customer");
            willDestroyObjectsUnderfoot = false;
			collidesWithOtherCharacters.Value = false;
			eventActor = true;
			speed = 2;

			Model = model;
			Name = "Customer_" + model.Name + Manager.CurrentCustomers.Count;

			currentLocation = location;
			location.addCharacter(this);

			State = CustomerState.ExitingBus;

			this.modData["CustomerData"] = "T";
			Debug.Log($"NPC {Name} spawned");
			
		}


		public override void update(GameTime time, GameLocation location)
		{
			if (!Context.IsWorldReady) return;

			base.update(time, location);

			// Spawning and waiting to leave the bus
			if (busDepartTimer > 0)
			{
				busDepartTimer -= time.ElapsedGameTime.Milliseconds;

				if (busDepartTimer <= 0)
				{
					if (Group.ReservedTable != null)
					{
						this.HeadTowards(busConvenePoint, 2, this.StartConvening);
						collidesWithOtherCharacters.Set(false);
					}
				}
			}

			// Convening with group members
			if (conveneWaitingTimer > 0)
			{
				conveneWaitingTimer -= time.ElapsedGameTime.Milliseconds;
				lookAroundTimer -= time.ElapsedGameTime.Milliseconds;

				if (Group.Members.Count == 1)
				{
					conveneWaitingTimer = 0;
					showTextAboveHead("Arrived, I have.");
				}

				if (lookAroundTimer <= 0)
				{
					faceDirection(lookingDirections[Game1.random.Next(lookingDirections.Count)]);
					lookAroundTimer += Game1.random.Next(400, 1000);
				}

                if (conveneWaitingTimer <= 0)
                {
                    this.FinishConvening();
                    return;
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
					State = CustomerState.Sitting;

				}
				else
					return;
			}
			
			// Sitting at table, waiting to order
			if (orderTimer > 0)
			{
                orderTimer -= time.ElapsedGameTime.Milliseconds;

                if (orderTimer <= 0)
				{
                    State = CustomerState.ReadyToOrder;
                    this.OrderReady();
				}
			}
		}


		public override void draw(SpriteBatch b, float alpha = 1f)
		{
			
			b.Draw(Sprite.Texture,
				getLocalPosition(Game1.viewport) + new Vector2(GetSpriteWidthForPositioning() * 4 / 2, GetBoundingBox().Height / 2) + drawOffset,
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
					getLocalPosition(Game1.viewport) + vector + drawOffset, 
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
				float layer = 0f;
                if (State == CustomerState.ReadyToOrder)
				{
					//localPosition2 = tableCenterForEmote;
					localPosition2 = GetTableCenter();
					layer = 0.991f;
                }
				else
				{
					layer = getStandingY() / 10000f;
                    localPosition2 = getLocalPosition(Game1.viewport);
                    localPosition2.Y -= 32 + Sprite.SpriteHeight * 4;
                }
                
                b.Draw(Game1.emoteSpriteSheet, localPosition2, new Rectangle(CurrentEmoteIndex * 16 % Game1.emoteSpriteSheet.Width, CurrentEmoteIndex * 16 / Game1.emoteSpriteSheet.Width * 16, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, layer);
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
			Furniture table = currentLocation.GetFurnitureAt(getTileLocation() + DirectionIntToDirectionVector(FacingDirection));
			if (table == null)
				return Vector2.Zero;
			return Game1.GlobalToLocal(table.boundingBox.Center.ToVector2()) + new Vector2(table.getTilesWide() * -16, table.getTilesHigh() * -64);
		}

		internal void Reset()
		{
			isCharging = false;
			freezeMotion = false;
			controller = null;
			State = CustomerState.Free;
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
				   + $"Model: [Name: {Model.Name}, Tilesheet: {Model.TilesheetPath}]\n";

			//+ $"Group members: {Group.Members.Count}\n"
			//+ $"Animation: {Model.Animation.NumberOfFrames} frames, {Model.Animation.Duration}ms each, Starting {Model.Animation.StartingFrame}\n"
			//+ $"Sprite info: {Sprite} current frame = {Sprite.CurrentFrame}, "
			//+ $"Texture name = {Sprite.Texture.Name}\n"
			//+ $"Facing direction {FacingDirection}, IsMoving = {isMoving()}";
		}
	}
}