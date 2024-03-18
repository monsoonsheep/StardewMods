using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonsoonSheep.Stardew.Common;
using MyCafe.Data.Customers;
using MyCafe.Data.Models;
using MyCafe.Data.Models.Appearances;
using MyCafe.Enums;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Buildings;
using xTile;

namespace MyCafe;
internal sealed class AssetManager
{
    private readonly IModHelper _modHelper;

    internal Dictionary<string, VillagerCustomerModel> VillagerVisitors = [];

    internal AssetManager(IModHelper helper)
    {
        this._modHelper = helper;
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
                    : this._modHelper.ModContent.GetInternalAssetName(Path.Combine("assets", "Portraits", string.IsNullOrEmpty(model.Portrait) ? "cat" : model.Portrait + ".png")).Name;

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

    internal T? LoadAppearanceModel<T>(IContentPack contentPack, string modelName) where T : AppearanceModel
    {
        string filename = Utility.GetFileNameForAppearanceType<T>();
        string folderName = Utility.GetFolderNameForAppearance<T>();

        string relativePathOfModel = Path.Combine(folderName, modelName);
        T? model = contentPack.ReadJsonFile<T>(Path.Combine(relativePathOfModel, $"{filename}.json"));
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

    internal void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        // NPC Schedules
        if (e.Name.IsEquivalentTo(ModKeys.ASSETS_NPCSCHEDULE))
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
        else if (e.Name.IsEquivalentTo("Data/Buildings"))
        {
            e.Edit(asset =>
            {
                var data = asset.AsDictionary<string, BuildingData>();
                data.Data[ModKeys.CAFE_BUILDING_BUILDING_ID] = this._modHelper.ModContent.Load<BuildingData>(Path.Combine("assets", "Buildings", "Cafe", "cafebuilding.json"));
                data.Data[ModKeys.CAFE_SIGNBOARD_BUILDING_ID] = this._modHelper.ModContent.Load<BuildingData>(Path.Combine("assets", "Buildings", "Signboard", "signboard.json"));
            }, AssetEditPriority.Early);
        }

        // Cafe building texture
        else if (e.Name.IsEquivalentTo(ModKeys.CAFE_BUILDING_TEXTURE_NAME))
        {
            e.LoadFromModFile<Texture2D>(Path.Combine("assets", "Buildings", "Cafe", "cafebuilding.png"), AssetLoadPriority.Low);
        }

        // Cafe map tmx
        else if (e.Name.IsEquivalentTo($"Maps/{ModKeys.CAFE_MAP_NAME}"))
        {
            e.LoadFromModFile<Map>(Path.Combine("assets", "Buildings", "Cafe", "cafemap.tmx"), AssetLoadPriority.Low);
        }

        // Signboard building texture
        else if (e.Name.IsEquivalentTo(ModKeys.CAFE_SIGNBOARD_TEXTURE_NAME))
        {
            e.LoadFromModFile<Texture2D>(Path.Combine("assets", "Buildings", "Signboard", "signboard.png"), AssetLoadPriority.Low);
        }

        else if (e.Name.StartsWith(ModKeys.GENERATED_SPRITE_PREFIX))
        {
            string id = e.Name.Name[(ModKeys.GENERATED_SPRITE_PREFIX.Length + 1)..];

            if (Mod.Cafe.GeneratedSprites.TryGetValue(id, out GeneratedSpriteData data))
            {
                Texture2D? sprite = data.Sprite;
                if (sprite == null)
                    Log.Error("Couldn't load texture from generated sprite data!");
                else
                    e.LoadFrom(() => sprite, AssetLoadPriority.Exclusive);
            }
            else
                Log.Error($"Couldn't find generate sprite data for guid {id}");
        }
    }

    internal void OnAssetReady(object? sender, AssetReadyEventArgs e)
    {
        // NPC Schedules
        if (e.Name.IsEquivalentTo(ModKeys.ASSETS_NPCSCHEDULE))
        {
            this.VillagerVisitors = Game1.content.Load<Dictionary<string, VillagerCustomerModel>>(ModKeys.ASSETS_NPCSCHEDULE);
        }
    }

    internal void LoadContent(IContentPack defaultContent)
    {
        this.VillagerVisitors = Game1.content.Load<Dictionary<string, VillagerCustomerModel>>(ModKeys.ASSETS_NPCSCHEDULE);

        // Load default content pack included in assets folder
        this.LoadContentPack(defaultContent);

        // Load content packs
        foreach (IContentPack contentPack in this._modHelper.ContentPacks.GetOwned())
            this.LoadContentPack(contentPack);
    }
}
