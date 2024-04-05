using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyCafe.Locations.Objects;
using StardewValley;

namespace MyCafe.Data;
public class CustomerSpawnedEventArgs : EventArgs
{
    public NPC Npc {get; init;}
    public bool IsVillager {get; init;}
    public Table Table {get; init;}


    public CustomerSpawnedEventArgs(NPC npc, Table table, bool isVillager)
    {
        this.Npc = npc;
        this.IsVillager = isVillager;
        this.Table = table;
    }
}
