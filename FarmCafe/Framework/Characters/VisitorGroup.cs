using System;
using System.Collections.Generic;
using FarmCafe.Framework.Objects;
using static FarmCafe.Framework.Utility;

namespace FarmCafe.Framework.Characters
{
    internal class VisitorGroup
	{
		public List<Visitor> Members;
		public Table ReservedTable;

		// SCHEDULES HERE

		public VisitorGroup(List<Visitor> members, Table table)
        {
            Members = members;
            Members[0].IsGroupLeader.Set(true);
            foreach (var member in members)
            {
                member.Group = this;
            }

            if (!ReserveTable(table))
                throw new Exception("Couldn't reserve table. Bug!");
        }

		public bool ReserveTable(Table table)
		{
            if (table.Reserve(Members) is false)
                return false;

            this.ReservedTable = table;
            return true;
		}

		public void GetLookingDirections()
		{
			foreach (Visitor member in Members)
			{
				member.LookingDirections.Clear();
				foreach (Visitor other in Members)
				{
					if (member.Equals(other)) continue;
					member.LookingDirections.Add(
						DirectionIntFromVectors(member.Tile, other.BusConvenePoint.ToVector2()));
				}
			}
		}

	}
}
