using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.Data.Customers;
using MyCafe.Data.Models;
using MyCafe.Data.Models.Appearances;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Buildings;
using xTile;

namespace MyCafe;
internal class AssetManager
{
    private readonly IModHelper _modHelper;

    internal Dictionary<string, VillagerCustomerModel> VillagerVisitors = [];

    internal Dictionary<string, CustomerModel> Customers = [];
    internal Dictionary<string, HairModel> Hairstyles = [];
    internal Dictionary<string, ShirtModel> Shirts = [];
    internal Dictionary<string, PantsModel> Pants = [];
    internal Dictionary<string, ShoesModel> Shoes = [];
    internal Dictionary<string, AccessoryModel> Accessories = [];
    internal Dictionary<string, OutfitModel> Outfits = [];

    internal Dictionary<string, Texture2D> GeneratedTexturesInUse = [];

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
                this.Customers[$"{contentPack.Manifest.UniqueID}/{model.Name}"] = model;
            }
        }

        if (hairsFolder.Exists)
        {
            DirectoryInfo[] hairModels = hairsFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);
            
            // Load hairstyles
            foreach (DirectoryInfo modelFolder in hairModels)
            {
                string relativePathOfModel = Path.Combine("Hairstyles", modelFolder.Name);
                HairModel? model = contentPack.ReadJsonFile<HairModel>(Path.Combine(relativePathOfModel, "hair.json"));
                if (model == null)
                {
                    Log.Debug("Couldn't read hair.json for content pack");
                    continue;
                }

                model.Id = $"{contentPack.Manifest.UniqueID}/{modelFolder.Name}";
                model.TexturePath = Path.Combine(contentPack.DirectoryPath, relativePathOfModel, "hair.png");

                Log.Trace($"Hair model added: {model.Id}");
                this.Hairstyles[model.Id] = model;
            }
        }

        if (shirtsFolder.Exists)
        {
            DirectoryInfo[] shirtModels = shirtsFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);

            // Load shirts
            foreach (DirectoryInfo modelFolder in shirtModels)
            {
                string relativePathOfModel = Path.Combine("Shirts", modelFolder.Name);
                ShirtModel? model = contentPack.ReadJsonFile<ShirtModel>(Path.Combine(relativePathOfModel, "shirt.json"));
                if (model == null)
                {
                    Log.Debug("Couldn't read shirt.json for content pack");
                    continue;
                }

                model.Id = $"{contentPack.Manifest.UniqueID}/{modelFolder.Name}";
                model.TexturePath = Path.Combine(contentPack.DirectoryPath, relativePathOfModel, "shirt.png");

                Log.Trace($"Shirt model added: {model.Id}");
                this.Shirts[model.Id] = model;
            }
        }

        if (pantsFolder.Exists)
        {
            DirectoryInfo[] pantsModels = pantsFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);

            // Load pants
            foreach (DirectoryInfo modelFolder in pantsModels)
            {
                string relativePathOfModel = Path.Combine("Pants", modelFolder.Name);
                PantsModel? model = contentPack.ReadJsonFile<PantsModel>(Path.Combine(relativePathOfModel, "pants.json"));
                if (model == null)
                {
                    Log.Debug("Couldn't read pants.json for content pack");
                    continue;
                }

                model.Id = $"{contentPack.Manifest.UniqueID}/{modelFolder.Name}";
                model.TexturePath = Path.Combine(contentPack.DirectoryPath, relativePathOfModel, "pants.png");

                Log.Trace($"Pants model added: {model.Id}");
                this.Pants[model.Id] = model;
            }
        }

        if (shoesFolder.Exists)
        {
            DirectoryInfo[] shoesModels = shoesFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);

            // Load shoes
            foreach (DirectoryInfo modelFolder in shoesModels)
            {
                string relativePathOfModel = Path.Combine("Shoes", modelFolder.Name);
                ShoesModel? model = contentPack.ReadJsonFile<ShoesModel>(Path.Combine(relativePathOfModel, "shoes.json"));
                if (model == null)
                {
                    Log.Debug("Couldn't read shoes.json for content pack");
                    continue;
                }

                model.Id = $"{contentPack.Manifest.UniqueID}/{modelFolder.Name}";
                model.TexturePath = Path.Combine(contentPack.DirectoryPath, relativePathOfModel, "shoes.png");

                Log.Trace($"Shoes model added: {model.Id}");
                this.Shoes[model.Id] = model;
            }
        }

        if (accessoriesFolder.Exists)
        {
            DirectoryInfo[] accessoryModels = accessoriesFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);

            // Load accessories
            foreach (DirectoryInfo modelFolder in accessoryModels)
            {
                string relativePathOfModel = Path.Combine("Accessories", modelFolder.Name);
                AccessoryModel? model = contentPack.ReadJsonFile<AccessoryModel>(Path.Combine(relativePathOfModel, "accessory.json"));
                if (model == null)
                {
                    Log.Debug("Couldn't read accessory.json for content pack");
                    continue;
                }

                model.Id = $"{contentPack.Manifest.UniqueID}/{modelFolder.Name}";
                model.TexturePath = Path.Combine(contentPack.DirectoryPath, relativePathOfModel, "accessory.png");

                Log.Trace($"Accessory model added: {model.Id}");
                this.Accessories[model.Id] = model;
            }
        }

        if (outfitsFolder.Exists)
        {
            DirectoryInfo[] outfitModels = outfitsFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);

            // Load outfits
            foreach (DirectoryInfo modelFolder in outfitModels)
            {
                string relativePathOfModel = Path.Combine("Outfits", modelFolder.Name);
                OutfitModel? model = contentPack.ReadJsonFile<OutfitModel>(Path.Combine(relativePathOfModel, "outfit.json"));
                if (model == null)
                {
                    Log.Debug("Couldn't read outfit.json for content pack");
                    continue;
                }

                model.Id = $"{contentPack.Manifest.UniqueID}/{modelFolder.Name}";
                model.TexturePath = Path.Combine(contentPack.DirectoryPath, relativePathOfModel, "outfit.png");

                Log.Trace($"Outfit model added: {model.Id}");
                this.Outfits[model.Id] = model;
            }
        }
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
                string name = file.Name.Replace(".json", "");
                VillagerCustomerModel model = this._modHelper.ModContent.Load<VillagerCustomerModel>(file.FullName);
                data[name] = model;
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
    }

    internal void OnAssetReady(object? sender, AssetReadyEventArgs e)
    {
        // NPC Schedules
        if (e.Name.IsEquivalentTo(ModKeys.ASSETS_NPCSCHEDULE))
        {
            this.VillagerVisitors = Game1.content.Load<Dictionary<string, VillagerCustomerModel>>(ModKeys.ASSETS_NPCSCHEDULE);
        }
    }

    internal List<CustomerModel> GetRandomCustomerModels(int count = 1)
    {
        return this.Customers.Values.OrderBy(_ => Game1.random.Next()).Take(count).ToList();
    }
}
