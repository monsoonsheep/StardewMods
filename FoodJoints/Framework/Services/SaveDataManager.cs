using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley.Inventories;
using System.Xml.Serialization;
using StardewMods.FoodJoints.Framework.Data;
using StardewMods.FoodJoints.Framework.Inventories;
using StardewMods.FoodJoints.Framework.Data.Models;

namespace StardewMods.FoodJoints.Framework.Services;
internal class SaveDataManager
{
    internal static SaveDataManager Instance = null!;
    internal SaveDataManager()
        => Instance = this;

    internal void Initialize()
    {
        Mod.Events.GameLoop.Saving += this.OnSaving;
        Mod.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.LoadCafeData();
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        this.SaveCafeData();
    }

    internal void LoadCafeData()
    {
        // Load cafe data
        if (!Game1.IsMasterGame || string.IsNullOrEmpty(Constants.CurrentSavePath) || !File.Exists(Path.Combine(Constants.CurrentSavePath, "FoodJoints", "cafedata")))
            return;

        string cafeDataFile = Path.Combine(Constants.CurrentSavePath, "FoodJoints", "cafedata");

        CafeArchiveData loaded;
        XmlSerializer serializer = new XmlSerializer(typeof(CafeArchiveData));

        try
        {
            using StreamReader reader = new StreamReader(cafeDataFile);
            loaded = (CafeArchiveData)serializer.Deserialize(reader)!;
        }
        catch (InvalidOperationException e)
        {
            Log.Error("Couldn't read Cafe Data from external save folder");
            Log.Error($"{e.Message}\n{e.StackTrace}");
            return;
        }

        Mod.Cafe.OpeningTime = loaded.OpeningTime;
        Mod.Cafe.ClosingTime = loaded.ClosingTime;
        Mod.Cafe.Menu.MenuObject.Set(loaded.MenuItemLists);

        foreach (var loadedEntry in loaded.VillagerCustomersData)
        {
            if (!Mod.Customers.VillagerCustomerModels.TryGetValue(loadedEntry.Key, out VillagerCustomerModel? model))
            {
                Log.Debug("Loading NPC customer data but not model found. Skipping...");
                continue;
            }

            Log.Trace($"Loading customer data from save file {model.NpcName}");
            loadedEntry.Value.NpcName = model.NpcName;
            Mod.Customers.VillagerData[loadedEntry.Key] = loadedEntry.Value;
        }

        //foreach (var loadedEntry in loaded.CustomersData)
        //{
        //    if (!Mod.Instance.CustomerModels.TryGetValue(loadedEntry.Key, out CustomerModel? model))
        //    {
        //        Log.Debug("Loading custom customer data but not model found. Skipping...");
        //        continue;
        //    }

        //    Log.Trace($"Loading custom customer data from save file {model.Name}");
        //    loadedEntry.Value.Id = model.Name;
        //    Mod.Instance.CustomerData[loadedEntry.Key] = loadedEntry.Value;
        //}
    }

    internal void SaveCafeData()
    {
        if (string.IsNullOrEmpty(Constants.CurrentSavePath))
            return;

        string externalSaveFolderPath = Path.Combine(Constants.CurrentSavePath, "FoodJoints");
        string cafeDataPath = Path.Combine(externalSaveFolderPath, "cafedata");

        CafeArchiveData cafeData = new()
        {
            OpeningTime = Mod.Cafe.OpeningTime,
            ClosingTime = Mod.Cafe.ClosingTime,
            MenuItemLists = new SerializableDictionary<FoodCategory, Inventory>(Mod.Cafe.Menu.ItemDictionary),
            VillagerCustomersData = new SerializableDictionary<string, VillagerCustomerData>(Mod.Customers.VillagerData),
            //CustomersData = new SerializableDictionary<string, CustomerData>(Mod.Instance.CustomerData)
        };

        XmlSerializer serializer = new(typeof(CafeArchiveData));
        try
        {
            // Create the FoodJoints folder near the save file, if one doesn't exist
            if (!Directory.Exists(externalSaveFolderPath))
                Directory.CreateDirectory(externalSaveFolderPath);

            using StreamWriter reader = new StreamWriter(cafeDataPath);
            serializer.Serialize(reader, cafeData);
        }
        catch
        {
            Log.Error("Couldn't read Cafe Data from external save folder");
        }
    }
}
