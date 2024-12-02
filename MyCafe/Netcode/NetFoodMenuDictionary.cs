using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Monsoonsheep.StardewMods.MyCafe.Enums;
using Monsoonsheep.StardewMods.MyCafe.Inventories;
using Netcode;
using StardewValley;
using StardewValley.Inventories;

#nullable disable
namespace Monsoonsheep.StardewMods.MyCafe.Netcode;
public class NetFoodMenuDictionary : NetFieldDictionary<FoodCategory, Inventory, NetRef<Inventory>, SerializableDictionary<FoodCategory, Inventory>, NetFoodMenuDictionary>
{
    public NetFoodMenuDictionary()
    {
    }

    public NetFoodMenuDictionary(IEnumerable<KeyValuePair<FoodCategory, Inventory>> dict)
        : base(dict)
    {
    }

    public Inventory this[string name] => this.Pairs.ToDictionary(i => i.Key, i => i.Value).FirstOrDefault(c => c.Key.Name.Equals(name)).Value;

    protected override FoodCategory ReadKey(BinaryReader reader)
    {
        string name = reader.ReadString();
        if (!Enum.TryParse(reader.ReadString(), ignoreCase: true, out MenuCategoryType type))
            type = MenuCategoryType.DEFAULT;

        return new FoodCategory(name, type);
    }

    protected override void WriteKey(BinaryWriter writer, FoodCategory key)
    {
        writer.Write(key.Name);
        writer.Write(key.Type.ToString());
    }
}
