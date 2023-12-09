using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace VisitorFramework.Framework.Visitors.Activities
{
    internal class VisitorActivity
    {
        internal string Name;
        internal string Location;
        internal List<VisitAction> Actions = new List<VisitAction>();
    }
}
