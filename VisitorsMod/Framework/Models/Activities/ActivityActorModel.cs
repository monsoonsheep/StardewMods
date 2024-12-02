#nullable disable
using Microsoft.Xna.Framework;

namespace StardewMods.VisitorsMod.Framework.Models.Activities;

public class ActivityActorModel
{
    public string Name { get; set; } = string.Empty;

    public Point TilePosition { get; set; }

    public string Behavior { get; set; } = string.Empty;
}


