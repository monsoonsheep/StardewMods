using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.Data.Customers;
using MyCafe.Data.Models;
using MyCafe.Data.Models.Appearances;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.Buildings;
using xTile;

namespace MyCafe;
internal sealed class AssetManager
{
    private readonly IModHelper _modHelper;

    internal Dictionary<string, VillagerCustomerModel> VillagerCustomerModels = [];

    internal AssetManager(IModHelper helper)
    {
        this._modHelper = helper;
    }

    internal void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        // NPC Schedules
        if (e.NameWithoutLocale.IsEquivalentTo(ModKeys.ASSETS_NPCSCHEDULE))
        {
            Dictionary<string, VillagerCustomerModel> data = [];

            DirectoryInfo schedulesFolder = new DirectoryInfo(Path.Combine(this._modHelper.DirectoryPath, "assets", "VillagerSchedules"));
            foreach (FileInfo file in schedulesFolder.GetFiles())
            {
                VillagerCustomerModel model = this._modHelper.ModContent.Load<VillagerCustomerModel>(file.FullName);
                string npcName = file.Name.Replace(".json", "");
                model.NpcName = npcName;
                data[npcName] = model;
            }

            e.LoadFrom(() => data, AssetLoadPriority.Medium);
        }

        // Buildings data (Cafe and signboard)
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/Buildings"))
        {
            e.Edit(asset =>
            {
                var data = asset.AsDictionary<string, BuildingData>();
                data.Data[ModKeys.CAFE_BUILDING_BUILDING_ID] = this._modHelper.ModContent.Load<BuildingData>(Path.Combine("assets", "Buildings", "Cafe", "cafebuilding.json"));
                data.Data[ModKeys.CAFE_SIGNBOARD_BUILDING_ID] = this._modHelper.ModContent.Load<BuildingData>(Path.Combine("assets", "Buildings", "Signboard", "signboard.json"));
            }, AssetEditPriority.Early);
        }

        // Cafe building texture
        else if (e.NameWithoutLocale.IsEquivalentTo(ModKeys.CAFE_BUILDING_TEXTURE_NAME))
        {
            e.LoadFromModFile<Texture2D>(Path.Combine("assets", "Buildings", "Cafe", "cafebuilding.png"), AssetLoadPriority.Low);
        }

        // Cafe map tmx
        else if (e.NameWithoutLocale.IsEquivalentTo($"Maps/{ModKeys.CAFE_MAP_NAME}"))
        {
            e.LoadFromModFile<Map>(Path.Combine("assets", "Buildings", "Cafe", "cafemap.tmx"), AssetLoadPriority.Low);
        }

        // Signboard building texture
        else if (e.NameWithoutLocale.IsEquivalentTo(ModKeys.CAFE_SIGNBOARD_TEXTURE_NAME))
        {
            e.LoadFromModFile<Texture2D>(Path.Combine("assets", "Buildings", "Signboard", "signboard.png"), AssetLoadPriority.Low);
        }

        // Random generated sprite (with a GUID after the initial asset name)
        else if (e.NameWithoutLocale.StartsWith(ModKeys.GENERATED_SPRITE_PREFIX))
        {
            string id = e.Name.Name[(ModKeys.GENERATED_SPRITE_PREFIX.Length + 1)..];

            if (Mod.Cafe.GeneratedSprites.TryGetValue(id, out GeneratedSpriteData data))
            {
                Texture2D? sprite = data.Sprite;
                if (sprite == null)
                    Log.Error("Couldn't load texture from generated sprite data!");
                else
                    e.LoadFrom(() => sprite, AssetLoadPriority.Medium);
            }
            else
                Log.Error($"Couldn't find generate sprite data for guid {id}");
        }

        // Custom events (Added by CP component)
        else if (e.NameWithoutLocale.IsEquivalentTo($"Data/{ModKeys.MODASSET_EVENTS}"))
        {
            e.LoadFrom(() => new Dictionary<string, string>(), AssetLoadPriority.Low);
        }

        // Cafe introduction event (Inject into Data/Events/cafelocation only when we need to, the game triggers the event and the event sets a mail flag that disables this
        else if (e.NameWithoutLocale.IsDirectlyUnderPath("Data/Events") &&
                 Context.IsMainPlayer &&
                 Game1.MasterPlayer.mailReceived.Contains(ModKeys.MAILFLAG_HAS_BUILT_SIGNBOARD) &&
                 Game1.MasterPlayer.mailReceived.Contains(ModKeys.MAILFLAG_HAS_SEEN_CAFE_INTRODUCTION) == false)
        {
            GameLocation eventLocation = Game1.locations.First(l => l.isBuildingConstructed(ModKeys.CAFE_SIGNBOARD_BUILDING_ID));
            if (e.NameWithoutLocale.IsEquivalentTo($"Data/Events/{eventLocation.Name}"))
            {
                string @event = Mod.GetCafeIntroductionEvent();
                e.Edit((asset) =>
                {
                    IDictionary<string, string> data = asset.AsDictionary<string, string>().Data;
                    data[$"{ModKeys.MODASSET_EVENTS.Replace('/', '_')}_{ModKeys.EVENT_CAFEINTRODUCTION}/M 100"] = @event;
                });
            }
        }
    }

    internal void OnAssetReady(object? sender, AssetReadyEventArgs e)
    {
        // NPC Schedules
        if (e.NameWithoutLocale.IsEquivalentTo(ModKeys.ASSETS_NPCSCHEDULE))
        {
            this.VillagerCustomerModels = Game1.content.Load<Dictionary<string, VillagerCustomerModel>>(ModKeys.ASSETS_NPCSCHEDULE);
        }
    }

    internal void LoadContent(IContentPack defaultContent)
    {
        this.VillagerCustomerModels = Game1.content.Load<Dictionary<string, VillagerCustomerModel>>(ModKeys.ASSETS_NPCSCHEDULE);

        // Load default content pack included in assets folder
        this.LoadContentPack(defaultContent);

        // Load content packs
        foreach (IContentPack contentPack in this._modHelper.ContentPacks.GetOwned())
            this.LoadContentPack(contentPack);

        Mod.CharacterFactory.BodyBase = this._modHelper.ModContent.Load<IRawTextureData>(Path.Combine("assets", "CharGen", "base.png"));
        Mod.CharacterFactory.Eyes = this._modHelper.ModContent.Load<IRawTextureData>(Path.Combine("assets", "CharGen", "eyes.png"));
        Mod.CharacterFactory.SkinTones = this._modHelper.ModContent.Load<List<int[]>>(Path.Combine("assets", "CharGen", "skintones.json"));
        Mod.CharacterFactory.EyeColors = this._modHelper.ModContent.Load<List<int[]>>(Path.Combine("assets", "CharGen", "eyecolors.json"));
        Mod.CharacterFactory.HairColors = this._modHelper.ModContent.Load<List<AppearancePaint>>(Path.Combine("assets", "CharGen", "haircolors.json"));
        Mod.CharacterFactory.ShirtColors = this._modHelper.ModContent.Load<List<AppearancePaint>>(Path.Combine("assets", "CharGen", "shirtcolors.json"));
        Mod.CharacterFactory.PantsColors = this._modHelper.ModContent.Load<List<AppearancePaint>>(Path.Combine("assets", "CharGen", "pantscolors.json"));
    }

    internal void LoadContentPack(IContentPack contentPack)
    {
        Log.Info($"Loading content pack {contentPack.Manifest.Name} by {contentPack.Manifest.Author}");

        DirectoryInfo customersFolder = new(Path.Combine(contentPack.DirectoryPath, "Customers"));
        DirectoryInfo hairsFolder = new(Path.Combine(contentPack.DirectoryPath, "Hairstyles"));
        DirectoryInfo shirtsFolder = new(Path.Combine(contentPack.DirectoryPath, "Shirts"));
        DirectoryInfo pantsFolder = new(Path.Combine(contentPack.DirectoryPath, "Pants"));
        DirectoryInfo shoesFolder = new(Path.Combine(contentPack.DirectoryPath, "Shoes"));
        DirectoryInfo accessoriesFolder = new(Path.Combine(contentPack.DirectoryPath, "Accessories"));
        DirectoryInfo outfitsFolder = new(Path.Combine(contentPack.DirectoryPath, "Outfits"));

        if (customersFolder.Exists)
        {
            DirectoryInfo[] customerModels = customersFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);

            // Load customer models
            foreach (DirectoryInfo modelFolder in customerModels)
            {
                string relativePathOfModel = Path.Combine("Customers", modelFolder.Name);
                CustomerModel? model = contentPack.ReadJsonFile<CustomerModel>(Path.Combine(relativePathOfModel, "customer.json"));
                if (model == null)
                {
                    Log.Debug("Couldn't read customer.json for content pack");
                    continue;
                }
                model.Name = modelFolder.Name;
                model.Spritesheet = contentPack.ModContent.GetInternalAssetName(Path.Combine(relativePathOfModel, "customer.png")).Name;

                string portraitPath = Path.Combine(relativePathOfModel, "portrait.png");

                model.Portrait = contentPack.HasFile(portraitPath)
                    ? contentPack.ModContent.GetInternalAssetName(portraitPath).Name
                    : this._modHelper.ModContent.GetInternalAssetName(Path.Combine("assets", "CharGen", "Portraits", (string.IsNullOrEmpty(model.Portrait) ? "cat" : model.Portrait) + ".png")).Name;

                Log.Trace($"Customer model added: {model.Name}");
                Mod.CharacterFactory.Customers[$"{contentPack.Manifest.UniqueID}/{model.Name}"] = model;
            }
        }

        if (hairsFolder.Exists)
        {
            DirectoryInfo[] hairModels = hairsFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);
            foreach (DirectoryInfo modelFolder in hairModels)
            {
                HairModel? model = this.LoadAppearanceModel<HairModel>(contentPack, modelFolder.Name);
                if (model != null) 
                    Mod.CharacterFactory.Hairstyles[model.Id] = model;
            }
        }

        if (shirtsFolder.Exists)
        {
            DirectoryInfo[] shirtModels = shirtsFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);
            foreach (DirectoryInfo modelFolder in shirtModels)
            {
                ShirtModel? model = this.LoadAppearanceModel<ShirtModel>(contentPack, modelFolder.Name);
                if (model != null) 
                    Mod.CharacterFactory.Shirts[model.Id] = model;
            }
        }

        if (pantsFolder.Exists)
        {
            DirectoryInfo[] pantsModels = pantsFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);
            foreach (DirectoryInfo modelFolder in pantsModels)
            {
                PantsModel? model = this.LoadAppearanceModel<PantsModel>(contentPack, modelFolder.Name);
                if (model != null) 
                    Mod.CharacterFactory.Pants[model.Id] = model;
            }
        }

        if (shoesFolder.Exists)
        {
            DirectoryInfo[] shoesModels = shoesFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);
            foreach (DirectoryInfo modelFolder in shoesModels)
            {
                ShoesModel? model = this.LoadAppearanceModel<ShoesModel>(contentPack, modelFolder.Name);
                if (model != null) 
                    Mod.CharacterFactory.Shoes[model.Id] = model;
            }
        }

        if (accessoriesFolder.Exists)
        {
            DirectoryInfo[] accessoryModels = accessoriesFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);
            foreach (DirectoryInfo modelFolder in accessoryModels)
            {
                AccessoryModel? model = this.LoadAppearanceModel<AccessoryModel>(contentPack, modelFolder.Name);
                if (model != null) 
                    Mod.CharacterFactory.Accessories[model.Id] = model;
            }
        }

        // Load outfits
        if (outfitsFolder.Exists)
        {
            DirectoryInfo[] outfitModels = outfitsFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);
            foreach (DirectoryInfo modelFolder in outfitModels)
            {
                OutfitModel? model = this.LoadAppearanceModel<OutfitModel>(contentPack, modelFolder.Name);
                if (model != null) 
                    Mod.CharacterFactory.Outfits[model.Id] = model;
            }
        }
    }

    internal TAppearance? LoadAppearanceModel<TAppearance>(IContentPack contentPack, string modelName) where TAppearance : AppearanceModel
    {
        string filename = Utility.GetFileNameForAppearanceType<TAppearance>();
        string folderName = Utility.GetFolderNameForAppearance<TAppearance>();

        string relativePathOfModel = Path.Combine(folderName, modelName);
        TAppearance? model = contentPack.ReadJsonFile<TAppearance>(Path.Combine(relativePathOfModel, $"{filename}.json"));
        if (model == null)
        {
            Log.Debug($"Couldn't read {filename}.json for content pack {contentPack.Manifest.UniqueID}");
            return null;
        }

        model.Id = $"{contentPack.Manifest.UniqueID}/{modelName}";
        model.TexturePath = contentPack.ModContent.GetInternalAssetName(Path.Combine(relativePathOfModel, $"{filename}.png")).Name;
        model.ContentPack = contentPack;

        Log.Trace($"{folderName} model added: {model.Id}");

        return model;
    }
}
