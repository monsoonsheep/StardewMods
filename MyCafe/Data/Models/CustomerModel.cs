namespace MyCafe.Data.Models;

internal class CustomerModel
{
    public string Name { get; set; } = null!;

    public string Gender { get; set; } = ModKeys.GENDER_ANY;

    public string Spritesheet { get; set; } = null!;

    public string Portrait { get; set; } = null!;
}
