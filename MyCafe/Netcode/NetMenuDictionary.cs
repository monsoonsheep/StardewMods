using System;
using System.Collections.Generic;
using System.IO;
using MyCafe.Enums;
using MyCafe.Inventories;
using Netcode;
using StardewValley;
using StardewValley.Inventories;

#nullable disable
namespace MyCafe.Netcode;
public class NetMenuDictionary : NetFieldDictionary<MenuCategory, Inventory, NetRef<Inventory>, SerializableDictionary<MenuCategory, Inventory>, NetMenuDictionary>
{
    public NetMenuDictionary()
    {
    }

    public NetMenuDictionary(IEnumerable<KeyValuePair<MenuCategory, Inventory>> dict)
        : base(dict)
    {
    }

    protected override MenuCategory ReadKey(BinaryReader reader)
    {
        string name = reader.ReadString();
        if (!Enum.TryParse(reader.ReadString(), ignoreCase: true, out MenuCategoryType type))
            type = MenuCategoryType.DEFAULT;

        return new MenuCategory(name, type);
    }

    protected override void WriteKey(BinaryWriter writer, MenuCategory key)
    {
        writer.Write(key.Name);
        writer.Write(key.Type.ToString());
    }
}
