using System.Text.Json.Serialization;
using StardewMods.CustomFarmerAnimations.Framework.SpriteEditing;

namespace StardewMods.CustomFarmerAnimations.Framework;

public class CustomFarmerAnimationModel
{
    private List<EditOperation>? editOperations = null;

    /// <summary>
    /// JSON Deserializing
    /// </summary>
    public string Name { get; init; } = null!;

    /// <summary>
    /// JSON Deserializing
    /// </summary>
    public List<string> Operations { get; init; } = [];

    [JsonIgnore]
    internal List<EditOperation> EditOperations
        => this.editOperations ??= this.Operations.Select(EditOperation.ParseOperation).ToList()!;

    
}
