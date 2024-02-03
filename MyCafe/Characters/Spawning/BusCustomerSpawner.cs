using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe;
using MyCafe.Characters;
using MyCafe.Data.Customers;
using MyCafe.Enums;
using MyCafe.Interfaces;
using MyCafe.Locations.Objects;
using StardewModdingAPI;
using StardewValley;

namespace MyCafe.Characters.Spawning;

internal class BusCustomerSpawner : CustomerSpawner
{
    internal Dictionary<string, BusCustomerData> CustomersData;

    private IBusSchedulesApi? BusSchedulesApi;

    internal BusCustomerSpawner(Dictionary<string, BusCustomerData> customersData) : base()
    {
        this.CustomersData = customersData;
    }

    internal override void Initialize(IModHelper helper)
    {
        base.Initialize(helper);
        this.BusSchedulesApi = helper.ModRegistry.GetApi<IBusSchedulesApi>("MonsoonSheep.BusSchedules");
    }

    internal List<BusCustomerData> GetRandomCustomerData(int members)
    {
        return this.CustomersData.Values.OrderBy(_ => Game1.random.Next()).Take(members).ToList();
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
        List<BusCustomerData> datas = this.GetRandomCustomerData(1);
        if (!datas.Any())
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

        bool busAvailable = this.BusSchedulesApi?.GetMinutesTillNextBus() is <= 30 and > 10;

        foreach (Customer c in customers)
        {
            busStop.addCharacter(c);
            c.Position = (busAvailable ? new Vector2(12, 9) : new Vector2(33, 9)) * 64;
        }

        if (group.MoveToTable() is false)
        {
            Log.Error("Customers couldn't path to cafe");
            this.LetGo(group, force: true);
            return false;
        }

        if (busAvailable)
        {
            if (!this.AddToBus(group))
            {
                this.LetGo(group, force: true);
                return false;
            }
        }

        Log.Debug("Customers are coming");
        table.State.Set(TableState.WaitingForCustomers);
        this.ActiveGroups.Add(group);
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
            AccessTools.Field(typeof(NPC), "returningToEndPoint").SetValue(c, true);
            AccessTools.Field(typeof(Character), "freezeMotion").SetValue(c, true);
            if (this.BusSchedulesApi != null && !this.BusSchedulesApi.AddVisitorsForNextArrival(c, 0))
            {
                Log.Debug("But couldn'");
                return false;
            }
        }

        return true;
    }
}
