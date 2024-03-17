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

namespace MyCafe.Characters;
internal class CharacterFactory
{
    private readonly IModHelper _modHelper;
    internal static int LUMINOSITY_THRESHOLD_FOR_SECONDARY_COLOR = 50;

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

    internal ICollection<T> GetCollection<T>() where T : AppearanceModel
    {
        ICollection<T> collection = (typeof(T).Name switch
        {
            nameof(HairModel) => Mod.CharacterFactory.Hairstyles.Values as ICollection<T>,
            nameof(ShirtModel) => Mod.CharacterFactory.Shirts.Values as ICollection<T>,
            nameof(PantsModel) => Mod.CharacterFactory.Pants.Values as ICollection<T>,
            nameof(OutfitModel) => Mod.CharacterFactory.Outfits.Values as ICollection<T>,
            nameof(ShoesModel) => Mod.CharacterFactory.Shoes.Values as ICollection<T>,
            nameof(AccessoryModel) => Mod.CharacterFactory.Accessories.Values as ICollection<T>,
            _ => throw new ArgumentOutOfRangeException(nameof(T), "Bad type given. How has this occurred?")
        })!;
        return collection;
    }

    internal CustomerModel CreateRandomCustomer()
    {
        string guid = Guid.NewGuid().ToString();
        Gender gender = Utility.GetRandomGender();
        int[] skin = this.SkinTones.PickRandom()!;
        Color skinTone = new Color(skin[0], skin[1], skin[2]);

        HairModel hair = this.GetRandomAppearance<HairModel>(gender)!;
        ShirtModel shirt = this.GetRandomAppearance<ShirtModel>(gender);
        PantsModel pants = this.GetRandomAppearance<PantsModel>(gender);

        AppearancePaint? hairPaint = this.HairColors.PickRandom();
        AppearancePaint? shirtPaint = this.ShirtColors.PickRandom();
        AppearancePaint? pantsPaint = this.PantsColors.PickRandom();
        
        GeneratedSpriteData spriteData = new GeneratedSpriteData(guid);

        spriteData.SkinTone.Set(skinTone);
        spriteData.SetAppearance<HairModel>(hair, hairPaint?.GetRandomPermutation());
        spriteData.SetAppearance<ShirtModel>(shirt, shirtPaint?.GetRandomPermutation());
        spriteData.SetAppearance<PantsModel>(pants, pantsPaint?.GetRandomPermutation());

        Mod.Cafe.GeneratedSprites[guid] = spriteData;

        CustomerModel model = new CustomerModel
        {
            Gender = Utility.GameGenderToCustomGender(gender),
            Name = $"{ModKeys.CUSTOMER_NPC_NAME_PREFIX}Random{guid}",
            Spritesheet = $"{ModKeys.GENERATED_SPRITE_PREFIX}/{guid}",
            Portrait = this._modHelper.ModContent.GetInternalAssetName(Path.Combine("assets", "Portraits", "cat.png")).Name
        };

        return model;
    }

    internal T GetRandomAppearance<T>(Gender gender = Gender.Undefined) where T : AppearanceModel
    {
        ICollection<T> collection = this.GetCollection<T>();
        return collection.Where(m => m.MatchesGender(gender)).MinBy(_ => Game1.random.Next())!;
    }

    internal T? GetModel<T>(string id) where T : AppearanceModel
    {
        return this.GetCollection<T>().FirstOrDefault(c => c.Id == id);
    }

}
