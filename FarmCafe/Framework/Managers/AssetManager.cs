using FarmCafe.Framework.Characters.Scheduling;
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
                e.LoadFromModFile<ScheduleData>("assets/npc_schedules/" + npcname + ".json", AssetLoadPriority.Low);
            }
        }

        internal static void OnAssetReady(object sender, AssetReadyEventArgs e)
        {
            if (e.Name.IsDirectlyUnderPath("monsoonsheep.FarmCafe/NPCSchedules"))
            {
                string npcname = e.Name.Name.Split('/').Last();
                ModEntry.NpcSchedules[npcname] = Game1.content.Load<ScheduleData>("monsoonsheep.FarmCafe/NPCSchedules/" + npcname);
            }
        }

        internal static void SetupNpcSchedules(IModHelper helper)
        {
            var dir = new DirectoryInfo(Path.Combine(helper.DirectoryPath, "assets", "npc_schedules"));
            foreach (FileInfo f in dir.GetFiles())
            {
                var name = f.Name.Replace(".json", null);
                ScheduleData scheduleData = Game1.content.Load<ScheduleData>("monsoonsheep.FarmCafe/NPCSchedules/" + name);
                if (scheduleData != null)
                {
                    ModEntry.NpcSchedules[name] = scheduleData;
                }
            }
        }
    }
}
