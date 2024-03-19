using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MyCafe.Data.Customers;
using MyCafe.Data.Models.Appearances;
using MyCafe.Data.Models;
using StardewModdingAPI;
using Microsoft.Xna.Framework.Content;
using MonsoonSheep.Stardew.Common;
using StardewValley;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.GameData.Pets;

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
    internal List<int[]> SkinTones = [];
    internal List<AppearancePaint> HairColors = [];
    internal List<AppearancePaint> ShirtColors = [];
    internal List<AppearancePaint> PantsColors = [];
    internal List<AppearancePaint> OutfitColors = [];

    internal CharacterFactory(IModHelper helper)
    {
        this._modHelper = helper;
    }

    internal void Initialize()
    {
        this.BodyBase = this._modHelper.ModContent.Load<IRawTextureData>(Path.Combine("assets", "CharGen", "base.png"));
        this.SkinTones = this._modHelper.ModContent.Load<List<int[]>>(Path.Combine("assets", "CharGen", "skintones.json"));
        this.HairColors = this._modHelper.ModContent.Load<List<AppearancePaint>>(Path.Combine("assets", "CharGen", "haircolors.json"));
        this.ShirtColors = this._modHelper.ModContent.Load<List<AppearancePaint>>(Path.Combine("assets", "CharGen", "shirtcolors.json"));
        this.PantsColors = this._modHelper.ModContent.Load<List<AppearancePaint>>(Path.Combine("assets", "CharGen", "pantscolors.json"));
    }

    internal CustomerModel CreateRandomCustomer()
    {
        string guid = Guid.NewGuid().ToString();
        Gender gender = Utility.GetRandomGender();
        int[] skin = this.SkinTones.PickRandom()!;
        Color skinTone = new Color(skin[0], skin[1], skin[2]);

        GeneratedSpriteData spriteData = new(guid);
        spriteData.SetSkinTone(skinTone);

        if (Game1.random.Next(2) == 0)
            this.SetShirtsPantsAppearance(spriteData, gender);
        else
            this.SetOutfitAppearance(spriteData, gender);

        Mod.Cafe.GeneratedSprites[guid] = spriteData;

        CustomerModel model = new CustomerModel
        {
            Gender = Utility.GameGenderToCustomGender(gender),
            Name = $"{ModKeys.CUSTOMER_NPC_NAME_PREFIX}Random{guid}",
            Spritesheet = $"{ModKeys.GENERATED_SPRITE_PREFIX}/{guid}",
            Portrait = this._modHelper.ModContent.GetInternalAssetName(Path.Combine("assets", "CharGen", "Portraits", "cat.png")).Name
        };

        return model;
    }

    private void SetOutfitAppearance(GeneratedSpriteData spriteData, Gender gender)
    {
        OutfitModel outfit = this.GetRandomAppearance<OutfitModel>(gender);
        AppearancePaint? outfitPaint = this.OutfitColors.PickRandom();
        spriteData.SetAppearance<OutfitModel>(outfit.Id, outfitPaint?.GetRandomPermutation());
    }

    private void SetShirtsPantsAppearance(GeneratedSpriteData spriteData, Gender gender)
    {
        HairModel hair = this.GetRandomAppearance<HairModel>(gender)!;
        ShirtModel shirt = this.GetRandomAppearance<ShirtModel>(gender);
        PantsModel pants = this.GetRandomAppearance<PantsModel>(gender);

        AppearancePaint? hairPaint = this.HairColors.PickRandom();
        AppearancePaint? shirtPaint = this.ShirtColors.PickRandom();
        AppearancePaint? pantsPaint = this.PantsColors.PickRandom();

        spriteData.SetAppearance<HairModel>(hair.Id, hairPaint?.GetRandomPermutation());
        spriteData.SetAppearance<ShirtModel>(shirt.Id, shirtPaint?.GetRandomPermutation());
        spriteData.SetAppearance<PantsModel>(pants.Id, pantsPaint?.GetRandomPermutation());
    }

    internal T? GetModel<T>(string id) where T : AppearanceModel
    {
        return this.GetCollection<T>().FirstOrDefault(c => c.Id == id);
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
