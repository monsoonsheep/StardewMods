using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using VisitorFramework.Framework.Characters;

namespace StardewCafe.Framework.Interfaces
{
    internal interface IVisitorFrameworkApi
    {
        public Visitor MakeNpcVisitor(NPC npc);
    }

}
