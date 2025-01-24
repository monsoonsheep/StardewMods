using System.Text.Json.Serialization;
using StardewMods.CustomFarmerAnimations.Framework.SpriteEditing;

namespace StardewMods.CustomFarmerAnimations.Framework;

public class CustomFarmerAnimationModel
{
    private EditOperation[] editOperations = null!;

    /// <summary>
    /// JSON Deserializing
    /// </summary>
    public string Name { get; init; } = null!;

    /// <summary>
    /// JSON Deserializing
    /// </summary>
    public List<string> Operations { get; init; } = [];

    [JsonIgnore]
    internal EditOperation[] EditOperations
    {
        get
        {
            if (this.editOperations == null)
            {
                this.editOperations = new EditOperation[this.Operations.Count];
                for (int i = 0; i < this.Operations.Count; i++)
                {
                    this.editOperations[i] = ParseOperation(this.Operations[i]) ?? throw new Exception();
                }
            }

            return this.editOperations;
        }
    }

    private static EditOperation? ParseOperation(string operation)
    {
        string[] split = operation.Split(' ');

        return split[0] switch
        {
            "move" => Move.Parse(split),
            "copy" => Copy.Parse(split),
            "erase" => Erase.Parse(split),
            _ => null,
        };
    }
}
