using System;
using System.Collections.Generic;
using static VisitorFramework.Framework.Utility;

namespace VisitorFramework.Framework.Characters
{
    internal class VisitorGroup
	{
		public List<Visitor> Members;

		// SCHEDULES HERE

		public VisitorGroup(List<Visitor> members)
        {
            Members = members;
            Members[0].IsGroupLeader.Set(true);
            foreach (var member in members)
            {
                member.Group = this;
            }
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
						DirectionIntFromVectors(member.Tile, other.ConvenePoint.ToVector2()));
				}
			}
		}

	}
}
