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
using StardewValley.GameData.Buildings;
using xTile;

namespace MyCafe;
internal class AssetManager
{
    private readonly IModHelper _modHelper;

    internal Dictionary<string, CustomerModel> Customers = [];
    internal Dictionary<string, HairModel> Hairstyles = [];
    internal Dictionary<string, ShirtModel> Shirts = [];
    internal Dictionary<string, PantsModel> Pants = [];
    internal Dictionary<string, ShoesModel> Shoes = [];
    internal Dictionary<string, AccessoryModel> Accessories = [];
    internal Dictionary<string, OutfitModel> Outfits = [];

    internal AssetManager(IModHelper helper)
    {
        this._modHelper = helper;
    }

    internal void LoadContentPack(IContentPack contentPack)
    {
        Log.Info($"Loading content pack {contentPack.Manifest.Name} by {contentPack.Manifest.Author}");

        DirectoryInfo customersFolder = new DirectoryInfo(Path.Combine(contentPack.DirectoryPath, "Customers"));
        DirectoryInfo hairsFolder = new DirectoryInfo(Path.Combine(contentPack.DirectoryPath, "Hairstyles"));
        DirectoryInfo shirtsFolder = new DirectoryInfo(Path.Combine(contentPack.DirectoryPath, "Shirts"));
        DirectoryInfo pantsFolder = new DirectoryInfo(Path.Combine(contentPack.DirectoryPath, "Pants"));
        DirectoryInfo shoesFolder = new DirectoryInfo(Path.Combine(contentPack.DirectoryPath, "Shoes"));
        DirectoryInfo accessoriesFolder = new DirectoryInfo(Path.Combine(contentPack.DirectoryPath, "Accessories"));
        DirectoryInfo outfitsFolder = new DirectoryInfo(Path.Combine(contentPack.DirectoryPath, "Outfits"));

        if (customersFolder.Exists)
        {
            DirectoryInfo[] customerModels = customersFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);

            // Load customer models
            foreach (DirectoryInfo modelFolder in customerModels)
            {
                CustomerModel? model = contentPack.ReadJsonFile<CustomerModel>(Path.Combine("Customers", modelFolder.Name, "customer.json"));
                if (model == null)
                {
                    Log.Debug("Couldn't read customer.json for content pack");
                    continue;
                }
                model.Name = modelFolder.Name;
                model.Spritesheet = contentPack.ModContent.GetInternalAssetName(Path.Combine("Customers", modelFolder.Name, "sprite.png")).Name;
                model.Portrait = contentPack.HasFile(Path.Combine("Customers", modelFolder.Name, "portrait.png"))
                    ? contentPack.ModContent.GetInternalAssetName(Path.Combine("Customers", modelFolder.Name, "portrait.png")).Name
                    : this._modHelper.ModContent.GetInternalAssetName(Path.Combine("assets", "Portraits", string.IsNullOrEmpty(model.Portrait) ? "cat" : model.Portrait + ".png")).Name;

                this.Customers[$"{contentPack.Manifest.UniqueID}/{model.Name}"] = model;
            }
        }

        if (hairsFolder.Exists)
        {
            DirectoryInfo[] hairModels = hairsFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);
            
            // Load hairstyles
            foreach (DirectoryInfo modelFolder in hairModels)
            {
                HairModel? model = contentPack.ReadJsonFile<HairModel>(Path.Combine("Hairstyles", modelFolder.Name, "hair.json"));
                if (model == null)
                {
                    Log.Debug("Couldn't read hair.json for content pack");
                    continue;
                }

                model.Id = $"{contentPack.Manifest.UniqueID}/{modelFolder.Name}";

                this.Hairstyles[model.Id] = model;
            }
        }

        if (shirtsFolder.Exists)
        {
            DirectoryInfo[] shirtModels = shirtsFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);

            // Load shirts
            foreach (DirectoryInfo modelFolder in shirtModels)
            {
                ShirtModel? model = contentPack.ReadJsonFile<ShirtModel>(Path.Combine("Shirts", modelFolder.Name, "shirt.json"));
                if (model == null)
                {
                    Log.Debug("Couldn't read shirt.json for content pack");
                    continue;
                }

                model.Id = $"{contentPack.Manifest.UniqueID}/{modelFolder.Name}";

                this.Shirts[model.Id] = model;
            }
        }

        if (pantsFolder.Exists)
        {
            DirectoryInfo[] pantsModels = pantsFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);

            // Load pants
            foreach (DirectoryInfo modelFolder in pantsModels)
            {
                PantsModel? model = contentPack.ReadJsonFile<PantsModel>(Path.Combine("Pants", modelFolder.Name, "pants.json"));
                if (model == null)
                {
                    Log.Debug("Couldn't read pants.json for content pack");
                    continue;
                }

                model.Id = $"{contentPack.Manifest.UniqueID}/{modelFolder.Name}";

                this.Pants[model.Id] = model;
            }
        }

        if (shoesFolder.Exists)
        {
            DirectoryInfo[] shoesModels = shoesFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);

            // Load shoes
            foreach (DirectoryInfo modelFolder in shoesModels)
            {
                ShoesModel? model = contentPack.ReadJsonFile<ShoesModel>(Path.Combine("Shoes", modelFolder.Name, "shoes.json"));
                if (model == null)
                {
                    Log.Debug("Couldn't read shoes.json for content pack");
                    continue;
                }

                model.Id = $"{contentPack.Manifest.UniqueID}/{modelFolder.Name}";

                this.Shoes[model.Id] = model;
            }
        }

        if (accessoriesFolder.Exists)
        {
            DirectoryInfo[] accessoryModels = accessoriesFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);

            // Load accessories
            foreach (DirectoryInfo modelFolder in accessoryModels)
            {
                AccessoryModel? model = contentPack.ReadJsonFile<AccessoryModel>(Path.Combine("Accessories", modelFolder.Name, "accessory.json"));
                if (model == null)
                {
                    Log.Debug("Couldn't read accessory.json for content pack");
                    continue;
                }

                model.Id = $"{contentPack.Manifest.UniqueID}/{modelFolder.Name}";

                this.Accessories[model.Id] = model;
            }
        }

        if (outfitsFolder.Exists)
        {
            DirectoryInfo[] outfitModels = outfitsFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);

            // Load outfits
            foreach (DirectoryInfo modelFolder in outfitModels)
            {
                OutfitModel? model = contentPack.ReadJsonFile<OutfitModel>(Path.Combine("Outfits", modelFolder.Name, "outfit.json"));
                if (model == null)
                {
                    Log.Debug("Couldn't read outfit.json for content pack");
                    continue;
                }

                model.Id = $"{contentPack.Manifest.UniqueID}/{modelFolder.Name}";

                this.Outfits[model.Id] = model;
            }
        }
    }

    internal void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        // NPC Schedules
        if (e.Name.IsDirectlyUnderPath(ModKeys.ASSETS_NPCSCHEDULE_PREFIX))
        {
            string npcname = e.Name.Name.Split('/').Last();
            VillagerCustomerData? data = this._modHelper.Data.ReadJsonFile<VillagerCustomerData>(Path.Combine("assets", "Schedules", npcname + ".json"));
            if (data != null)
                e.LoadFrom(() => data, AssetLoadPriority.Low);
        }

        // Buildings data (Cafe and signboard)
        else if (e.Name.IsEquivalentTo("Data/Buildings"))
        {
            e.Edit(asset =>
            {
                IAssetDataForDictionary<string, BuildingData> data = asset.AsDictionary<string, BuildingData>();

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
        if (e.Name.IsDirectlyUnderPath(ModKeys.ASSETS_NPCSCHEDULE_PREFIX))
        {
            string npcname = e.Name.Name.Split('/').Last();
        }
    }
}
