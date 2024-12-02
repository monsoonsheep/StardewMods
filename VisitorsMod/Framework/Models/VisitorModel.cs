namespace StardewMods.VisitorsMod.Framework.Models;

internal class VisitorModel
{
    public string Name { get; set; } = null!;

    public string Gender { get; set; } = Values.GENDER_ANY;

    public string Spritesheet { get; set; } = null!;

    public string Portrait { get; set; } = null!;
}
