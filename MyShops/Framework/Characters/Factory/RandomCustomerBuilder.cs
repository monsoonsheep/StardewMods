using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewMods.MyShops.Framework.Enums;
using StardewValley;

namespace StardewMods.MyShops.Framework.Characters.Factory;

internal class RandomCustomerBuilder : CustomerBuilder
{
    internal override CustomerGroup GenerateGroup()
    {
        throw new NotImplementedException();

        List<NPC> customers = [];
        int count = ModUtility.RandomNumberOfSeatsForTable(base._table!.Seats.Count);




        // get new NPCs, path them to their tables




        

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
        catch (Exception e)
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
            Mod.Customers.DeleteNpcFromExistence(npc);
        }
    }
}
