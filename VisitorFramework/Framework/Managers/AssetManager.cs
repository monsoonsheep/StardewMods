using System;
using VisitorFramework.Models;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using StardewValley.Tools;
using SUtility = StardewValley.Utility;

namespace VisitorFramework.Framework.Managers
{
    internal class AssetManager
    {
        /// <summary>
        /// Load content packs for this mod
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="visitorModels"></param>
        internal static void LoadContentPacks(IModHelper helper, ref List<VisitorModel> visitorModels)
        {
            foreach (IContentPack contentPack in helper.ContentPacks.GetOwned())
            {
                Logger.Log($"Loading content pack {contentPack.Manifest.Name} by {contentPack.Manifest.Author}");
                var modelsInPack = new DirectoryInfo(Path.Combine(contentPack.DirectoryPath, "Visitors")).GetDirectories();
                foreach (var modelFolder in modelsInPack)
                {
                    VisitorModel model = contentPack.ReadJsonFile<VisitorModel>(Path.Combine("Visitors", modelFolder.Name, "Visitor.json"));
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
                    
                    visitorModels.Add(model);
                }
            }
        }

        internal static void LoadVisitorModels(IModHelper helper, ref List<VisitorModel> visitorModels)
        {
            var modelFolders = new DirectoryInfo(Path.Combine(helper.DirectoryPath, "assets", "Visitors")).GetDirectories();
            foreach (var modelFolder in modelFolders)
            {
                VisitorModel model = helper.ModContent.Load<VisitorModel>(Path.Combine("assets", "Visitors", modelFolder.Name, "Visitor.json"));
                if (model == null)
                {
                    Logger.Log("Couldn't read json for content pack");
                    continue;
                }

                model.Name = model.Name.Replace(" ", "");
                model.TilesheetPath = helper.ModContent.GetInternalAssetName(Path.Combine("assets", "Visitors", modelFolder.Name, "Visitor.png")).Name;

                string portraitName = "Tempcat";
                model.Portrait = helper.ModContent.GetInternalAssetName(Path.Combine("assets", "Portraits", portraitName + ".png")).Name;

                visitorModels.Add(model);
            }

            Axe axe = new() { UpgradeLevel = 3 };
            typeof(Axe).GetField("lastUser")?.SetValue(axe, Game1.player);
        }
    }
}
