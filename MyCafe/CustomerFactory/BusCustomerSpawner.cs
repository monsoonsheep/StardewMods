using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.Locations;
using MyCafe.Customers;
using MyCafe.Customers.Data;
using MyCafe.Interfaces;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Pathfinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyCafe.CustomerFactory;

internal class BusCustomerSpawner : CustomerSpawner
{
    private IBusSchedulesApi _busSchedulesApi;

    internal override Task<bool> Initialize(IModHelper helper)
    {
        _busSchedulesApi = Mod.ModHelper.ModRegistry.GetApi<IBusSchedulesApi>("MonsoonSheep.BusSchedules");
        return Task.FromResult(true);
    }

    internal List<BusCustomerData> GetRandomCustomerData(int members)
    {
        return Mod.CustomersData.Values.OrderBy(_ => Game1.random.Next()).Take(members).ToList();
    }

    internal static Customer GetCustomerFromData(BusCustomerData data)
    {
        Texture2D portrait = Game1.content.Load<Texture2D>(data.Model.PortraitName);
        AnimatedSprite sprite = new AnimatedSprite(data.Model.Spritesheet, 0, 16, 32);
        Customer c = new Customer($"CustomerNPC_{data.Model.Name}", new Vector2(10, 12) * 64f, "BusStop", sprite, portrait);
        return c;
    }

    internal override bool Spawn(Table table, out CustomerGroup group)
    {
        group = new CustomerGroup();
        List<BusCustomerData> datas = GetRandomCustomerData(1);
        if (datas == null || !datas.Any())
            return false;

        List<Customer> customers = [];

        foreach (var data in datas)
        {
            try
            {
                Customer c = GetCustomerFromData(data);
                customers.Add(c);
            }
            catch (Exception e)
            {
                Log.Error($"Error creating character {data.Model.Name}: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        foreach (var c in customers)
            group.Add(c);
        if (!group.ReserveTable(table))
        {
            Log.Error("Table couldn't be reserved. Bug!");
            return false;
        }

        foreach (Customer c in customers)
        {
            c.ItemToOrder.Set(ItemRegistry.Create<StardewValley.Object>("(O)128"));
        }

        GameLocation busStop = Game1.getLocationFromName("BusStop");

        bool busAvailable = _busSchedulesApi?.GetMinutesTillNextBus() is <= 30 and > 10;

        foreach (Customer c in customers)
        {
            busStop.addCharacter(c);
            c.Position = (busAvailable ? new Vector2(12, 9) : new Vector2(33, 9)) * 64;
        }

        if (group.MoveToTable() is false)
        {
            Log.Error("Customers couldn't path to cafe");
            LetGo(group, force: true);
            return false;
        }

        if (busAvailable)
        {
            if (!AddToBus(group))
            {
                LetGo(group, force: true);
                return false;
            }
        }
        
        ActiveGroups.Add(group);
        return true;
    }

    internal override bool LetGo(CustomerGroup group, bool force = false)
    {
        if (!base.LetGo(group))
            return false;

        if (force)
        {
            group.Delete();
        }
        else
        {
            group.MoveTo(
            Game1.getLocationFromName("BusStop"), 
            new Point(33, 9), 
            (c, loc) => loc.characters.Remove(c as NPC));
        }
        return true;
    }

    internal override void DayUpdate()
    {

    }

    internal bool AddToBus(CustomerGroup group)
    {
        Log.Debug("Adding to bus");
        foreach (Customer c in group.Members)
        {
            c.Position = new Vector2(-1000, -1000);
            AccessTools.Field(typeof(NPC), "returningToEndPoint")?.SetValue(c, true);
            AccessTools.Field(typeof(Character), "freezeMotion")?.SetValue(c, true);
            if (!_busSchedulesApi.AddVisitorsForNextArrival(c, 0))
            {
                return false;
            }
        }

        return true;
    }
}