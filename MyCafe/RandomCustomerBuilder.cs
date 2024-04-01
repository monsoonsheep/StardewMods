using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.Characters;
using MyCafe.Data.Models;
using MyCafe.Enums;
using MyCafe.Locations.Objects;
using MyCafe.Netcode;
using StardewValley;

#nullable disable

namespace MyCafe;

internal class RandomCustomerBuilder : CustomerBuilder
{
    internal override CustomerGroup BuildGroup()
    {
        List<NPC> customers = [];

        for (int i = 0; i < base.table.Seats.Count; i++)
        {
            CustomerModel model = Mod.CharacterFactory.GenerateRandomCustomer(); // Generate from CharGen

            Texture2D portrait = Game1.content.Load<Texture2D>(model.Portrait);
            AnimatedSprite sprite = new AnimatedSprite(model.Spritesheet, 0, 16, 32);
            NPC c = new NPC(sprite, new Vector2(10, 12) * 64f, "BusStop", 2, model.Name, false, portrait);
            customers.Add(c);
        }

        CustomerGroup group = new CustomerGroup(GroupType.Random);
        foreach (NPC member in customers)
            group.AddMember(member);
        
        return group;
    }

    internal override bool SetupGroup()
    {
        foreach (NPC c in this._group.Members)
            c.get_OrderItem().Set(Debug.SetTestItemForOrder(c));

        return true;
    }

    internal override bool PreMove()
    {
        GameLocation busStop = Game1.getLocationFromName("BusStop");
        foreach (NPC c in this._group.Members)
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
            this._group.GoToTable();
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

        this._group.ReservedTable?.Free();

        foreach (NPC npc in this._group.Members)
        {
            npc.Portrait?.Dispose();
            npc.Sprite?.Texture?.Dispose();
            Mod.Cafe.DeleteNpcFromExistence(npc);
        }
    }
}
