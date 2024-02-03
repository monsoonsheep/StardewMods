using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using MyCafe.Enums;
using Netcode;
using StardewValley;

namespace MyCafe.Inventories;

public class MenuCategory : INetObject<NetFields>
{
    public NetFields NetFields { get; } = new NetFields("MenuCategory");

    [XmlElement("Name")]
    public NetString NetName { get; set; } = new();

    [XmlElement("Type")]
    public NetEnum<MenuCategoryType> Type { get; set; } = new NetEnum<MenuCategoryType>(MenuCategoryType.DEFAULT);

    public string Name
        => this.NetName.Value;

    public MenuCategory(string name, MenuCategoryType type) : this()
    {
        this.NetName.Set(name);
        this.Type.Set(type);
    }

    public MenuCategory()
    {
        this.NetFields.SetOwner(this).AddField(this.NetName).AddField(this.Type);
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
