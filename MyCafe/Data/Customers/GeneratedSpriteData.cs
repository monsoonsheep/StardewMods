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

    public GeneratedSpriteData(string guid) : this()
    {
        this.Guid.Set(guid);
    }

    /// <summary>
    /// Set the appearance of the given type to the given model and set the corresponding color data in this object
    /// </summary>
    /// <typeparam name="T">The type of model inheriting from <see cref="AppearanceModel"/></typeparam>
    /// <param name="id">The <see cref="AppearanceModel.Id"/> for the model</param>
    /// <param name="colors">An array of 3 colors: The main color, the secondary darker color, and the multiplier, in that order</param>
    internal void SetAppearance<T>(string id, Color[]? colors = null) where T: AppearanceModel
    {
        NetString modelField = this.GetModelIdField<T>();
        modelField.Set(id);

        if (colors != null)
        {
            NetArray<Color, NetColor>? colorsField = this.GetColorsField<T>();
            colorsField?.Set(colors);
        }
    }

    /// <summary>
    /// This is called by both the host and clients. Composites the appearance model textures available in this object (synced by net fields) into a <see cref="Texture2D"/>
    /// </summary>
    internal Texture2D? GenerateTexture()
    {
        IRawTextureData body = this.GetBody();

        IRawTextureData? hair = this.GetTextureForPart<HairModel>();

        IRawTextureData? shirt = this.GetTextureForPart<ShirtModel>();
        IRawTextureData? pants = this.GetTextureForPart<PantsModel>();

        IRawTextureData? outfit = this.GetTextureForPart<OutfitModel>();

        IRawTextureData? shoes = this.GetTextureForPart<ShoesModel>();
        IRawTextureData? accessory = this.GetTextureForPart<AccessoryModel>();

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

        Color[] finalData = Utility.CompositeTextures(layers);

        Texture2D finalTexture = new Texture2D(Game1.graphics.GraphicsDevice, body.Width, body.Height);
        finalTexture.SetData(finalData);
        return finalTexture;
    }

    /// <summary>
    /// Paint the body base texture with the skin tone set in this object and return it as raw texture data
    /// </summary>
    private IRawTextureData GetBody()
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

    /// <summary>
    /// Returns the texture data set in this object for the given type of appearance model. For example, Calling with HairModel will return the hair texture for this object
    /// </summary>
    /// <typeparam name="T">The type of model inheriting from <see cref="AppearanceModel"/></typeparam>
    private IRawTextureData? GetTextureForPart<T>() where T : AppearanceModel
    {
        string id = this.GetModelIdField<T>().Value;
        AppearanceModel? model = Mod.CharacterFactory.GetModel<T>(id);

        IList<Color>? colors = this.GetColorsField<T>();

        return model?.GetTexture(colors);
    }

    private NetString GetModelIdField<T>() where T : AppearanceModel
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

    private NetArray<Color, NetColor>? GetColorsField<T>() where T : AppearanceModel
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

    public void Dispose()
    {
        this._cachedTexture?.Dispose();
    }
}
