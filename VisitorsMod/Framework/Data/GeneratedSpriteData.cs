using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewMods.VisitorsMod.Framework.Data.Models.Appearances;

namespace StardewMods.VisitorsMod.Framework.Data;

public class GeneratedSpriteData : INetObject<NetFields>, IDisposable
{
    private static Func<GeneratedSpriteData, Texture2D> generateTextureFunction = ModEntry.RandomSprites.GenerateTexture;

    public NetFields NetFields { get; } = new NetFields("GeneratedSpriteData");

    internal Lazy<Texture2D> Sprite;
    internal string Guid = string.Empty;
    internal Gender Gender = Gender.Undefined;

    // Appearances (IDs)
    private readonly NetString HairId = [];
    private readonly NetString ShirtId = [];
    private readonly NetString PantsId = [];
    private readonly NetString OutfitId = [];
    private readonly NetString ShoesId = [];
    private readonly NetString AccessoryId = [];

    // Paint
    internal readonly NetColor SkinTone = [Color.White];
    internal readonly NetColor EyeColor = [Color.White];
    internal readonly NetArray<Color, NetColor> HairColors = new(3);
    internal readonly NetArray<Color, NetColor> ShirtColors = new(3);
    internal readonly NetArray<Color, NetColor> PantsColors = new(3);
    internal readonly NetArray<Color, NetColor> OutfitColors = new(3);

    public GeneratedSpriteData()
    {
        this.Sprite = new Lazy<Texture2D>(() => GeneratedSpriteData.generateTextureFunction(this));
        this.NetFields.SetOwner(this).AddField(this.SkinTone).AddField(this.EyeColor).AddField(this.HairId).AddField(this.HairColors).AddField(this.ShirtId).AddField(this.ShirtColors).AddField(this.PantsId).AddField(this.PantsColors)
            .AddField(this.ShoesId).AddField(this.AccessoryId).AddField(this.OutfitId).AddField(this.OutfitColors);
    }

    internal void SetAppearance<TAppearance>(string id, Color[]? colors = null) where TAppearance: AppearanceModel
    {
        this.GetModelId<TAppearance>().Set(id);

        if (colors != null)
        {
            NetArray<Color, NetColor>? colorsField = this.GetPaint<TAppearance>();
            colorsField?.Set(colors);
        }
    }

    internal NetString GetModelId<TAppearance>() where TAppearance : AppearanceModel
    {
        return (typeof(TAppearance).Name switch
        {
            nameof(HairModel) => this.HairId,
            nameof(ShirtModel) => this.ShirtId,
            nameof(PantsModel) => this.PantsId,
            nameof(OutfitModel) => this.OutfitId,
            nameof(ShoesModel) => this.ShoesId,
            nameof(AccessoryModel) => this.AccessoryId,
            _ => throw new ArgumentOutOfRangeException(nameof(TAppearance), "Bad type given. How has this occurred?")
        })!;
    }

    internal NetArray<Color, NetColor>? GetPaint<TAppearance>() where TAppearance : AppearanceModel
    {
        return typeof(TAppearance).Name switch
        {
            nameof(HairModel) => this.HairColors,
            nameof(ShirtModel) => this.ShirtColors,
            nameof(PantsModel) => this.PantsColors,
            nameof(OutfitModel) => this.OutfitColors,
            _ => null
        };
    }

    public void Dispose()
    {
        this.Sprite.Value.Dispose();
    }
}
