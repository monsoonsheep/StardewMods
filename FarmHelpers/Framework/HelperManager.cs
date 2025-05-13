using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewMods.SheepCore.Framework.Services;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Pathfinding;
using StardewValley.Tools;

namespace StardewMods.FarmHelpers.Framework;

internal class HelperManager
{
    internal static HelperManager Instance = null!;

    private LocationProvider locations = LocationProvider.Instance;

    private NPC? helper = null;
    private PathFindController? previousController = null;
    private MilkPail milkPail = null!;

    private List<FarmAnimal> animals = [];
    private int index = 0;

    internal int StartTime = 620;

    internal HelperManager()
    {
        Instance = this;

        Mod.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        Mod.Events.GameLoop.DayStarted += this.OnDayStarted;
        Mod.Events.GameLoop.TimeChanged += this.OnTimeChanged;
        Mod.Events.Content.AssetRequested += this.OnAssetRequested;
        Mod.Events.Content.AssetReady += this.OnAssetReady;

        this.milkPail = new MilkPail();
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        foreach (var pair in Game1.characterData)
        {
            if (pair.Value.CustomFields != null
                && pair.Value.CustomFields.TryGetValue("Mods/MonsoonSheep.FarmHelpers/HelperNpc", out string? val)
                && val.ToLower() == "true")
            {
                this.helper = Game1.getCharacterFromName(pair.Key);
            }
        }
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        this.animals.Clear();
        this.index = 0;
    }

    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        if (this.helper != null)
        {
            this.helper.speed = 4;

            if (e.NewTime == this.StartTime)
            {
                // search for available jobs
                // if there's even one job, move to the start point
                // at the end of the start point

                this.previousController = this.helper.controller;

                GameLocation loc = this.locations.Forest;
                Point pos = new Point(67, 4);

                if (!this.helper.MoveTo(loc, pos, this.StartJobs))
                {
                    Log.Error("Path not found");
                    // go back
                }


            }
        }
    }

    private void StartJobs(NPC helper)
    {
        // warp to Farm 41, 61, start jobs 
        // Helper jobs -> Coop take (eggs, duck feather, rabbit foot), barn milk cows, fish pond (in the future)

        Game1.warpCharacter(this.helper, this.locations.Farm, new Vector2(41, 61));

        Utility.ForEachBuilding((loc) =>
        {
            if (loc.GetIndoors() != null)
            {
                foreach (FarmAnimal animal in loc.GetIndoors().animals.Values)
                {
                    this.animals.Add(animal);
                }
            }
            
            return true;
        });


    }

    private void Next(NPC helper)
    {

    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        
    }

    private void OnAssetReady(object? sender, AssetReadyEventArgs e)
    {
        
    }
}
