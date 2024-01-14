using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.ChairsAndTables;
using MyCafe.Customers;
using MyCafe.Customers.Data;
using MyCafe.Interfaces;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Pathfinding;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyCafe.CustomerFactory;

internal class BusCustomerSpawner : CustomerSpawner
{
    internal Dictionary<string, BusCustomerData> CustomersData;
    private IBusSchedulesApi _busSchedulesApi;

    internal override void Initialize(IModHelper helper)
    {
        Mod.Cafe.Assets.LoadContentPackBusCustomers(helper, out CustomersData);
        _busSchedulesApi = Mod.ModHelper.ModRegistry.GetApi<IBusSchedulesApi>("MonsoonSheep.BusSchedules");
    }

    internal List<BusCustomerData> GetRandomCustomerData(int members)
    {
        return CustomersData.Values.OrderBy(_ => Game1.random.Next()).Take(members).ToList();
    }

    internal Customer GetCustomerFromData(BusCustomerData data)
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

        if (false && _busSchedulesApi != null && _busSchedulesApi.GetMinutesTillNextBus() is <= 30 and > 10)
        {
            foreach (Customer c in customers)
            {
                busStop.addCharacter(c);
                c.Position = new Vector2(-1000, -1000);
                AccessTools.Field(typeof(Character), "returningToEndPoint")?.SetValue(c, true);
                Stack<Point> points = new Stack<Point>();
                points.Push(new Point(12, 9));
                GameLocation targetLocation = Utility.GetLocationFromName(table.CurrentLocation);
                Point targetPoint = c.ReservedSeat.Position;
                CustomerGroup g = group;

                c.temporaryController = new PathFindController(points, c, busStop)
                {
                    NPCSchedule = false,
                    endBehaviorFunction = delegate (Character x, GameLocation loc)
                    {
                        if (x is NPC n)
                        {
                            if (!n.PathTo(targetLocation, targetPoint, 3, Customer.SitDownBehavior))
                            {
                                Log.Error($"Customer {n.Name} couldn't path to cafe");
                                LetGo(g);
                            }
                        }
                    }
                };

                _busSchedulesApi.AddVisitorsForNextArrival(c, 0);
            }
        }
        else
        {
            foreach (Customer c in customers)
            {
                busStop.addCharacter(c);
                c.Position = new Vector2(33, 9) * 64;
            }
            if (group.MoveToTable() is false)
            {
                Log.Error("Customers couldn't path to cafe");
                LetGo(group);
                return false;
            }
        }

        ActiveGroups.Add(group);
        return true;
    }

    internal override void LetGo(CustomerGroup group)
    {
        base.LetGo(group);
        foreach (Customer c in group.Members)
            c.currentLocation.characters.Remove(c);
    }

    internal override void DayUpdate()
    {

    }
}