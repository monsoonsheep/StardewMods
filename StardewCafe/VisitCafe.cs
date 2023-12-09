using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewValley;
using VisitorFramework.Framework.Characters;
using VisitorFramework.Framework.Activities;

namespace StardewCafe
{
    public class VisitCafe : VisitorFramework.Framework.Activities.Activity
    {
        public override void Start(Visitor v)
        {
            v.HeadTowards(Game1.getFarm(), new Point(80, 20), 0, () => SitDown(v));
        }

        public void SitDown(Visitor v)
        {
            Logger.Log("boy");
        }

        public override void Stop(Visitor v)
        {

        }
    }
}
