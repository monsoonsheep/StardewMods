#region Usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisitorFramework.Framework.Visitors;
using StardewValley;
using VisitorFramework.Framework.Managers;
#endregion

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
