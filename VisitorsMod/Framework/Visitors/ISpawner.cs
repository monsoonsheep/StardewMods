using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewMods.VisitorsMod.Framework.Data;
using StardewMods.VisitorsMod.Framework.Models.Activities;

namespace StardewMods.VisitorsMod.Framework.Visitors;
public interface ISpawner
{
    public string Id { get; }

    public int NextArrivalTime { get; }

    public bool IsAvailable();

    public bool StartVisit(Visit visit);

    public bool EndVisit(Visit visit);
}
