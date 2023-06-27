using FarmCafe.Framework.Managers;
using FarmCafe.Framework.Objects;
using StardewValley.Objects;
using System.Collections.Generic;
using static FarmCafe.Framework.Utilities.Utility;

namespace FarmCafe.Framework.Customers
{
	internal class CustomerGroup
	{
		public List<Customer> Members;
		public Table ReservedTable;
		public Dictionary<Furniture, Customer> SeatsToMembers;

		public CustomerGroup()
		{
			Members = new List<Customer>();
		}

		public void Add(Customer customer)
		{
			Members.Add(customer);
		}

		public bool ReserveTable(Table table)
		{
			if (table.Chairs.Count < Members.Count)
			{
				return false;
			}

			ReservedTable = table;
			table.isReserved = true;

			SeatsToMembers = new Dictionary<Furniture, Customer>();
			for (int i = 0; i < Members.Count; i++)
			{
				SeatsToMembers[table.Chairs[i]] = Members[i];
				Members[i].Seat = table.Chairs[i];
			}

			return true;
		}

		public void ConveneEnd(Customer member)
		{
			foreach (Customer customer in Members)
			{
				if (customer.State != CustomerState.Convening)
				{
					return;
				}
			}

			GroupStartMoving();
		}

		public void GroupStartMoving()
		{
			foreach (Customer member in Members)
			{
				member.State = CustomerState.MovingToTable;
				member.collidesWithOtherCharacters.Set(false);
				member.HeadTowards(CustomerManager.GetBusWarpToFarm(), 3, null);
			}
		}

		public void GetLookingDirections()
		{
			foreach (Customer member in Members)
			{
				member.lookingDirections.Clear();
				foreach (Customer other in Members)
				{
					if (member.Equals(other)) continue;
					member.lookingDirections.Add(
						DirectionIntFromVectors(member.getTileLocation(), other.getTileLocation()));
				}
			}
		}

	}
}
