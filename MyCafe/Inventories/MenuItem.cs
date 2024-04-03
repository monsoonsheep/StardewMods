using System;
using StardewValley;

namespace MyCafe.Inventories;
internal class MenuItem
{
    /// <summary>A sample item instance.</summary>
    public Item Item { get; }

    /// <summary>The unqualified item ID.</summary>
    public string Id { get; }

    /// <summary>The qualified item ID.</summary>
    public string QualifiedItemId { get; }

    /// <summary>The menu category of the item</summary>
    public FoodCategory Category { get; }

    /// <summary>The item's default name.</summary>
    public string Name => this.Item.Name;

    /// <summary>The item's display name for the current language.</summary>
    public string DisplayName => this.Item.DisplayName;

    /// <summary>Construct an instance.</summary>
    public MenuItem(Item item, FoodCategory category)
    {
        this.Item = item;
        this.Id = item.ItemId;
        this.QualifiedItemId = "(O)" + this.Id;
        this.Category = category;
    }

    /// <summary>Get whether the item name contains a case-insensitive substring.</summary>
    /// <param name="substring">The substring to find.</param>
    public bool NameContains(string substring)
    {
        return
            this.Name.IndexOf(substring, StringComparison.OrdinalIgnoreCase) != -1
            || this.DisplayName.IndexOf(substring, StringComparison.OrdinalIgnoreCase) != -1;
    }

    /// <summary>Get whether the item name is exactly equal to a case-insensitive string.</summary>
    /// <param name="name">The substring to find.</param>
    public bool NameEquivalentTo(string name)
    {
        return
            this.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
            || this.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase);
    }
}
