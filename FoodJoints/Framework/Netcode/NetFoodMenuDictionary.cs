using Netcode;
using StardewMods.FoodJoints.Framework.Enums;
using StardewMods.FoodJoints.Framework.Inventories;
using StardewValley.Inventories;

#nullable disable
namespace StardewMods.FoodJoints.Framework.Netcode;
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
