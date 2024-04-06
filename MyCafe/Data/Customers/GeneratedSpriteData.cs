using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonsoonSheep.Stardew.Common;
using MyCafe.Data.Models.Appearances;
using Netcode;
using StardewModdingAPI;
using StardewValley;

namespace MyCafe.Data.Customers;
public class GeneratedSpriteData : INetObject<NetFields>, IDisposable
{
    public NetFields NetFields { get; } = new NetFields("GeneratedSpriteData");

    internal readonly NetColor SkinTone = [Color.White];
    internal readonly NetColor EyeColor = [Color.White];

    internal readonly NetString HairId = [];
    internal readonly NetArray<Color, NetColor> HairColors = new(3);
    internal readonly NetString ShirtId = [];
    internal readonly NetArray<Color, NetColor> ShirtColors = new(3);
    internal readonly NetString PantsId = [];
    internal readonly NetArray<Color, NetColor> PantsColors = new(3);

    internal readonly NetString OutfitId = [];
    internal readonly NetArray<Color, NetColor> OutfitColors = new(3);

    internal readonly NetString ShoesId = [];
    internal readonly NetString AccessoryId = [];

    internal string Guid = string.Empty;
    internal Gender Gender = Gender.Undefined;

    private Texture2D? _cachedTexture;

    internal Texture2D? Sprite
        => this._cachedTexture ??= Mod.RandomCharacterGenerator.GenerateTexture(this);

    public GeneratedSpriteData()
    {
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
        NetString field = (typeof(TAppearance).Name switch
        {
            nameof(HairModel) => this.HairId,
            nameof(ShirtModel) => this.ShirtId,
            nameof(PantsModel) => this.PantsId,
            nameof(OutfitModel) => this.OutfitId,
            nameof(ShoesModel) => this.ShoesId,
            nameof(AccessoryModel) => this.AccessoryId,
            _ => throw new ArgumentOutOfRangeException(nameof(TAppearance), "Bad type given. How has this occurred?")
        })!;
        return field;
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
        this._cachedTexture?.Dispose();
    }
}
