#region Usings
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.GameData.Characters;
using VisitorFramework.Framework.Managers;
using VisitorFramework.Framework.Visitors;
using VisitorFramework.Patching;
using Patch = VisitorFramework.Patching.Patch;
using StardewValley.Locations;

#endregion

namespace VisitorFramework
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        internal new static IMonitor Monitor;
        internal static IModHelper ModHelper;
        internal new static IManifest ModManifest;


        /// <inheritdoc/>
        public override void Entry(IModHelper helper)
        {
            Monitor = base.Monitor;
            ModHelper = helper;
            ModManifest = base.ModManifest;
            Log.Monitor = Monitor;

            // Harmony patches
            try {
                var harmony = new Harmony(ModManifest.UniqueID);
                List<PatchList> patchLists = new List<PatchList>() { new GameLocationPatches(), new CharacterPatches() };
                patchLists.ForEach(l => l.ApplyAll(harmony));
            }
            catch (Exception e)
            {
                Log.Debug($"Couldn't patch methods - {e}", LogLevel.Error);
                return;
            }

            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.DayEnding += OnDayEnding;

            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.GameLoop.TimeChanged += OnTimeChanged;
            helper.Events.GameLoop.TimeChanged += BusManager.TenMinuteUpdate;

            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.Content.AssetReady += OnAssetReady;

            #if DEBUG
            helper.Events.Input.ButtonPressed += Debug.ButtonPress;
            #endif
        }

        private static void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            if (!Context.IsMainPlayer)
                return;

            BusManager.BusDoorOpen += BusManager.OnBusDoorOpen;
            BusManager.BusDoorOpen += delegate
            {
                VisitorManager.SpawnVisitors();
            };
        }

        private static void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsMainPlayer)
                return;
        }

        private static void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            VisitorManager.DayUpdate();

            AssetManager.LoadActivities(ModHelper);
            BusManager.DayUpdate(ModHelper);
        }

        private static void OnDayEnding(object sender, DayEndingEventArgs e)
        {
            if (!Context.IsMainPlayer) 
                return;

            VisitorManager.RemoveAllVisitors();
            //Game1.player.modData[ModKeys.ModDataVisitorDataList] = JsonSerializer.Serialize(VisitorManager.LoadedVisitorData);
        }

        private static void OnTimeChanged(object sender, TimeChangedEventArgs e)
        {
            //Game1.realMilliSecondsPerGameMinute = 300;
            //Game1.realMilliSecondsPerGameTenMinutes = 3000;
        }

        private static void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.Name.IsEquivalentTo("Data/Characters"))
            {
                e.Edit(asset =>
                    {
                        foreach (var pair in asset.AsDictionary<string, CharacterData>().Data)
                        {
                            if (pair.Value is { Home.Count: > 0 } && pair.Value.Home[0].Location.Equals("BusStop"))
                            {
                                Log.Debug($"Adding visitor {pair.Key}");
                                pair.Value.Home[0].Tile = new Point(-10, -10);
                                VisitorManager.VisitorsData.Add(pair.Key, new VisitorData() { GameData = pair.Value });
                            }
                        }
                    }, 
                    AssetEditPriority.Late);
            }
            else if (e.Name.IsDirectlyUnderPath("Characters/schedules"))
            {
                string npcName = e.Name.BaseName.Replace("Characters/schedules/", "");
                if (!VisitorManager.VisitorsData.ContainsKey(npcName))
                    return;

                e.Edit(asset =>
                    {
                        EditSchedule(npcName, asset);
                    }, 
                    AssetEditPriority.Late
                );
            }
        }

        private static void EditSchedule(string npcName, IAssetData schedule)
        {
            var assetData = schedule.AsDictionary<string, string>();

            VisitorData visitorData = VisitorManager.VisitorsData[npcName];

            Regex busTimeRegex = new Regex(@"Bus([0123])");
            foreach (var entry in assetData.Data)
            {
                var split = NPC.SplitScheduleCommands(entry.Value);
                int index = 0;

                if (split[0].StartsWith("NOT friendship"))
                {
                    index = 1;
                } else if (split[0].StartsWith("MAIL"))
                {
                    index = 2;
                }

                Match m = busTimeRegex.Match(split[index].Split(' ')[0]);
                if ((m.Success))
                {
                    int busArrivalIndex = int.Parse(m.Groups[1].Value);

                    split[index] = split[index].Replace(m.Value, BusManager.BusArrivalTimes[busArrivalIndex].ToString());

                    // Leaving time (Look at last command in schedule entry)
                    int busDepartureIndex;
                    string lastCommand = split[^1];
                    Match d = busTimeRegex.Match(lastCommand);
                    if (d.Success && split.Length > index + 1)
                    {
                        busDepartureIndex = int.Parse(d.Groups[1].Value);
                    }
                    else
                    {
                        busDepartureIndex = 4;
                        split = split.AddItem("").ToArray();
                    }

                    split[^1] = "a" + (BusManager.BusArrivalTimes[busDepartureIndex]) + $" BusStop {BusManager.BusDoorPosition.X} {BusManager.BusDoorPosition.Y} 0 BoardBus";

                    visitorData.ScheduleKeysForBusArrival.Add(entry.Key, (busArrivalIndex, busDepartureIndex));

                    // Perform Edit on the asset
                    assetData.Data[entry.Key] = string.Join('/', split);
                }
            }
        }

        private static void OnAssetReady(object sender, AssetReadyEventArgs e)
        {
            if (e.Name.IsEquivalentTo($"{ModManifest.UniqueID}/VisitorsData"))
            {
                VisitorManager.VisitorsData = Game1.content.Load<Dictionary<string, VisitorData>>($"{ModManifest.UniqueID}/VisitorsData");
            }
        }
    }
}