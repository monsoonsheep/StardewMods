using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.Framework.Customers;
using StardewValley.Tools;
using SUtility = StardewValley.Utility;
using StardewValley.GameData.Buildings;
using xTile;

namespace MyCafe.Framework.Managers;

internal sealed class AssetManager
{
    internal static AssetManager Instance;

    internal static List<CustomerModel> CustomerModels = new List<CustomerModel>();
    internal static Texture2D Sprites;

    internal AssetManager() => Instance = this;

    internal static void OnAssetRequested(object sender, AssetRequestedEventArgs e)
    {
        // Schedules
        if (e.Name.IsDirectlyUnderPath(ModKeys.ASSETS_NPCSCHEDULE_PREFIX))
        {
            string npcname = e.Name.Name.Split('/').Last();
            var file = Mod.ModHelper.Data.ReadJsonFile<ScheduleData>("assets/Schedules/" + npcname + ".json");
            if (file != null)
            {
                e.LoadFrom(() => file, AssetLoadPriority.Low);
            }
        }

        // Cafe
        else if (e.Name.IsEquivalentTo("Data/Buildings")) {
            e.Edit(asset => {
                var data = asset.AsDictionary<string, BuildingData>();
                data.Data["MonsoonSheep.MyCafe_CafeBuilding"] = Mod.ModHelper.ModContent.Load<BuildingData>("assets/Cafe/cafebuilding.json");
            }, AssetEditPriority.Early);
        } else if (e.Name.IsEquivalentTo("MonsoonSheep.MyCafe_CafeBuildingTexture")) {
            e.LoadFromModFile<Texture2D>("assets/Cafe/cafebuilding.png", AssetLoadPriority.Medium);
        } else if (e.Name.IsEquivalentTo("Maps/MonsoonSheep.MyCafe_CafeMap")) {
            e.LoadFromModFile<Map>("assets/Cafe/cafemap.tmx", AssetLoadPriority.Medium);
        }
    }

    internal static void OnAssetReady(object sender, AssetReadyEventArgs e)
    {
        if (e.Name.IsDirectlyUnderPath(ModKeys.ASSETS_NPCSCHEDULE_PREFIX))
        {
            string npcname = e.Name.Name.Split('/').Last();
            CustomerManager.Instance.VillagerCustomerSchedules[npcname] = Game1.content.Load<ScheduleData>(ModKeys.ASSETS_NPCSCHEDULE_PREFIX + npcname);
        }
    }

    internal void LoadValuesFromModData()
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
            CafeManager.Instance.OpeningTime = int.Parse(openCloseTimes.Split(' ')[0]);
            CafeManager.Instance.ClosingTime = int.Parse(openCloseTimes.Split(' ')[1]);
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

    internal void LoadNpcSchedules(IModHelper helper)
    {
        int count = 0, doneCount = 0;
        SUtility.ForEachVillager(npc =>
        {
            try
            {
                ScheduleData scheduleData = Game1.content.Load<ScheduleData>(ModKeys.ASSETS_NPCSCHEDULE_PREFIX + npc.Name);
                if (scheduleData != null)
                {
                    CustomerManager.Instance.VillagerCustomerSchedules[npc.Name] = scheduleData;
                    doneCount++;
                }
            }
            catch
            {
                // ignored
            }

            count++;
            return true;
        });

        Log.Debug($"{doneCount} NPCs have Schedule Data. The other {count} won't visit the cafe.");
    }

    internal static void LoadContentPacks(IModHelper helper)
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
                model.TilesheetPath = contentPack.ModContent.GetInternalAssetName(Path.Combine("Customers", modelFolder.Name, "sprite.png")).Name;

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
            }
        }
    }
}
