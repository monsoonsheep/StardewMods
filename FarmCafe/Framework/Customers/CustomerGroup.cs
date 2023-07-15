using FarmCafe.Framework.Managers;
using StardewValley.Objects;
using System.Collections.Generic;
using static FarmCafe.Framework.Utilities.Utility;

namespace FarmCafe.Framework.Customers
{
	internal class CustomerGroup
	{
		public List<Customer> Members;
		public Furniture ReservedTable;
		public Dictionary<Furniture, Customer> SeatsToMembers;

		public CustomerGroup()
		{
			Members = new List<Customer>();
		}

		public void Add(Customer customer)
		{
			Members.Add(customer);
		}

		public bool ReserveTable(Furniture table)
		{
			List<Furniture> chairs = TableManager.GetChairsOfTable(table);
			if (chairs.Count < Members.Count)
			{
				return false;
			}

			ReservedTable = table;
			table.modData["FarmCafeTableIsReserved"] = "T";

			SeatsToMembers = new Dictionary<Furniture, Customer>();
			for (int i = 0; i < Members.Count; i++)
			{
				SeatsToMembers[chairs[i]] = Members[i];
				Members[i].Seat = chairs[i];
				chairs[i].modData["FarmCafeChairIsReserved"] = "T";
			}

			return true;
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
