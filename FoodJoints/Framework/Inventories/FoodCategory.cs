using System.Xml.Serialization;
using Netcode;
using StardewMods.FoodJoints.Framework.Enums;

namespace StardewMods.FoodJoints.Framework.Inventories;

[XmlRoot("Category")]
public class FoodCategory : INetObject<NetFields>
{
    [XmlIgnore]
    public NetFields NetFields { get; } = new NetFields("FoodCategory");

    private NetString netName { get; set; } = new();

    [XmlElement("Type")]
    internal NetEnum<MenuCategoryType> Type { get; set; } = new NetEnum<MenuCategoryType>(MenuCategoryType.DEFAULT);

    [XmlElement("Name")]
    public string Name
    {
        get => this.netName.Value;
        set => this.netName.Set(value);
    }
    public FoodCategory(string name, MenuCategoryType type) : this()
    {
        this.netName.Set(name);
        this.Type.Set(type);
    }

    public FoodCategory()
    {
        this.NetFields.SetOwner(this).AddField(this.netName).AddField(this.Type);
    }

    public override bool Equals(object? obj)
    {
        return obj is FoodCategory other && other.Name == this.Name;
    }

    public override int GetHashCode()
    {
        return this.Name.GetHashCode();
    }
}
