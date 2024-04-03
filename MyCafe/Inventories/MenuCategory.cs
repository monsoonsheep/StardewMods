using System;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using MyCafe.Enums;
using Netcode;

namespace MyCafe.Inventories;

public class MenuCategory : INetObject<NetFields>
{
    [XmlIgnore]
    public NetFields NetFields { get; } = new NetFields("MenuCategory");

    private NetString netName { get; set; } = new();

    [XmlElement("Type")]
    internal NetEnum<MenuCategoryType> Type { get; set; } = new NetEnum<MenuCategoryType>(MenuCategoryType.DEFAULT);

    [XmlElement("Name")]
    public string Name
    {
        get => this.netName.Value;
        set => this.netName.Set(value);
    }
    public MenuCategory(string name, MenuCategoryType type) : this()
    {
        this.netName.Set(name);
        this.Type.Set(type);
    }

    public MenuCategory()
    {
        this.NetFields.SetOwner(this).AddField(this.netName).AddField(this.Type);
    }

    public override bool Equals(object? obj)
    {
        return obj is MenuCategory other && other.Name == this.Name;
    }

    public override int GetHashCode()
    {
        return this.Name.GetHashCode();
    }
}
