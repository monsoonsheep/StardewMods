#region Usings
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
using VisitorFramework.Framework.Visitors;
using VisitorFramework.Framework.Visitors.Activities;
using SUtility = StardewValley.Utility;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using StardewValley.GameData.Characters;

#endregion

namespace VisitorFramework.Framework.Managers
{
    internal static class AssetManager
    {

        internal static void LoadNpcVisitorsData()
        {
            //foreach (var d in Game1.content.Load<Dictionary<string, VisitorData>>(ModKeys.AssetVisitorsData))
            //{
            //    if (Game1.characterData.TryGetValue(d.Key, out var npcData))
            //    {
            //        npcData.SpawnIfMissing = false;
            //        d.Value.GameData = npcData;
            //        VisitorManager.LoadedVisitorData[d.Key] = d.Value;
            //    }
            //    else
            //    {
            //        Log.Warn("Trying to add a custom NPC to visitors list but couldn't find the NPC's data");
            //    }
            //}
        }

        internal static void LoadCachedVisitorData()
        {
            if (!Game1.player.modData.ContainsKey(ModKeys.ModDataVisitorDataList))
                return;

            //Dictionary<string, VisitorData> dataFromSave = JsonSerializer.Deserialize<Dictionary<string, VisitorData>>(Game1.player.modData[ModKeys.ModDataVisitorDataList]);
            //if (dataFromSave == null)
            //    return;

            //foreach (var data in dataFromSave)
            //{
            //    VisitorData existing = VisitorManager.LoadedVisitorData[data.Key];
            //    if (existing != null)
            //    {
            //        existing.ActivitiesEngagedIn = data.Value.ActivitiesEngagedIn;
            //        existing.LastVisited = data.Value.LastVisited;
            //    }
            //    else
            //    {
            //        Log.Warn("Visitor data found in save but there's no visitor loaded that has the same name.");
            //    }
            //}
        }

        /// <summary>
        /// Load content packs for this mod
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="visitorModels"></param>
        internal static void LoadContentPacks(IModHelper helper)
        {
            //foreach (IContentPack contentPack in helper.ContentPacks.GetOwned())
            //{
            //    Log.Debug($"Loading content pack {contentPack.Manifest.Name} by {contentPack.Manifest.Author}");
            //    var modelsInPack = new DirectoryInfo(Path.Combine(contentPack.DirectoryPath, "Visitors")).GetDirectories();
            //    int count = 0;
            //    foreach (var modelFolder in modelsInPack)
            //    {
            //        VisitorData data = contentPack.ReadJsonFile<VisitorData>(Path.Combine("Visitors", modelFolder.Name, "visitor.json"));

            //        if (data == null)
            //        {
            //            Log.Debug($"Invalid data for visitor model: {modelFolder.Name}");
            //            continue;
            //        }

            //        data.TexturesPath = contentPack.ModContent.GetInternalAssetName(Path.Combine(modelFolder.Parent!.Name, modelFolder.Name)).Name;

            //        if (VisitorManager.LoadedVisitorData.ContainsKey(modelFolder.Name))
            //        {
            //            Log.Debug($"Already existing visitor model: {modelFolder.Name}");
            //        }
            //        else
            //        {
            //            VisitorManager.LoadedVisitorData[modelFolder.Name] = data;
            //            count++;
            //        }
            //    }

            //    Log.Debug($"Loaded {count} visitors");
            }

        internal static void LoadActivities(IModHelper helper)
        {
            List<ActivityModel> models = helper.Data.ReadJsonFile<List<ActivityModel>>("assets/activities.json");
            if (models == null)
            {
                Log.Error("Couldn't load activities");
                return;
            }
            foreach (ActivityModel model in models)
            {
                VisitorActivity activity = new VisitorActivity
                {
                    Name = model.Id,
                    Location = model.Location,
                };

                foreach (ActionModel actionModel in model.Actions)
                {
                    if (actionModel.Behavior == null)
                    {
                        Log.Warn($"Invalid action behavior in activity {model.Id}");
                        actionModel.Behavior = "-1";
                    }

                    VisitAction action;

                    if (actionModel.Behavior.StartsWith("square"))
                    {
                        string[] split = actionModel.Behavior.Split('_');
                        int width = int.Parse(split[1]);
                        int height = int.Parse(split[2]);
                        int preferredDirection = (split.Length == 4) ? int.Parse(split[3]) : -1;

                        action = new ActionWalkInSquare(Game1.getLocationFromName(model.Location), actionModel.Position.ToPoint(), width, height, preferredDirection);
                    }
                    else if (int.TryParse(Regex.Match(actionModel.Behavior, @"^([0123])$").Value, out int d))
                    {
                        action = new VisitAction(Game1.getLocationFromName(model.Location), actionModel.Position.ToPoint(), d);
                    }
                    else
                    {
                        action = new VisitAction(Game1.getLocationFromName(model.Location), actionModel.Position.ToPoint(), -1);
                    }

                    activity.Actions.Add(action);
                }

                ActivityManager.Activities[model.Id] = activity;
            }
        }
    }
}
