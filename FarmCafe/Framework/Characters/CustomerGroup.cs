using FarmCafe.Framework.Managers;
using StardewValley;
using StardewValley.Objects;
using System.Collections.Generic;
using FarmCafe.Framework.Characters;
using static FarmCafe.Framework.Utilities.Utility;

namespace FarmCafe.Framework.Characters
{
	internal class CustomerGroup
	{
		public List<Customer> Members;
		public Furniture ReservedTable;
		public Dictionary<Furniture, Customer> SeatsToMembers;
		public GameLocation TableLocation;

		public CustomerGroup()
		{
			Members = new List<Customer>();
		}

		public void Add(Customer customer)
        {
            if (Members.Count == 0)
                customer.IsGroupLeader = true;
            Members.Add(customer);
		}

		public bool ReserveTable(Furniture table)
		{
			List<Furniture> chairs = FarmCafe.tableManager.GetChairsOfTable(table);
			if (chairs.Count < Members.Count)
			{
				return false;
			}

			this.ReservedTable = table;
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

		public void GetLookingDirections()
		{
			foreach (Customer member in Members)
			{
				member.LookingDirections.Clear();
				foreach (Customer other in Members)
				{
					if (member.Equals(other)) continue;
					member.LookingDirections.Add(
						DirectionIntFromVectors(member.getTileLocation(), other.BusConvenePoint.ToVector2()));
				}
			}
		}

	}
}
