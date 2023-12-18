﻿using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using StardewCafe.Framework.Customers;
using StardewValley.Tools;
using SUtility = StardewValley.Utility;

namespace StardewCafe.Framework
{
    internal class AssetManager
    {
        internal static void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.Name.IsDirectlyUnderPath("monsoonsheep.StardewCafe/NPCSchedules"))
            {
                string npcname = e.Name.Name.Split('/').Last();
                var file = ModEntry.ModHelper.Data.ReadJsonFile<ScheduleData>("assets/Schedules/" + npcname + ".json");
                if (file != null)
                {
                    e.LoadFrom(() => file, AssetLoadPriority.Low);
                }
            }
        }

        internal static void OnAssetReady(object sender, AssetReadyEventArgs e)
        {
            if (e.Name.IsDirectlyUnderPath(ModKeys.ASSETS_NPCSCHEDULE_PREFIX))
            {
                string npcname = e.Name.Name.Split('/').Last();
                CustomerManager.NpcCustomerSchedule[npcname] = Game1.content.Load<ScheduleData>(ModKeys.ASSETS_NPCSCHEDULE_PREFIX + npcname);
            }
        }

        /// <summary>
        /// Go through all villagers and load mod content for their <see cref="ScheduleData"/>
        /// </summary>
        /// <param name="helper">Mod Helper</param>
        internal static void LoadNpcSchedules(IModHelper helper)
        {
            int count = 0, doneCount = 0;
            SUtility.ForEachVillager(npc =>
            {
                try
                {
                    ScheduleData scheduleData = Game1.content.Load<ScheduleData>(ModKeys.ASSETS_NPCSCHEDULE_PREFIX + npc.Name);
                    if (scheduleData != null)
                    {
                        CustomerManager.NpcCustomerSchedule[npc.Name] = scheduleData;
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

            Logger.Log($"{doneCount} NPCs have Schedule Data. The other {count} won't visit the cafe.");
        }

        /// <summary>
        /// Load content packs for this mod
        /// </summary>
        internal static void LoadContentPacks(IModHelper helper, ref List<CustomerModel> CustomerModels)
        {
            foreach (IContentPack contentPack in helper.ContentPacks.GetOwned())
            {
                Logger.Log($"Loading content pack {contentPack.Manifest.Name} by {contentPack.Manifest.Author}");
                var modelsInPack = new DirectoryInfo(Path.Combine(contentPack.DirectoryPath, "Visitors")).GetDirectories();
                foreach (var modelFolder in modelsInPack)
                {
                    CustomerModel model = contentPack.ReadJsonFile<CustomerModel>(Path.Combine("Visitors", modelFolder.Name, "Visitor.json"));
                    if (model == null)
                    {
                        Logger.Log("Couldn't read json for content pack");
                        continue;
                    }

                    model.Name = model.Name.Replace(" ", "");
                    model.TilesheetPath = contentPack.ModContent.GetInternalAssetName(Path.Combine("Visitors", modelFolder.Name, "Visitor.png")).Name;

                    if (contentPack.HasFile(Path.Combine("Visitors", modelFolder.Name, "portrait.png")))
                    {
                        model.Portrait = contentPack.ModContent.GetInternalAssetName(Path.Combine("Visitors", modelFolder.Name, "portrait.png")).Name;
                    }
                    else
                    {
                        string portraitName = string.IsNullOrEmpty(model.Portrait) ? "cat" : model.Portrait;
                        model.Portrait = helper.ModContent.GetInternalAssetName(Path.Combine("assets", "Portraits", portraitName + ".png")).Name;
                    }

                    CustomerModels.Add(model);
                }
            }
        }

        internal static void LoadCustomerModels(IModHelper helper, ref List<CustomerModel> CustomerModels)
        {
            var modelFolders = new DirectoryInfo(Path.Combine(helper.DirectoryPath, "assets", "Visitors")).GetDirectories();
            foreach (var modelFolder in modelFolders)
            {
                CustomerModel model = helper.ModContent.Load<CustomerModel>(Path.Combine("assets", "Visitors", modelFolder.Name, "Visitor.json"));
                if (model == null)
                {
                    Logger.Log("Couldn't read json for content pack");
                    continue;
                }

                model.Name = model.Name.Replace(" ", "");
                model.TilesheetPath = helper.ModContent.GetInternalAssetName(Path.Combine("assets", "Visitors", modelFolder.Name, "Visitor.png")).Name;

                string portraitName = "Tempcat";
                model.Portrait = helper.ModContent.GetInternalAssetName(Path.Combine("assets", "Portraits", portraitName + ".png")).Name;

                CustomerModels.Add(model);
            }

            Axe axe = new() { UpgradeLevel = 3 };
            typeof(Axe).GetField("lastUser")?.SetValue(axe, Game1.player);
        }
    }
}
