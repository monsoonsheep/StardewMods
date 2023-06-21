using Microsoft.Xna.Framework;
using StardewValley;

namespace FarmCafe.Framework.Customers
{
	internal static class CustomerBehavior
	{
		internal static void StartConvening(this Customer me)
		{
			me.conveneWaitingTimer = Game1.random.Next(500, 3000);
			me.State = CustomerState.Convening;
			me.Group.GetLookingDirections();
		}


		internal static void SitDown(this Customer me)
		{
			me.controller = null;
			me.isCharging = true;

			var mypos = me.Position;
			var seatpos = me.Seat.TileLocation * 64f;

			me.State = CustomerState.GoingToSit;
			me.LerpPosition(mypos, seatpos, 0.15f);
			me.faceDirection(me.Seat.GetSittingDirection());

			me.drawOffset = me.facingDirection.Value switch
			{
				0 => new Vector2(0f, -24f), // up
				1 => new Vector2(12f, -8f), // right
				2 => new Vector2(0f, 0f), // down 
				3 => new Vector2(-12f, -8f), // left
				_ => me.drawOffset
			};
		}


		internal static void DoNothingAndWait(this Customer me)
		{
			me.State = CustomerState.Free;
		}


		internal static void GoHome(this Customer me)
		{

		}


		internal static void ArriveAtFarm(this Customer me)
		{
			if (me.Seat == null)
			{
				Debug.Log($"Couldn't find a seat for {me.Name}");
				return;
			}

			me.HeadTowards(me.Seat.TileLocation.ToPoint(), -1, me.SitDown);
		}
	}
}
