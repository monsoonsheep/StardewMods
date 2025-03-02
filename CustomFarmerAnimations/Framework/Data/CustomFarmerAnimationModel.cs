using System.Text.Json.Serialization;
using StardewMods.CustomFarmerAnimations.Framework.SpriteEditing;
using StardewMods.CustomFarmerAnimations.Framework.SpriteEditing.Overlays;
using StardewMods.CustomFarmerAnimations.Framework.SpriteEditing.Patching;

namespace StardewMods.CustomFarmerAnimations.Framework.Data;

public class CustomFarmerAnimationModel
{
    /// <summary>
    /// JSON Deserializing
    /// </summary>
    public string Name { get; init; } = null!;

    /// <summary>
    /// JSON Deserializing
    /// </summary>
    public List<string> Operations { get; init; } = [];

    /// <summary>
    /// JSON Deserializing
    /// </summary>
    public List<ImagePatch> Patches { get; init; } = [];

    /// <summary>
    /// JSON Deserializing
    /// </summary>
    public List<DrawOver> DrawOver { get; init; } = [];

    private List<EditOperation>? editOperations = null;

    [JsonIgnore]
    internal List<EditOperation> EditOperations
        => this.editOperations ??= this.Operations.Select(EditOperation.ParseOperation).ToList()!;
}
