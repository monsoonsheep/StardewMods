using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;

namespace StardewMods.FoodJoints.Framework.Data;
public class CustomerData
{
    public string Id { get; set; } = null!;
    public WorldDate LastVisitedData = new(1, Season.Spring, 1);

    public CustomerData() {}
    public CustomerData(string id)
    {
        this.Id = id;
    }
}
