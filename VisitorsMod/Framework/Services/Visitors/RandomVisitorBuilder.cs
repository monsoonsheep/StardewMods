using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewMods.VisitorsMod.Framework.Data;
using StardewMods.VisitorsMod.Framework.Models;
using StardewMods.VisitorsMod.Framework.Services.Visitors.RandomVisitors;

namespace StardewMods.VisitorsMod.Framework.Services.Visitors;
internal class RandomVisitorBuilder : Service
{
    private readonly IModContentHelper modContent;

    private readonly NetState netState;
    private readonly RandomSprites randomSprites;

    public RandomVisitorBuilder(
        RandomSprites sprites,
        NetState netState,
        IModContentHelper modContent,
        IModEvents events,
        ILogger logger,
        IManifest manifest)
        : base(logger, manifest)
    {
        this.modContent = modContent;
        this.netState = netState;
        this.randomSprites = sprites;
    }

    /// <summary>
    /// Create a randomly-generated <see cref="VisitorModel"/> and a randomly-generated <see cref="GeneratedSpriteData"/>.
    /// This associates the sprite data with the Model by setting the model's Spritesheet path to a custom Guid, which is then
    /// added to a netsynced set and its texture is loaded by AssetRequested
    /// </summary>
    internal VisitorModel GenerateRandomVisitor()
    {
        GeneratedSpriteData spriteData = this.randomSprites.GenerateRandomSpriteData();

        this.netState.GeneratedSprites[spriteData.Guid] = spriteData;

        VisitorModel model = new VisitorModel
        {
            Gender = ModUtility.GameGenderToCustomGender(spriteData.Gender),
            Name = $"Random{spriteData.Guid}",
            Spritesheet = $"{Values.GENERATED_SPRITE_PREFIX}/{spriteData.Guid}",
            Portrait = this.modContent.GetInternalAssetName(Path.Combine("assets", "CharGen", "Portraits", "cat.png")).Name
        };

        return model;
    }
}
