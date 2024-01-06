using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.Framework.ChairsAndTables;
using MyCafe.Framework.Customers;
using StardewValley;

namespace MyCafe.Framework.Managers;

internal class BusCustomerSpawner : ICustomerSpawner
{
    internal readonly Dictionary<string, BusCustomerData> CustomersData = new();

    internal List<BusCustomerData> GetRandomCustomerDataMultiple(int members)
    {
        return CustomersData.Values.OrderBy(_ => Game1.random.Next()).Take(members).ToList();
    }

    public bool Spawn(Table table, out CustomerGroup group)
    {
        List<BusCustomerData> datas = GetRandomCustomerDataMultiple(table.Seats.Count);
        if (datas == null || !datas.Any())
        {
            group = null;
            return false;
        }

        List<Customer> list = datas.Select(data =>
        {
            Texture2D portrait = Game1.content.Load<Texture2D>(data.Model.PortraitName);
            AnimatedSprite sprite = new AnimatedSprite(data.Model.Spritesheet, 0, 16, 32);
            Customer c = new Customer($"CustomerNPC_{data.Model.Name}", new Vector2(10, 12) * 64f, "BusStop", sprite, portrait)
            {
                portraitOverridden = true
            };
            return c;
        }).ToList();

        group = new CustomerGroup(list);
        GameLocation busStop = Game1.getLocationFromName("BusStop");
        foreach (Customer c in group.Members)
        {
            c.ReservedSeat.Reserve(c);
            busStop.addCharacter(c);
        }
        group.ReserveTable(table);

        if (group.MoveToTable() is false)
        {
            foreach (Customer c in group.Members)
                c.currentLocation.characters.Remove(c);
            group.ReservedTable.Free();
            return false;
        }

        return true;
    }

    public void LetGo(CustomerGroup group)
    {

    }

    public void DayUpdate()
    {

    }


}