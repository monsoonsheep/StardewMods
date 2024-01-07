using Microsoft.Xna.Framework.Graphics;
using MyCafe.Framework.Customers;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Buildings;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using xTile;
using SUtility = StardewValley.Utility;

namespace MyCafe.Framework.Managers;

internal sealed class AssetManager
{
    internal static AssetManager Instance;

    internal List<CustomerModel> CustomerModels = new List<CustomerModel>();

    internal AssetManager() => Instance = this;

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

        // Cafe
        else if (e.Name.IsEquivalentTo("Data/Buildings"))
        {
            e.Edit(asset =>
            {
                var data = asset.AsDictionary<string, BuildingData>();
                data.Data["MonsoonSheep.MyCafe_CafeBuilding"] = Mod.ModHelper.ModContent.Load<BuildingData>("assets/Cafe/cafebuilding.json");
            }, AssetEditPriority.Early);
        }
        else if (e.Name.IsEquivalentTo("MonsoonSheep.MyCafe_CafeBuildingTexture"))
        {
            e.LoadFromModFile<Texture2D>("assets/Cafe/cafebuilding.png", AssetLoadPriority.Medium);
        }
        else if (e.Name.IsEquivalentTo("Maps/MonsoonSheep.MyCafe_CafeMap"))
        {
            e.LoadFromModFile<Map>("assets/Cafe/cafemap.tmx", AssetLoadPriority.Medium);
        }
    }

    internal static void OnAssetReady(object sender, AssetReadyEventArgs e)
    {
        if (e.Name.IsDirectlyUnderPath(ModKeys.ASSETS_NPCSCHEDULE_PREFIX))
        {
            string npcname = e.Name.Name.Split('/').Last();
            Mod.Customers.VillagerCustomers.VillagerData[npcname] = Game1.content.Load<VillagerCustomerData>(ModKeys.ASSETS_NPCSCHEDULE_PREFIX + npcname);
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
            Mod.Cafe.OpeningTime = int.Parse(openCloseTimes.Split(' ')[0]);
            Mod.Cafe.ClosingTime = int.Parse(openCloseTimes.Split(' ')[1]);
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

    internal void LoadContentPacks(IModHelper helper)
    {
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

                Mod.Customers.BusCustomers.CustomersData[model.Name] = new BusCustomerData()
                {
                    Model = model
                };
            }
        }
    }
}
