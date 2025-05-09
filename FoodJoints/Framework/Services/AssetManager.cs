using Microsoft.Xna.Framework.Graphics;
using StardewMods.FoodJoints.Framework.Data.Models;
using StardewValley.GameData.BigCraftables;

namespace StardewMods.FoodJoints.Framework.Services;
internal class AssetManager
{
    internal static AssetManager Instance = null!;

    internal Dictionary<string, VillagerCustomerModel> VillagerCustomerModels = [];

    internal AssetManager()
    {
        Instance = this;

        Mod.Events.Content.AssetRequested += this.OnAssetRequested;
        Mod.Events.Content.AssetReady += this.OnAssetReady;

        this.VillagerCustomerModels = Game1.content.Load<Dictionary<string, VillagerCustomerModel>>(Values.MODASSET_NPC_VISITING_DATA);
    }

    internal void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        // Injecting cafe introduction event (Inject into Data/Events/<cafelocation> only when we need to, the game triggers the event and the event sets a mail flag that disables this
        if (e.NameWithoutLocale.IsDirectlyUnderPath("Data/Events") &&
                 Context.IsMainPlayer &&
                 Game1.MasterPlayer.mailReceived.Contains(Values.MAILFLAG_HAS_BUILT_SIGNBOARD) &&
                 Game1.MasterPlayer.mailReceived.Contains(Values.MAILFLAG_HAS_SEEN_CAFE_INTRODUCTION) == false)
        {
            GameLocation eventLocation = Game1.getFarm();

            //GameLocation eventLocation = Game1.locations.First(l => l.isBuildingConstructed(Values.CAFE_SIGNBOARD_BUILDING_ID));
            if (e.NameWithoutLocale.IsEquivalentTo($"Data/Events/{eventLocation.Name}"))
            {
                string @event = Mod.CustomEvents.GetCafeIntroductionEvent();

                e.Edit((asset) =>
                {
                    IDictionary<string, string> data = asset.AsDictionary<string, string>().Data;
                    data[$"{Values.EVENT_CAFEINTRODUCTION}/"] = @event;
                });
            }
        }

        // Mod sprites
        else if (e.NameWithoutLocale.IsEquivalentTo(Values.MODASSET_SPRITES))
        {
            e.LoadFromModFile<Texture2D>(Path.Combine("assets", "sprites.png"), AssetLoadPriority.Medium);
        }

        // NPC Schedules
        else if (e.NameWithoutLocale.IsEquivalentTo(Values.MODASSET_NPC_VISITING_DATA))
        {
            Dictionary<string, VillagerCustomerModel> data = [];

            DirectoryInfo schedulesFolder = new DirectoryInfo(Path.Combine(Mod.ModHelper.DirectoryPath, "assets", "VillagerSchedules"));
            foreach (FileInfo file in schedulesFolder.GetFiles())
            {
                VillagerCustomerModel model = Mod.ModHelper.ModContent.Load<VillagerCustomerModel>(file.FullName);
                string npcName = file.Name.Replace(".json", "");
                model.NpcName = npcName;
                data[npcName] = model;
            }

            e.LoadFrom(() => data, AssetLoadPriority.Medium);
        }

        // Signboard object
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
        {
            //e.Edit((asset) =>
            //    {
            //        IDictionary<string, BigCraftableData> data = asset.AsDictionary<string, BigCraftableData>().Data;
                    
            //        data[Values.CAFE_SIGNBOARD_OBJECT_ID] = new BigCraftableData()
            //        {
            //            Name = Values.CAFE_SIGNBOARD_OBJECT_ID,
            //            DisplayName = "Signboard",
            //            Description = "Place to start a food shop",
            //            Texture = Values.MODASSET_SPRITES,
            //            SpriteIndex = 15
            //        };
            //    },
            //    AssetEditPriority.Default);
        }
    }

    internal void OnAssetReady(object? sender, AssetReadyEventArgs e)
    {
        // NPC Schedules
        if (e.NameWithoutLocale.IsEquivalentTo(Values.MODASSET_NPC_VISITING_DATA))
        {
            this.VillagerCustomerModels = Game1.content.Load<Dictionary<string, VillagerCustomerModel>>(Values.MODASSET_NPC_VISITING_DATA);
        }
    }
}
