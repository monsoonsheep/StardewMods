using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.Data.Customers;
using MyCafe.Data.Models;
using MyCafe.Enums;
using StardewValley;

namespace MyCafe.Characters.Factory;

internal class RandomCustomerBuilder : CustomerBuilder
{
    private List<CustomerModel>? GetCustomModels(int count)
    {
        List<CustomerModel> list = [];
        List<CustomerData> data = Mod.Instance.CustomerData.Values.OrderBy(i => i.LastVisitedData.TotalDays).Take(count).ToList();
        if (data.Count < count)
            return null;

        return data.Select(i => Mod.Instance.CustomerModels[i.Id]).ToList();
    }

    private List<CustomerModel> GetGeneratedModels(int count)
    {
        List<CustomerModel> list = [];
        for (int i = 0; i < count; i++)
            list.Add(Mod.RandomCharacterGenerator.GenerateRandomCustomer());

        return list;
    }

    internal override CustomerGroup? BuildGroup()
    {
        List<NPC> customers = [];

        int count = base._table!.Seats.Count;
        int random = Game1.random.Next(count);

        count = (count) switch
        {
            1 => 1,
            2 or 3 => random == 0 ? count : count - 1,
            >=4 => random == 0 ? count : Game1.random.Next(2, count),
            _ => random == 0 ? count : Game1.random.Next(3, count)
        };

        int weightForCustom = Mod.Config.EnableCustomCustomers;
        int weightForGenerated = Mod.Config.EnableRandomlyGeneratedCustomers;
        int r = Game1.random.Next(weightForCustom + weightForGenerated);
        r -= weightForGenerated;

        List<CustomerModel> models;
        if (r < 0)
            models = this.GetGeneratedModels(count);
        else
            models = this.GetCustomModels(count) ?? this.GetGeneratedModels(count);

        for (int i = 0; i < count; i++)
        {
            CustomerModel model = models[i];
            Texture2D portrait = Game1.content.Load<Texture2D>(model.Portrait);
            AnimatedSprite sprite = new AnimatedSprite(model.Spritesheet, 0, 16, 32);
            NPC c = new NPC(sprite, new Vector2(10, 12) * 64f, "BusStop", 2, ModKeys.CUSTOMER_NPC_NAME_PREFIX + model.Name, false, portrait);
            customers.Add(c);
        }

        CustomerGroup group = new CustomerGroup(GroupType.Random);
        foreach (NPC member in customers)
            group.AddMember(member);
        
        return group;
    }

    internal override bool PreMove()
    {
        GameLocation busStop = Game1.getLocationFromName("BusStop");
        foreach (NPC c in this._group!.Members)
        {
            busStop.addCharacter(c);
            c.currentLocation = busStop;
            c.Position = new Vector2(33, 9) * 64;
        }

        return true;
    }

    internal override bool MoveToTable()
    {
        try
        {
            this._group!.GoToTable();
        }
        catch (PathNotFoundException e)
        {
            Log.Error($"Couldn't spawn random customers: {e.Message}\n{e.StackTrace}");
            return false;
        }

        return true;
    }

    internal override bool PostMove()
    {
        return true;
    }

    internal override void RevertChanges()
    {
        Log.Trace($"Reverting changes");

        this._group!.ReservedTable?.Free();

        foreach (NPC npc in this._group.Members)
        {
            npc.Portrait?.Dispose();
            npc.Sprite?.Texture?.Dispose();
            Mod.Cafe.DeleteNpcFromExistence(npc);
        }
    }
}
