using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisitorFramework.Framework.Characters;
using VisitorFramework.Framework.Managers;
using StardewValley;

namespace VisitorFramework.Framework.Api
{
    public interface IApi
    {
        public Visitor MakeNpcVisitor(NPC npc);
    }

    public class Api : IApi
    {
        public Visitor MakeNpcVisitor(NPC npc)
        {
            return VisitorManager.CreateVisitorFromNpc(npc);
        }
    }
}
