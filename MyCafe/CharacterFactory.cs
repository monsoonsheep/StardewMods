using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using MyCafe.Data.Customers;
using MyCafe.Data.Models.Appearances;
using MyCafe.Data.Models;
using StardewModdingAPI;
using MonsoonSheep.Stardew.Common;
using StardewValley;
using Netcode;
using Microsoft.Xna.Framework.Graphics;

namespace MyCafe;
internal sealed class CharacterFactory
{
    private readonly IModHelper _modHelper;

    internal Dictionary<string, CustomerModel> Customers = [];
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

    internal CharacterFactory(IModHelper helper)
    {
        this._modHelper = helper;
    }

    /// <summary>
    /// Create a randomly-generated <see cref="CustomerModel"/> and a randomly-generated <see cref="GeneratedSpriteData"/>.
    /// This associates the sprite data with the Model by setting the model's Spritesheet path to a custom Guid, which is then
    /// added to a netsynced set and its texture is loaded by AssetRequested
    /// </summary>
    internal CustomerModel GenerateCustomer()
    {
        GeneratedSpriteData sprite = this.GenerateRandomSpriteData();

        Mod.Cafe.GeneratedSprites[sprite.Guid] = sprite;

        CustomerModel model = new CustomerModel
        {
            Gender = Utility.GameGenderToCustomGender(sprite.Gender),
            Name = $"{ModKeys.CUSTOMER_NPC_NAME_PREFIX}Random{sprite.Guid}",
            Spritesheet = $"{ModKeys.GENERATED_SPRITE_PREFIX}/{sprite.Guid}",
            Portrait = this._modHelper.ModContent.GetInternalAssetName(Path.Combine("assets", "CharGen", "Portraits", "cat.png")).Name
        };

        return model;
    }

    internal GeneratedSpriteData GenerateRandomSpriteData()
    {
        GeneratedSpriteData sprite = new();

        string guid = Guid.NewGuid().ToString();
        sprite.Guid = guid;

        Gender gender = Utility.GetRandomGender();
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
            sprite.SetAppearanceForSprite<ShirtModel>(
                this.GetRandomAppearance<ShirtModel>(gender).Id,
                this.ShirtColors.PickRandom()!.GetRandomPermutation());

            sprite.SetAppearanceForSprite<PantsModel>(
                this.GetRandomAppearance<PantsModel>(gender).Id,
                this.PantsColors.PickRandom()!.GetRandomPermutation());
        }
        else
        {
            sprite.SetAppearanceForSprite<OutfitModel>(
                this.GetRandomAppearance<OutfitModel>(Gender.Undefined).Id,
                this.OutfitColors.PickRandom()!.GetRandomPermutation());
        }

        // Hair
        sprite.SetAppearanceForSprite<HairModel>(
            this.GetRandomAppearance<HairModel>(gender).Id,
            this.HairColors.PickRandom()!.GetRandomPermutation());

        return sprite;
    }

    
    /// <summary>
    /// This is called by both the host and clients. Composites the appearance model textures available in this object (synced by net fields) into a <see cref="Texture2D"/>
    /// </summary>
    internal Texture2D? GenerateTextureWithSpriteBatch(GeneratedSpriteData sprite)
    {
        Texture2D body = this.GetBodyTexture();
        Texture2D eyes = this.GetEyesTexture(sprite);
        Color eyeColor = sprite.EyeColor.Value;
        
        Texture2D? hair = this.GetTextureForPart<HairModel>(sprite);
        Color hairColor = sprite.HairColors[0];

        Texture2D? shirt = this.GetTextureForPart<ShirtModel>(sprite);
        Color shirtColor = sprite.ShirtColors.Count > 0 ? sprite.ShirtColors[0] : Color.White;

        Texture2D? pants = this.GetTextureForPart<PantsModel>(sprite);
        Color pantsColor = sprite.PantsColors.Count > 0 ? sprite.PantsColors[0] : Color.White;

        Texture2D? outfit = this.GetTextureForPart<OutfitModel>(sprite);
        Color outfitColor = sprite.OutfitColors.Count > 0 ? sprite.OutfitColors[0] : Color.White;

        Texture2D? shoes = this.GetTextureForPart<ShoesModel>(sprite);
        Texture2D? accessory = this.GetTextureForPart<AccessoryModel>(sprite);

        // Verify not null
        if (!string.IsNullOrEmpty(sprite.OutfitId.Value) && outfit == null)
        {
            Log.Error("Couldn't load outfit texture");
            return null;
        }
        if ((!string.IsNullOrEmpty(sprite.ShirtId.Value) || !string.IsNullOrEmpty(sprite.PantsId.Value)) && (shirt == null || pants == null))
        {
            Log.Error("Couldn't load shirt/pants textures");
            return null;
        }

        Texture2D finalSprite = new Texture2D(Game1.graphics.GraphicsDevice, 64, 160);
        RenderTarget2D renderTarget = new RenderTarget2D(Game1.graphics.GraphicsDevice, 64, 160);
        SpriteBatch sb = new SpriteBatch(Game1.graphics.GraphicsDevice);
        RenderTargetBinding[] temp = [];
        Game1.graphics.GraphicsDevice.GetRenderTargets(temp);
        Game1.graphics.GraphicsDevice.SetRenderTarget(renderTarget);
        Game1.graphics.GraphicsDevice.Clear(Color.Transparent);
        BlendState previousBlendState = Game1.graphics.GraphicsDevice.BlendState;
        Game1.graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;

        sb.Begin(SpriteSortMode.Immediate);

        sb.Draw(body, Vector2.Zero, sprite.SkinTone.Value);
        sb.Draw(eyes, Vector2.Zero, sprite.EyeColor.Value);


        if (shoes != null)
            sb.Draw(shoes, Vector2.Zero, Color.White);

        if (pants != null)
            sb.Draw(pants, Vector2.Zero, pantsColor);

        if (shirt != null)
            sb.Draw(shirt, Vector2.Zero, shirtColor);

        if (outfit != null)
            sb.Draw(outfit, Vector2.Zero, outfitColor);

        if (hair != null)
            sb.Draw(hair, Vector2.Zero, hairColor);

        if (accessory != null)
            sb.Draw(accessory, Vector2.Zero, Color.White);

        sb.End();

        Color[] data = new Color[body.Width * body.Height];
        renderTarget.GetData(data, 0, body.Width * body.Height);
        finalSprite.SetData(data);

        sb.Dispose();
        renderTarget.Dispose();
        body.Dispose();
        eyes.Dispose();
        hair?.Dispose();
        pants?.Dispose();
        shirt?.Dispose();
        outfit?.Dispose();
        shoes?.Dispose();
        accessory?.Dispose();
        Game1.graphics.GraphicsDevice.BlendState = previousBlendState;
        Game1.graphics.GraphicsDevice.SetRenderTargets(temp);

        return finalSprite;
    }

    /// <summary>
    /// This is called by both the host and clients. Composites the appearance model textures available in this object (synced by net fields) into a <see cref="Texture2D"/>
    /// </summary>
    internal Texture2D? GenerateTextureManual(GeneratedSpriteData sprite)
    {
        IRawTextureData body = this.GetBodyData(sprite);
        IRawTextureData eyes = this.GetEyesData(sprite);

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

        Color[] finalData = Utility.CompositeTextures(layers);

        Texture2D finalTexture = new Texture2D(Game1.graphics.GraphicsDevice, body.Width, body.Height);
        finalTexture.SetData(finalData);
        return finalTexture;
    }

    /// <summary>
    /// Paint the body base texture with the skin tone set in this object and return it as raw texture data
    /// </summary>
    private IRawTextureData GetBodyData(GeneratedSpriteData sprite)
    {
        Color[] data = new Color[this.BodyBase.Width * this.BodyBase.Height];

        float[] skinColor = [
            sprite.SkinTone.Value.R / 255f,
            sprite.SkinTone.Value.G / 255f,
            sprite.SkinTone.Value.B / 255f
        ];
        
        for (int i = 0; i < this.BodyBase.Data.Length; i++)
        {
            Color baseColor = this.BodyBase.Data[i];
            if (baseColor == Color.Transparent)
                continue;

            data[i] = new Color((int) (baseColor.R * skinColor[0]), (int) (baseColor.G * skinColor[1]), (int) (baseColor.B * skinColor[2]), baseColor.A);
        }

        RawTextureData tex = new RawTextureData(data, this.BodyBase.Width, this.BodyBase.Height);
        return tex;
    }

    private Texture2D GetBodyTexture()
    {
        Texture2D tex = new Texture2D(Game1.graphics.GraphicsDevice, this.BodyBase.Width, this.BodyBase.Height);
        tex.SetData(this.BodyBase.Data);
        return tex;
    }

    private IRawTextureData GetEyesData(GeneratedSpriteData sprite)
    {
        Color[] data = new Color[this.Eyes.Data.Length];

        float[] eyeColor = [
            sprite.EyeColor.Value.R / 255f,
            sprite.EyeColor.Value.G / 255f,
            sprite.EyeColor.Value.B / 255f
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
                    (int) (baseColor.R * eyeColor[0]),
                    (int) (baseColor.G * eyeColor[1]),
                    (int) (baseColor.B * eyeColor[2]),
                    baseColor.A);
            }
        }

        RawTextureData tex = new RawTextureData(data, this.Eyes.Width, this.Eyes.Height);
        return tex;
    }

    private Texture2D GetEyesTexture(GeneratedSpriteData sprite)
    {
        Texture2D tex = new Texture2D(Game1.graphics.GraphicsDevice, this.Eyes.Width, this.Eyes.Height);
        tex.SetData(this.Eyes.Data);
        return tex;
    }

    /// <summary>
    /// Returns the texture data set in this object for the given type of appearance model. For example, Calling with HairModel will return the hair texture for this object
    /// </summary>
    /// <typeparam name="TAppearance">The type of model inheriting from <see cref="AppearanceModel"/></typeparam>
    private IRawTextureData? GetTextureDataForPart<TAppearance>(GeneratedSpriteData sprite) where TAppearance : AppearanceModel
    {
        string id = sprite.GetModelIdField<TAppearance>().Value;
        AppearanceModel? model = this.GetModel<TAppearance>(id);
        IList<Color>? colors = sprite.GetPaint<TAppearance>();

        if (model == null)
            return null;

        if (colors == null || colors.Count == 0)
        {
            return model.GetTextureNoColor();
        }
        else
        {
            return model.GetTextureWithMultiplyColors(colors[0]);
        }
    }

    /// <summary>
    /// Returns the texture data set in this object for the given type of appearance model. For example, Calling with HairModel will return the hair texture for this object
    /// </summary>
    /// <typeparam name="TAppearance">The type of model inheriting from <see cref="AppearanceModel"/></typeparam>
    private Texture2D? GetTextureForPart<TAppearance>(GeneratedSpriteData sprite) where TAppearance : AppearanceModel
    {
        string id = sprite.GetModelIdField<TAppearance>().Value;
        AppearanceModel? model = this.GetModel<TAppearance>(id);
        IRawTextureData? data = model?.GetTextureNoColor();
        if (data != null)
        {
            Texture2D tex = new Texture2D(Game1.graphics.GraphicsDevice, data.Width, data.Height);
            tex.SetData(data.Data);
            return tex;
        }

        return null;
    }

    private TAppearance? GetModel<TAppearance>(string id) where TAppearance : AppearanceModel
    {
        return this.GetCollection<TAppearance>().FirstOrDefault(c => c.Id == id);
    }

    private ICollection<TAppearance> GetCollection<TAppearance>() where TAppearance : AppearanceModel
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
        ICollection<TAppearance> collection = this.GetCollection<TAppearance>();
        return collection.Where(m => m.MatchesGender(gender)).MinBy(_ => Game1.random.Next())!;
    }
}
