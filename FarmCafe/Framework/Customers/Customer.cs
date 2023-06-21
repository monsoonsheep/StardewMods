using FarmCafe.Framework.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
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
		Eating,
		DoneEating,
		GettingUpFromSeat,
		Leaving,
		Free,
	}

	public class Customer : NPC
	{
		internal CustomerState State;


		public CustomerModel Model { get; set; }
		internal CustomerGroup Group { get; set; }

		public Furniture Seat;

		internal int conveneWaitingTimer = 0;
		internal int busDepartTimer = 0;
		internal Point busConvenePoint;

		private int lookAroundTimer = 300;

		private float lerpPosition = -1f;
		private float lerpDuration = 0f;
		private Vector2 lerpStartPosition;
		private Vector2 lerpEndPosition;

		internal new Vector2 drawOffset;
		internal List<int> lookingDirections = new() { 0, 1, 3 };

		internal delegate void BehaviorFunction();


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
			Debug.Log("Creating a customer");
			willDestroyObjectsUnderfoot = false;
			collidesWithOtherCharacters.Value = false;
			eventActor = true;
			speed = 2;

			Model = model;
			Name = "Customer_" + model.Name + Manager.CurrentCustomers.Count;

			currentLocation = location;
			location.addCharacter(this);
			State = CustomerState.ExitingBus;
			Debug.Log($"NPC {Name} spawned");
		}


		public override void update(GameTime time, GameLocation location)
		{
			if (!Context.IsWorldReady) return;

			base.update(time, location);

			if (busDepartTimer > 0)
			{
				busDepartTimer -= time.ElapsedGameTime.Milliseconds;

				if (busDepartTimer <= 0)
				{
					isCharging = false;

					if (Group.ReservedTable != null)
					{
						HeadTowards(busConvenePoint, 2, this.StartConvening);
						collidesWithOtherCharacters.Set(false);
					}
					else
					{
						this.DoNothingAndWait();
					}
				}
			}

			if (conveneWaitingTimer > 0)
			{

				conveneWaitingTimer -= time.ElapsedGameTime.Milliseconds;
				lookAroundTimer -= time.ElapsedGameTime.Milliseconds;

				if (Group.Members.Count == 1)
				{
					conveneWaitingTimer = 0;
					showTextAboveHead("Arrived, I have.");
				}

				if (conveneWaitingTimer <= 0)
				{
					Group.ConveneEnd(this);
					return;
				}

				if (lookAroundTimer <= 0)
				{
					faceDirection(lookingDirections[Game1.random.Next(lookingDirections.Count)]);
					lookAroundTimer += Game1.random.Next(400, 1000);
				}
			}


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

			if (IsEmoting)
			{
				Vector2 localPosition2 = getLocalPosition(Game1.viewport);
				localPosition2.Y -= 32 + Sprite.SpriteHeight * 4;
				b.Draw(Game1.emoteSpriteSheet, localPosition2, new Rectangle(CurrentEmoteIndex * 16 % Game1.emoteSpriteSheet.Width, CurrentEmoteIndex * 16 / Game1.emoteSpriteSheet.Width * 16, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, getStandingY() / 10000f);
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

		internal void HeadTowards(Point targetTile, int finalFacingDirection = 0, BehaviorFunction endBehaviorFunction = null)
		{
			controller = null;
			freezeMotion = false;
			Stack<Point> path = Pathing.FindPath(getTileLocationPoint(), targetTile, currentLocation);

			if (State == CustomerState.MovingToTable && currentLocation.Name == "Farm")
			{
				finalFacingDirection = DirectionIntFromPoints(path.Last(), targetTile);
			}

			if (path == null)
			{
				if (State == CustomerState.MovingToTable)
				{
					Debug.Log("Customer can't get to their chair.", LogLevel.Warn);
				}
				else
				{
					foreach (var pos in AdjacentTiles(targetTile))
					{
						path = Pathfinder.findPathForNPCSchedules(getTileLocationPoint(), pos, currentLocation, 500);
						if (path != null) break;
					}
				}
			}

			if (path == null || !path.Any())
			{
				Debug.Log("Customer couldn't find path.", LogLevel.Warn);
				this.GoHome();
				return;
			}

			controller = new Pathfinder(path, currentLocation, this, new Point(0, 0))
			{
				nonDestructivePathing = true,
				endBehaviorFunction = endBehaviorFunction != null ? (c, loc) => endBehaviorFunction() : null,
				finalFacingDirection = finalFacingDirection
			};

			if (controller == null)
			{
				Debug.Log("Can't construct controller.");
				this.GoHome();
			}

			Debug.Log($"Path = {this.GetCurrentPathStackShort()}");
		}

		internal void LerpPosition(Vector2 startPos, Vector2 endPos, float duration)
		{
			lerpStartPosition = startPos;
			lerpEndPosition = endPos;
			lerpPosition = 0f;
			lerpDuration = duration;
		}

		protected override void updateSlaveAnimation(GameTime time)
		{
			return;
		}

		public override bool canPassThroughActionTiles() => false;

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