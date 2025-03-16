using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewModdingAPI;
using StardewValley.Locations;

namespace StardewMods.FarmHelpers.Framework;

internal class HelperManager
{
    private HelperModel? model = null;
    private NPC? helper = null;

    internal void Initialize()
    {
        Mod.Events.GameLoop.DayStarted += this.OnDayStarted;
        Mod.Events.Content.AssetRequested += this.OnAssetRequested;
        Mod.Events.Content.AssetReady += this.OnAssetReady;

        this.model = Game1.content.Load<HelperModel?>("Mods/MonsoonSheep.FarmHelpers/HelperNpc");
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (this.model?.Name == "Itachi" && ItachiHouseFixes.BushesRemoved == false)
        {
            ItachiHouseFixes.RemoveBushes();
        }


    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Mods/MonsoonSheep.FarmHelpers/HelperNpc"))
        {
            e.LoadFrom(() => null!, AssetLoadPriority.Medium);
        }
    }

    private void OnAssetReady(object? sender, AssetReadyEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Mods/MonsoonSheep.FarmHelpers/HelperNpc"))
        {
            this.model = Game1.content.Load<HelperModel>("Mods/MonsoonSheep.FarmHelpers/HelperNpc");

            if (string.IsNullOrEmpty(this.model.Name))
            {
                this.helper = Game1.getCharacterFromName(this.model.Name);

                if (this.helper == null)
                {
                    Log.Error($"Couldn't find the NPC {this.model.Name}");
                }
            }
        }
    }
}
