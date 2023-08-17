using FarmCafe.Framework.Managers;
using StardewValley;
using StardewValley.Objects;
using System.Collections.Generic;
using FarmCafe.Framework.Characters;
using FarmCafe.Framework.Objects;
using static FarmCafe.Framework.Utilities.Utility;

namespace FarmCafe.Framework.Characters
{
	internal class CustomerGroup
	{
		public List<Customer> Members;
		public ITable ReservedTable;

		public CustomerGroup()
		{
			Members = new List<Customer>();
		}

		public void Add(Customer customer)
        {
            if (Members.Count == 0)
                customer.IsGroupLeader.Set(true);
            Members.Add(customer);
		}

		public bool ReserveTable(ITable table)
		{
			List<ISeat> chairs = table.Seats;

            if (table.Reserve(Members) is false)
                return false;

            this.ReservedTable = table;
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
