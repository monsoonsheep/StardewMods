using FarmCafe.Models;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarmCafe.Framework.Managers
{
    internal class AssetManager
    {
        internal static void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.Name.IsDirectlyUnderPath("monsoonsheep.FarmCafe/NPCSchedules"))
            {
                string npcname = e.Name.Name.Split('/').Last();
                e.LoadFromModFile<ScheduleData>("assets/Schedules/" + npcname + ".json", AssetLoadPriority.Low);
            }
        }

        internal static void OnAssetReady(object sender, AssetReadyEventArgs e)
        {
            if (e.Name.IsDirectlyUnderPath("monsoonsheep.FarmCafe/NPCSchedules"))
            {
                string npcname = e.Name.Name.Split('/').Last();
                CafeManager.NpcSchedules[npcname] = Game1.content.Load<ScheduleData>("monsoonsheep.FarmCafe/NPCSchedules/" + npcname);
            }
        }

        internal static void LoadNpcSchedules(IModHelper helper)
        {
            var dir = new DirectoryInfo(Path.Combine(helper.DirectoryPath, "assets", "Schedules"));
            foreach (FileInfo f in dir.GetFiles())
            {
                var name = f.Name.Replace(".json", null);
                ScheduleData scheduleData = Game1.content.Load<ScheduleData>("monsoonsheep.FarmCafe/NPCSchedules/" + name);
                if (scheduleData != null)
                {
                    CafeManager.NpcSchedules[name] = scheduleData;
                }
            }
        }

        internal static void LoadContentPacks(IModHelper helper, ref List<CustomerModel> customerModels)
        {
            foreach (IContentPack contentPack in helper.ContentPacks.GetOwned())
            {
                Logger.Log($"Loading content pack {contentPack.Manifest.Name} by {contentPack.Manifest.Author}");
                var modelsInPack = new DirectoryInfo(Path.Combine(contentPack.DirectoryPath, "Customers")).GetDirectories();
                foreach (var modelFolder in modelsInPack)
                {
                    CustomerModel model = contentPack.ReadJsonFile<CustomerModel>(Path.Combine("Customers", modelFolder.Name, "customer.json"));
                    if (model == null)
                    {
                        Logger.Log("Couldn't read json for content pack");
                        continue;
                    }

                    model.Name = model.Name.Replace(" ", "");
                    model.TilesheetPath = contentPack.ModContent.GetInternalAssetName(Path.Combine("Customers", modelFolder.Name, "customer.png")).Name;

                    if (contentPack.HasFile(Path.Combine("Customers", modelFolder.Name, "portrait.png")))
                    {
                        model.Portrait = contentPack.ModContent.GetInternalAssetName(Path.Combine("Customers", modelFolder.Name, "portrait.png")).Name;
                    }
                    else
                    {
                        string portraitName = string.IsNullOrEmpty(model.Portrait) ? "cat" : model.Portrait;
                        model.Portrait = helper.ModContent.GetInternalAssetName(Path.Combine("assets", "Portraits", portraitName + ".png")).Name;
                    }
                    
                    customerModels.Add(model);
                }
            }
        }

        internal static void LoadCustomerModels(IModHelper helper, ref List<CustomerModel> customerModels)
        {
            var modelFolders = new DirectoryInfo(Path.Combine(helper.DirectoryPath, "Customers")).GetDirectories();
            foreach (var modelFolder in modelFolders)
            {
                CustomerModel model = helper.ModContent.Load<CustomerModel>(Path.Combine("Customers", modelFolder.Name, "customer.json"));
                if (model == null)
                {
                    Logger.Log("Couldn't read json for content pack");
                    continue;
                }

                model.Name = model.Name.Replace(" ", "");
                model.TilesheetPath = helper.ModContent.GetInternalAssetName(Path.Combine("Customers", modelFolder.Name, "customer.png")).Name;

                string portraitName = "cat";
                model.Portrait = helper.ModContent.GetInternalAssetName(Path.Combine("assets", "Portraits", portraitName + ".png")).Name;

                customerModels.Add(model);
            }

        }
    }
}
