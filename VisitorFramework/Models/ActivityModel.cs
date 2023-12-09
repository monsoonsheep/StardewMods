#region Usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
#endregion

namespace VisitorFramework.Models
{
    public class ActivityModel
    {
        public string Id { get; set; }
        public string[] TimeFrames { get; set; }
        public string Location { get; set; }
        public List<ActionModel> Actions { get; set; }
    }

    public class ActionModel
    {
        public Vector2 Position { get; set; }
        public string Behavior { get; set; }
    }
}
