using System;
using System.Collections.Generic;
using System.Linq;
using MyCafe.Data.Models;
using MyCafe.Enums;
using MyCafe.Netcode;
using Netcode;
using StardewValley;
using StardewValley.Inventories;

namespace MyCafe.Inventories;

public class FoodMenuInventory : INetObject<NetFields>
{
    public NetFields NetFields { get; } = new NetFields("FoodMenuInventory");

    public readonly NetFoodMenuDictionary MenuObject = new NetFoodMenuDictionary();

    internal IDictionary<FoodCategory, Inventory> ItemDictionary
        => this.MenuObject.Pairs.ToDictionary(pair => pair.Key, pair => pair.Value);

    public FoodMenuInventory()
    {
        this.NetFields.SetOwner(this).AddField(this.MenuObject);
    }

    internal void InitializeForHost()
    {
        foreach (MenuCategoryArchive category in Mod.Config.MenuCategories)
        {
            if (!this.ItemDictionary.Any(c => c.Key.Name.Equals(category.Name)))
            {
                if (!Enum.TryParse(category.Type, out MenuCategoryType type))
                    type = MenuCategoryType.DEFAULT;

                this.AddCategory(new FoodCategory(category.Name, type));
            }
        }

        if (!this.ItemDictionary.Any())
            this.AddCategory("Items");

        if (this.HasCategory("Items") && this.GetItemsInCategory("Items")?.Any() == false)
            this.RemoveCategory("Items");
    }
    public bool AddItem(Item item, FoodCategory category, int index = 0)
    {
        if (this.MenuObject.Values.Any(i => i.ContainsId(item.QualifiedItemId)))
            return false;

        if (!this.MenuObject.ContainsKey(category))
            this.AddCategory(category);

        this.MenuObject[category].Insert(index, item);
        return true;
    }

    public bool AddItem(Item itemToAdd, string category, int index = 0)
    {
        FoodCategory? cat = this.MenuObject.Keys.FirstOrDefault(x => x.Name == category);
        if (cat != null)
            return this.AddItem(itemToAdd, cat, index);

        return false;
    }

    public void RemoveItem(Item item)
    {
        this.MenuObject.Values.FirstOrDefault(i => i.Contains(item))?.Remove(item);
    }

    public void AddCategory(string name, MenuCategoryType type = default)
    {
        this.AddCategory(new FoodCategory(name, type));
    }

    public void AddCategory(FoodCategory category)
    {
        if (this.MenuObject.Keys.Any(x => x.Name == category.Name))
            return;

        this.MenuObject.Add(category, new Inventory());
    }

    public void RemoveCategory(FoodCategory category)
    {
        this.MenuObject.Remove(category);
    }

    public void RemoveCategory(string name)
    {
        this.MenuObject.RemoveWhere(pair => pair.Key.Name == name);
    }

    internal bool HasCategory(string name)
    {
        return this.ItemDictionary.Any(c => c.Key.Name == name);
    }

    internal Inventory? GetItemsInCategory(string name)
    {
        return this.MenuObject[name];
    }

    internal Inventory? GetItemsInCategory(FoodCategory category)
    {
        return this.MenuObject.ContainsKey(category) ? this.MenuObject[category] : null;
    }
}
