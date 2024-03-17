using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonsoonSheep.Stardew.Common.Utilities;
using MyCafe.Characters;
using MyCafe.Data.Models.Appearances;
using Netcode;
using StardewModdingAPI;
using StardewValley;

namespace MyCafe.Data.Customers;
public class GeneratedSpriteData : INetObject<NetFields>, IDisposable
{
    public NetFields NetFields { get; } = new NetFields("GeneratedSpriteData");

    public NetString Guid = [];

    public NetColor SkinTone = [Color.White];

    public NetString HairId = [];
    public NetArray<Color, NetColor> HairColors = new(3);
    public NetString ShirtId = [];
    public NetArray<Color, NetColor> ShirtColors = new(3);
    public NetString PantsId = [];
    public NetArray<Color, NetColor> PantsColors = new(3);

    public NetString OutfitId = [];
    public NetArray<Color, NetColor> OutfitColors = new(3);

    public NetString ShoesId = [];
    public NetString AccessoryId = [];

    private Texture2D? _cachedTexture;

    internal Texture2D? Sprite
        => this._cachedTexture ??= this.GenerateTexture();

    public GeneratedSpriteData()
    {
        this.NetFields.SetOwner(this).AddField(this.Guid).AddField(this.HairId).AddField(this.HairColors).AddField(this.ShirtId).AddField(this.ShirtColors).AddField(this.PantsId).AddField(this.PantsColors)
            .AddField(this.ShoesId).AddField(this.AccessoryId).AddField(this.OutfitId).AddField(this.OutfitColors);
    }

    internal GeneratedSpriteData(string guid) : this()
    {
        this.Guid.Set(guid);
    }

    internal void SetAppearance<T>(AppearanceModel model, Color[]? colors = null) where T: AppearanceModel
    {
        NetString field = this.GetModelId<T>();
        field.Set(model.Id);
        if (colors != null)
            this.GetColors<T>()?.Set(colors);
    }

    private NetString GetModelId<T>() where T : AppearanceModel
    {
        NetString field = (typeof(T).Name switch
        {
            nameof(HairModel) => this.HairId,
            nameof(ShirtModel) => this.ShirtId,
            nameof(PantsModel) => this.PantsId,
            nameof(OutfitModel) => this.OutfitId,
            nameof(ShoesModel) => this.ShoesId,
            nameof(AccessoryModel) => this.AccessoryId,
            _ => throw new ArgumentOutOfRangeException(nameof(T), "Bad type given. How has this occurred?")
        })!;
        return field;
    }

    internal NetArray<Color, NetColor>? GetColors<T>() where T : AppearanceModel
    {
        return typeof(T).Name switch
        {
            nameof(HairModel) => this.HairColors,
            nameof(ShirtModel) => this.ShirtColors,
            nameof(PantsModel) => this.PantsColors,
            nameof(OutfitModel) => this.OutfitColors,
            _ => null
        };
    }

    internal IRawTextureData? GetTexture<T>() where T : AppearanceModel
    {
        return this.GetModel<T>()?.GetTexture(this.GetColors<T>());
    }

    internal T? GetModel<T>() where T : AppearanceModel
    {
        string id = this.GetModelId<T>().Value;
        return Mod.CharacterFactory.GetModel<T>(id);
    }

    internal IRawTextureData GetBody()
    {
        IRawTextureData BodyBase = Mod.CharacterFactory.BodyBase;
        Color[] data = new Color[BodyBase.Width * BodyBase.Height];

        float[] skinColor = [
            this.SkinTone.Value.R / 255f,
            this.SkinTone.Value.G / 255f,
            this.SkinTone.Value.B / 255f
        ];
        
        for (int i = 0; i < BodyBase.Data.Length; i++)
        {
            Color baseColor = BodyBase.Data[i];
            if (baseColor == Color.Transparent)
                continue;

            data[i] = new Color((int) (baseColor.R * skinColor[0]), (int) (baseColor.G * skinColor[1]), (int) (baseColor.B * skinColor[2]), baseColor.A);
        }

        RawTextureData tex = new RawTextureData(data, BodyBase.Width, BodyBase.Height);
        return tex;
    }

    internal Texture2D? GenerateTexture()
    {
        IRawTextureData body = this.GetBody();

        IRawTextureData? hair = this.GetTexture<HairModel>();

        IRawTextureData? shirt = this.GetTexture<ShirtModel>();
        IRawTextureData? pants = this.GetTexture<PantsModel>();

        IRawTextureData? outfit = this.GetTexture<OutfitModel>();

        IRawTextureData? shoes = this.GetTexture<ShoesModel>();
        IRawTextureData? accessory = this.GetTexture<AccessoryModel>();

        // Verify not null
        if (!string.IsNullOrEmpty(this.OutfitId.Value) && outfit == null)
        {
            Log.Error("Couldn't load outfit texture");
            return null;
        }
        if ((!string.IsNullOrEmpty(this.ShirtId.Value) || !string.IsNullOrEmpty(this.PantsId.Value)) && (shirt == null || pants == null))
        {
            Log.Error("Couldn't load shirt/pants textures");
            return null;
        }

        List<Color[]> layers = [body.Data];

        if (shoes != null)
            layers.Add(shoes.Data);

        if (pants != null)
            layers.Add(pants.Data);

        if (shirt != null)
            layers.Add(shirt.Data);

        if (outfit != null)
            layers.Add(outfit.Data);

        if (hair != null)
            layers.Add(hair.Data);

        if (accessory != null)
            layers.Add(accessory.Data);

        Color[] finalData = CompositeTextures(layers);

        Texture2D finalTexture = new Texture2D(Game1.graphics.GraphicsDevice, body.Width, body.Height);
        finalTexture.SetData(finalData);
        return finalTexture;
    }

    internal static Color[] CompositeTextures(List<Color[]> textures)
    {
        Color[] finalData = new Color[textures[0].Length];

        for (int i = 0; i < textures[0].Length; i++)
        {
            Color below = textures[0][i];

            int r = below.R;
            int g = below.G;
            int b = below.B;
            int a = below.A;

            foreach (Color[] layer in textures)
            {
                Color above = layer[i];

                float alphaBelow = 1 - (above.A / 255f);

                r = (int) (above.R + (r * alphaBelow));
                g = (int) (above.G + (g * alphaBelow));
                b = (int) (above.B + (b * alphaBelow));
                a = Math.Max(a, above.A);
            }

            finalData[i] = new Color(r, g, b, a);
        }

        return finalData;
    }

    public void Dispose()
    {
        this._cachedTexture?.Dispose();
    }
}
