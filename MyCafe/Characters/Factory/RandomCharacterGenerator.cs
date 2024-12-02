using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Monsoonsheep.StardewMods.MyCafe.Data.Customers;
using Monsoonsheep.StardewMods.MyCafe.Data.Models.Appearances;
using Monsoonsheep.StardewMods.MyCafe.Data.Models;
using StardewModdingAPI;
using MonsoonSheep.Stardew.Common;
using StardewValley;
using Netcode;
using Microsoft.Xna.Framework.Graphics;

namespace Monsoonsheep.StardewMods.MyCafe.Characters.Factory;
internal sealed class RandomCharacterGenerator
{
    private readonly IModHelper _modHelper;

    internal Dictionary<string, HairModel> Hairstyles = [];
    internal Dictionary<string, ShirtModel> Shirts = [];
    internal Dictionary<string, PantsModel> Pants = [];
    internal Dictionary<string, ShoesModel> Shoes = [];
    internal Dictionary<string, AccessoryModel> Accessories = [];
    internal Dictionary<string, OutfitModel> Outfits = [];

    internal IRawTextureData BodyBase = null!;
    internal IRawTextureData Eyes = null!;
    internal List<int[]> SkinTones = [];
    internal List<int[]> EyeColors = [];
    internal List<AppearancePaint> HairColors = [];
    internal List<AppearancePaint> ShirtColors = [];
    internal List<AppearancePaint> PantsColors = [];
    internal List<AppearancePaint> OutfitColors = [];

    internal RandomCharacterGenerator(IModHelper helper)
    {
        this._modHelper = helper;
    }

    /// <summary>
    /// Create a randomly-generated <see cref="CustomerModel"/> and a randomly-generated <see cref="GeneratedSpriteData"/>.
    /// This associates the sprite data with the Model by setting the model's Spritesheet path to a custom Guid, which is then
    /// added to a netsynced set and its texture is loaded by AssetRequested
    /// </summary>
    internal CustomerModel GenerateRandomCustomer()
    {
        GeneratedSpriteData sprite = this.GenerateRandomSpriteData();

        Mod.Cafe.GeneratedSprites[sprite.Guid] = sprite;

        CustomerModel model = new CustomerModel
        {
            Gender = ModUtility.GameGenderToCustomGender(sprite.Gender),
            Name = $"Random{sprite.Guid}",
            Spritesheet = $"{ModKeys.GENERATED_SPRITE_PREFIX}/{sprite.Guid}",
            Portrait = this._modHelper.ModContent.GetInternalAssetName(Path.Combine("assets", "CharGen", "Portraits", "cat.png")).Name
        };

        return model;
    }

    /// <summary>
    /// Create a randomized <see cref="GeneratedSpriteData"/> containing info for appearance parts, to be added to a netsynced list
    /// </summary>
    /// <returns></returns>
    private GeneratedSpriteData GenerateRandomSpriteData()
    {
        GeneratedSpriteData sprite = new();

        string guid = Guid.NewGuid().ToString();
        sprite.Guid = guid;

        Gender gender = ModUtility.GetRandomGender();
        sprite.Gender = gender;

        int[] skin = this.SkinTones.PickRandom()!;
        Color skinTone = new Color(skin[0], skin[1], skin[2]);
        sprite.SkinTone.Set(skinTone);

        int[] eyes = this.EyeColors.PickRandom()!;
        Color eyeColor = new Color(eyes[0], eyes[1], eyes[2]);
        sprite.EyeColor.Set(eyeColor);

        // Either shirts + pants or outfit
        if (Game1.random.Next(2) >= 0)
        {
            sprite.SetAppearance<ShirtModel>(
                this.GetRandomAppearance<ShirtModel>(gender).Id,
                this.ShirtColors.PickRandom()!.GetRandomPermutation());

            sprite.SetAppearance<PantsModel>(
                this.GetRandomAppearance<PantsModel>(gender).Id,
                this.PantsColors.PickRandom()!.GetRandomPermutation());
        }
        else
        {
            sprite.SetAppearance<OutfitModel>(
                this.GetRandomAppearance<OutfitModel>(Gender.Undefined).Id,
                this.OutfitColors.PickRandom()!.GetRandomPermutation());
        }

        // Hair
        sprite.SetAppearance<HairModel>(
            this.GetRandomAppearance<HairModel>(gender).Id,
            this.HairColors.PickRandom()!.GetRandomPermutation());

        return sprite;
    }

    /// <summary>
    /// This is called by both the host and clients. Composites the appearance model textures available in the <see cref="GeneratedSpriteData"/> object (synced by net fields) into a <see cref="Texture2D"/>
    /// </summary>
    internal Texture2D GenerateTexture(GeneratedSpriteData sprite)
    {
        IRawTextureData body = this.PaintBody(sprite.SkinTone.Value);
        IRawTextureData eyes = this.PaintEyes(sprite.EyeColor.Value);

        IRawTextureData? hair = this.GetTextureDataForPart<HairModel>(sprite);

        IRawTextureData? shirt = this.GetTextureDataForPart<ShirtModel>(sprite);
        IRawTextureData? pants = this.GetTextureDataForPart<PantsModel>(sprite);

        IRawTextureData? outfit = this.GetTextureDataForPart<OutfitModel>(sprite);

        IRawTextureData? shoes = this.GetTextureDataForPart<ShoesModel>(sprite);
        IRawTextureData? accessory = this.GetTextureDataForPart<AccessoryModel>(sprite);

        List<Color[]> layers = [body.Data, eyes.Data];

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

    /// <summary>
    /// Paint the body base texture with the skin tone set in this object and return it as raw texture data
    /// </summary>
    private IRawTextureData PaintBody(Color skinTone)
    {
        Color[] data = new Color[this.BodyBase.Width * this.BodyBase.Height];

        float[] multiplier = [
            skinTone.R / 255f,
            skinTone.G / 255f,
            skinTone.B / 255f
        ];

        for (int i = 0; i < this.BodyBase.Data.Length; i++)
        {
            Color baseColor = this.BodyBase.Data[i];
            if (baseColor == Color.Transparent)
                continue;

            data[i] = new Color(
                (int)(baseColor.R * multiplier[0]),
                (int)(baseColor.G * multiplier[1]),
                (int)(baseColor.B * multiplier[2]),
                baseColor.A);
        }

        RawTextureData tex = new RawTextureData(data, this.BodyBase.Width, this.BodyBase.Height);
        return tex;
    }

    /// <summary>
    /// Get the eyes texture data with the given color applied
    /// </summary>
    private IRawTextureData PaintEyes(Color color)
    {
        Color[] data = new Color[this.Eyes.Data.Length];

        float[] eyeColor = [
            color.R / 255f,
            color.G / 255f,
            color.B / 255f
        ];

        for (int i = 0; i < this.Eyes.Data.Length; i++)
        {
            Color baseColor = this.Eyes.Data[i];
            if (baseColor == Color.Transparent)
                continue;

            // Leave the bright white parts of the eye (don't color them)
            if (baseColor.R >= 200)
            {
                data[i] = baseColor;
            }
            else
            {
                data[i] = new Color(
                    (int)(baseColor.R * eyeColor[0]),
                    (int)(baseColor.G * eyeColor[1]),
                    (int)(baseColor.B * eyeColor[2]),
                    baseColor.A);
            }
        }

        RawTextureData tex = new RawTextureData(data, this.Eyes.Width, this.Eyes.Height);
        return tex;
    }

    /// <summary>
    /// Returns the texture data set in this object for the given type of appearance model. For example, Calling with HairModel will return the hair texture for this object
    /// </summary>
    /// <typeparam name="TAppearance">The type of model inheriting from <see cref="AppearanceModel"/></typeparam>
    private IRawTextureData? GetTextureDataForPart<TAppearance>(GeneratedSpriteData sprite) where TAppearance : AppearanceModel
    {
        string id = sprite.GetModelId<TAppearance>().Value;
        AppearanceModel? model = this.GetModelCollection<TAppearance>().FirstOrDefault(c => c.Id == id);
        IList<Color>? colors = sprite.GetPaint<TAppearance>();

        if (model == null)
            return null;

        if (colors is { Count: > 0 })
            return model.GetTexture(colors[0]);

        return model.GetRawTexture();
    }

    private ICollection<TAppearance> GetModelCollection<TAppearance>() where TAppearance : AppearanceModel
    {
        ICollection<TAppearance> collection = (typeof(TAppearance).Name switch
        {
            nameof(HairModel) => this.Hairstyles.Values as ICollection<TAppearance>,
            nameof(ShirtModel) => this.Shirts.Values as ICollection<TAppearance>,
            nameof(PantsModel) => this.Pants.Values as ICollection<TAppearance>,
            nameof(OutfitModel) => this.Outfits.Values as ICollection<TAppearance>,
            nameof(ShoesModel) => this.Shoes.Values as ICollection<TAppearance>,
            nameof(AccessoryModel) => this.Accessories.Values as ICollection<TAppearance>,
            _ => throw new ArgumentOutOfRangeException(nameof(TAppearance), "Bad type given. How has this occurred?")
        })!;
        return collection;
    }

    private TAppearance GetRandomAppearance<TAppearance>(Gender gender = Gender.Undefined) where TAppearance : AppearanceModel
    {
        ICollection<TAppearance> collection = this.GetModelCollection<TAppearance>();
        return collection.Where(m => m.MatchesGender(gender)).MinBy(_ => Game1.random.Next())!;
    }

    /// <summary>
    /// Alpha-blend the given raw texture data arrays over each other in order
    /// </summary>
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
}
