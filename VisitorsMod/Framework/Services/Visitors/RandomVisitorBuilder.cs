using StardewMods.VisitorsMod.Framework.Data;
using StardewMods.VisitorsMod.Framework.Data.Models;
using StardewMods.VisitorsMod.Framework.Services.Visitors.RandomVisitors;

namespace StardewMods.VisitorsMod.Framework.Services.Visitors;
internal class RandomVisitorBuilder
{
    /// <summary>
    /// Create a randomly-generated <see cref="VisitorModel"/> and a randomly-generated <see cref="GeneratedSpriteData"/>.
    /// This associates the sprite data with the Model by setting the model's Spritesheet path to a custom Guid, which is then
    /// added to a netsynced set and its texture is loaded by AssetRequested
    /// </summary>
    internal VisitorModel GenerateRandomVisitor()
    {
        GeneratedSpriteData spriteData = Mod.RandomSprites.GenerateRandomSpriteData();

        Mod.NetState.GeneratedSprites[spriteData.Guid] = spriteData;

        VisitorModel model = new VisitorModel
        {
            Gender = ModUtility.GameGenderToCustomGender(spriteData.Gender),
            Name = $"Random{spriteData.Guid}",
            Spritesheet = $"{Values.GENERATED_SPRITE_PREFIX}/{spriteData.Guid}",
            Portrait = Mod.ModHelper.ModContent.GetInternalAssetName(Path.Combine("assets", "CharGen", "Portraits", "cat.png")).Name
        };

        return model;
    }
}
