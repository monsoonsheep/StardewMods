using Microsoft.Xna.Framework.Graphics;
using MyCafe.Customers.Data;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Buildings;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using xTile;

namespace MyCafe;

internal sealed class AssetManager
{
    internal List<CustomerModel> CustomerModels = new List<CustomerModel>();

    internal AssetManager()
    {
        LoadStoredCustomerData();
    }

    internal static void OnAssetRequested(object sender, AssetRequestedEventArgs e)
    {
        // Schedules
        if (e.Name.IsDirectlyUnderPath(ModKeys.ASSETS_NPCSCHEDULE_PREFIX))
        {
            string npcname = e.Name.Name.Split('/').Last();
            var file = Mod.ModHelper.Data.ReadJsonFile<VillagerCustomerData>("assets/Schedules/" + npcname + ".json");
            if (file != null)
            {
                e.LoadFrom(() => file, AssetLoadPriority.Low);
            }
        }

        // Buildings
        else if (e.Name.IsEquivalentTo("Data/Buildings"))
        {
            e.Edit(asset =>
            {
                var data = asset.AsDictionary<string, BuildingData>();
                data.Data[ModKeys.CAFE_BUILDING_BUILDING_ID] = Mod.ModHelper.ModContent.Load<BuildingData>("assets/Cafe/cafebuilding.json");
                data.Data[ModKeys.CAFE_SIGNBOARD_BUILDING_ID] = Mod.ModHelper.ModContent.Load<BuildingData>("assets/Cafe/signboard.json");
            }, AssetEditPriority.Early);
        }

        // Cafe
        else if (e.Name.IsEquivalentTo(ModKeys.CAFE_BUILDING_TEXTURE_NAME))
        {
            e.LoadFromModFile<Texture2D>("assets/Cafe/cafebuilding.png", AssetLoadPriority.Medium);
        }
        else if (e.Name.IsEquivalentTo($"Maps/{ModKeys.CAFE_MAP_NAME}"))
        {
            e.LoadFromModFile<Map>("assets/Cafe/cafemap.tmx", AssetLoadPriority.Medium);
        }

        // Signboard
        else if (e.Name.IsEquivalentTo(ModKeys.CAFE_SIGNBOARD_TEXTURE_NAME))
        {
            e.LoadFromModFile<Texture2D>("assets/Cafe/signboard.png", AssetLoadPriority.Medium);
        }
    }

    internal static void OnAssetReady(object sender, AssetReadyEventArgs e)
    {
        if (e.Name.IsDirectlyUnderPath(ModKeys.ASSETS_NPCSCHEDULE_PREFIX))
        {
            string npcname = e.Name.Name.Split('/').Last();
            //Mod.Customers.VillagerCustomers.VillagerData[npcname] = Game1.content.Load<VillagerCustomerData>(ModKeys.ASSETS_NPCSCHEDULE_PREFIX + npcname);
        }
    }

    internal void LoadStoredCustomerData()
    {
        if (Game1.player.modData.TryGetValue(ModKeys.MODDATA_MENUITEMSLIST, out string menuItemsString))
        {
            var itemIds = menuItemsString.Split(' ');
            if (itemIds.Length == 0)
            {
                Log.Debug("The menu for the cafe has nothing in it!");
            }
            else
            {
                foreach (var id in itemIds)
                {
                    try
                    {
                        //Item item = new Object(id, 1);
                        //CafeManager.AddToMenu(item);
                    }
                    catch
                    {
                        Log.Debug("Invalid item ID in player's modData.", LogLevel.Warn);
                        break;

                    }
                }
            }
        }

        if (Game1.player.modData.TryGetValue(ModKeys.MODDATA_OPENCLOSETIMES, out string openCloseTimes))
        {
            Mod.Cafe.OpeningTime.Set(int.Parse(openCloseTimes.Split(' ')[0]));
            Mod.Cafe.ClosingTime.Set(int.Parse(openCloseTimes.Split(' ')[1]));
        }

        if (Game1.player.modData.TryGetValue(ModKeys.MODDATA_NPCSLASTVISITEDDATES, out string npcsLastVisited))
        {
            var split = npcsLastVisited.Split(' ');
            for (var index = 0; index < split.Length; index += 2)
            {
                string npcName = split[index];
                string[] date = split[index + 1].Split(',');

                //CafeManager.NpcVisitorSchedules[npcName].LastVisitedDate =
                //    new WorldDate(int.Parse(date[0]), (Season)int.Parse(date[1]), int.Parse(date[2]));
            }
        }
    }

    internal Dictionary<string, BusCustomerData> LoadContentPackBusCustomers(IModHelper helper, out Dictionary<string, BusCustomerData> data)
    {
        data = new Dictionary<string, BusCustomerData>();

        foreach (IContentPack contentPack in helper.ContentPacks.GetOwned())
        {
            Log.Debug($"Loading content pack {contentPack.Manifest.Name} by {contentPack.Manifest.Author}");
            var modelsInPack = new DirectoryInfo(Path.Combine(contentPack.DirectoryPath, "Customers")).GetDirectories();
            foreach (var modelFolder in modelsInPack)
            {
                CustomerModel model = contentPack.ReadJsonFile<CustomerModel>(Path.Combine("Customers", modelFolder.Name, "customer.json"));
                if (model == null)
                {
                    Log.Debug("Couldn't read json for content pack");
                    continue;
                }

                model.Name = model.Name.Replace(" ", "");
                model.Spritesheet = contentPack.ModContent.GetInternalAssetName(Path.Combine("Customers", modelFolder.Name, "sprite.png")).Name;

                if (contentPack.HasFile(Path.Combine("Customers", modelFolder.Name, "portrait.png")))
                {
                    model.PortraitName = contentPack.ModContent.GetInternalAssetName(Path.Combine("Customers", modelFolder.Name, "portrait.png")).Name;
                }
                else
                {
                    string portraitName = string.IsNullOrEmpty(model.PortraitName) ? "cat" : model.PortraitName;
                    model.PortraitName = helper.ModContent.GetInternalAssetName(Path.Combine("assets", "Portraits", portraitName + ".png")).Name;
                }

                CustomerModels.Add(model);

                data[model.Name] = new BusCustomerData()
                {
                    Model = model
                };
            }
        }

        return data;
    }
}
