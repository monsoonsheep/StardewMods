using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using MonsoonSheep.Stardew.Common;
using MyCafe.Enums;
using MyCafe.Netcode;
using Netcode;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Network;

namespace MyCafe.Inventories;

public class MenuInventory : INetObject<NetFields>
{
    public NetFields NetFields { get; } = new NetFields("MenuInventory");

    public readonly NetMenuDictionary Menu = new NetMenuDictionary();

    public IEnumerable<Inventory> Inventories
        => this.Menu.Values.AsEnumerable();

    public IDictionary<MenuCategory, Inventory> ItemDictionary
        => this.Menu.Pairs.ToDictionary(pair => pair.Key, pair => pair.Value);

    public MenuInventory()
    {
        this.NetFields.SetOwner(this).AddField(this.Menu);
    }

    public Inventory GetItemsInCategory(MenuCategory category)
    {
        return this.Menu[category];
    }

    public bool AddItem(Item item, MenuCategory category, int index = 0)
    {
        if (this.Inventories.Any(i => i.ContainsId(item.QualifiedItemId)))
            return false;

        if (!this.Menu.ContainsKey(category))
            this.AddCategory(category);

        this.Menu[category].Add(item);
        return true;
    }

    public bool AddItem(Item itemToAdd, string category, int index = 0)
    {
        MenuCategory? cat = this.Menu.Keys.FirstOrDefault(x => x.Name == category);
        if (cat != null)
            return this.AddItem(itemToAdd, cat, index);

        return false;
    }

    public void RemoveItem(Item item)
    {
        this.Inventories.FirstOrDefault(i => i.Contains(item))?.Remove(item);
    }

    public void AddCategory(string name, MenuCategoryType type = default)
    {
        this.AddCategory(new MenuCategory(name, type));
    }

    public void AddCategory(MenuCategory category)
    {
        if (this.Menu.Keys.Any(x => x.Name == category.Name))
            return;

        this.Menu.Add(category, new Inventory());
    }

    public void RemoveCategory(MenuCategory category)
    {
        this.Menu.Remove(category);
    }

    public void SetItems(Dictionary<MenuCategory, Inventory> items)
    {
        this.Menu.Set(items);
    }
}
