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
    private LocationProvider locations = LocationProvider.Instance;

    private NPC? helper = null;
    private PathFindController? oldController = null;

    private MilkPail milkPail = null!;

    private List<FarmAnimal> animals = [];
    private int index = 0;

    internal int StartTime = 620;

    internal void Initialize()
    {
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
                && pair.Value.CustomFields.TryGetValue("Mods/MonsoonSheep.FarmHelpers/HelperNpc", out string? val))
            {
                this.helper = Game1.getCharacterFromName(pair.Key);
            }
        }
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (Game1.characterData.ContainsKey($"{Mod.Manifest.UniqueID}_Itachi") && ItachiHouseFixes.BushesRemoved == false)
        {
            ItachiHouseFixes.RemoveBushes();
        }

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

                this.MoveTo(this.locations.Forest, new Point(67, 4), this.StartJobs);
            }
        }
    }

    private void StartJobs(NPC helper)
    {
        // stop at Farm 41, 64, start jobs 
        // Helper jobs -> Coop take (eggs, duck feather, rabbit foot), barn milk cows, fish pond (in the future)

        Utility.ForEachLocation(delegate (GameLocation loc)
        {
            foreach (FarmAnimal animal in loc.animals.Values)
            {
                this.animals.Add(animal);
            }
            return true;
        });


    }

    private void Next(NPC helper)
    {

    }

    private bool MoveTo(GameLocation location, Point position, Action<NPC> endBehavior)
    {
        if (this.helper == null)
            throw new NullReferenceException("Helper is null");

        if (!this.helper.CanPath(location, position, out Stack<Point>? path))
            return false;

        this.oldController = this.helper.controller;

        this.helper.MoveTo(path, endBehavior);

        return true;
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        
    }

    private void OnAssetReady(object? sender, AssetReadyEventArgs e)
    {
        
    }
}
