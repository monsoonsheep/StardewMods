using System.Collections.Generic;
using System.Linq;
using Force.DeepCloner;
using Microsoft.Xna.Framework;
using StardewValley;
using static FarmCafe.Framework.Utilities.Utility;

namespace FarmCafe.Framework.Characters
{
    public partial class Customer
    {
        internal void LeaveBus()
        {
            if (Group.Members.Count == 1)
            {
                GoToSeat();
            }
            else
            {
                collidesWithOtherCharacters.Set(false);
                this.HeadTowards(GetLocationFromName("BusStop"), BusConvenePoint, 2, StartConvening);
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
                Group.ReservedTable.CurrentLocation,
                Seat.Position.ToPoint(),
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

        internal void FinishConvening()
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
            IsSitting.Set(true);
            State.Set(CustomerState.Sitting);
            controller = null;
            isCharging = true;

            LerpPosition(
                Position,
                Seat.Position * 64f,
                0.15f,
                () => this.orderTimer = Game1.random.Next(300, 500));

            int sittingDirection = Seat.SittingDirection;
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
            IsSitting.Set(false);
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
            State.Set(CustomerState.OrderReady);
            if (IsGroupLeader)
                this.Group.ReservedTable.IsReadyToOrder = true;

            //if (IsGroupLeader)
            //    TableCenterForEmote = this.Group.ReservedTable.GetCenter() + new Vector2(-8, -64);

            Multiplayer.Sync.UpdateCustomerInfo(this, nameof(OrderItem), OrderItem.ParentSheetIndex);
            //Sync.UpdateCustomerInfo(this, nameof(TableCenterForEmote), TableCenterForEmote.ToString());
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
            int[] directions = new[] { (FacingDirection + 1) % 4, (FacingDirection + 3) % 4, (FacingDirection + 2) % 4 };
            foreach (int direction in directions)
            {
                var boundingBox = GetBoundingBox();
                boundingBox.X += (int) DirectionIntToDirectionVector(direction).X * 64;
                boundingBox.Y += (int) DirectionIntToDirectionVector(direction).Y * 64;

                var nextPosition = Position + (DirectionIntToDirectionVector(direction) * 64f);
                if (!currentLocation.isCollidingPosition(boundingBox, Game1.viewport, false, 0, glider: false, this))
                {
                    // location.isCollidingPosition(nextPosition(0), viewport, isFarmer: false, 0, glider: false, this) && !isCharging
                    this.GetUpFromSeat(direction);
                    return;
                }
            }
            // Handle when customer can't get up from seat because of collisions
        }

        internal void DoNothingAndWait()
        {
            State.Set(CustomerState.Free);
        }

        internal void GoHome()
        {
            if (IsGroupLeader)
            {
                Group.ReservedTable.Free();
                Group.ReservedTable = null;
            }
            if (OriginalNpc == null)
                this.HeadTowards(Game1.getLocationFromName("BusStop"), ModEntry.CafeManager.BusPosition, 0, ReachHome);
            else
            {
                this.HeadTowards(Game1.getLocationFromName("BusStop"), new Point(5, 23), 1, ConvertBack);
            }
        }

        internal void ReachHome()
        {
            IsInvisible = true;
            Game1.removeCharacterFromItsLocation(this.Name);
            if (Group.Members.All(c => c.IsInvisible))
                ModEntry.CafeManager.DeleteGroup(Group);
        }

        internal void ConvertBack()
        {
            this.currentLocation.characters.Remove(this);
            Game1.removeThisCharacterFromAllLocations(this);

            NPC original = new NPC(Sprite, getTileLocation(), DefaultMap, FacingDirection, Name, datable.Value, null, Portrait);
            original.ignoreScheduleToday = false;
            original.followSchedule = true;
            original.syncedPortraitPath.Set(syncedPortraitPath.Value);
            original.lastSeenMovieWeek.Set(lastSeenMovieWeek.Value);
            original.Portrait = Portrait;
            original.Schedule = Schedule;
            original.Breather = Breather;
            original.Schedule = Schedule;

            original.currentLocation = currentLocation;
            original.Position = Position;
            original.faceDirection(FacingDirection);

            this.currentLocation.addCharacter(original);

            original.reloadData();
            original.checkSchedule(Game1.timeOfDay);

            ModEntry.CafeManager.DeleteGroup(Group);
        }
    }
}