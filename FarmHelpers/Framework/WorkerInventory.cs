using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewMods.FarmHelpers.Framework;
internal class WorkerInventory
{
    private List<Item> Inventory = [];

    internal void Add(Item item)
    {
        this.Inventory.Add(item);
    }
}
