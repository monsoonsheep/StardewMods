using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.Data.Models;
using MyCafe.Enums;
using MyCafe.Locations.Objects;
using MyCafe.Netcode;
using StardewModdingAPI;
using StardewValley;

namespace MyCafe.Characters.Spawning;
internal class RandomCustomerSpawner : CustomerSpawner
{
    private readonly Func<CustomerModel?> _getModelFunc;

    internal RandomCustomerSpawner(Func<CustomerModel?> getModelFunc)
    {
        this._getModelFunc = getModelFunc;
    }

    internal override void Initialize(IModHelper helper)
    {
    }

    internal override void DayUpdate()
    {
    }

    internal override bool Spawn(Table table)
    {
        List<NPC>? npcs = this.CreateRandomCustomerGroup(Game1.random.Next(1, table.Seats.Count + 1));
        if (npcs == null)
        {
            Log.Debug("No random customers can be created.");
            return false;
        }

        CustomerGroup group = new(GroupType.Random, this);

        foreach (NPC npc in npcs)
            group.AddMember(npc);

        if (group.ReserveTable(table) == false)
        {
            Log.Debug("Couldn't reserve table for random customers");
            this.EndCustomers(group, force: true);
            return false;
        }

        foreach (NPC c in group.Members)
            c.get_OrderItem().Set(Debug.SetTestItemForOrder(c));

        GameLocation busStop = Game1.getLocationFromName("BusStop");
        foreach (NPC c in group.Members)
        {
            busStop.addCharacter(c);
            c.Position = new Vector2(33, 9) * 64;
        }

        try
        {
            group.GoToTable();
        }
        catch (PathNotFoundException e)
        {
            // TODO: Return NPC to schedule
            Log.Error($"Couldn't spawn random customers: {e.Message}\n{e.StackTrace}");
            this.EndCustomers(group, force: true);
            return false;
        }

        this._groups.Add(group);
        return true;
    }

    internal override bool EndCustomers(CustomerGroup group, bool force = false)
    {
        Log.Debug($"Removing customers{(force ? " By force" : "")}");
        group.ReservedTable?.Free();
        this._groups.Remove(group);

        // Random
        if (force)
        {
            foreach (NPC c in group.Members)
                this.DeleteNpc(c);
        }
        else
        {
            try
            {
                group.MoveTo(
                    Game1.getLocationFromName("BusStop"),
                    new Point(33, 9),
                    (c, loc) => this.DeleteNpc((c as NPC)!));
            }
            catch (PathNotFoundException e)
            {
                Log.Error($"Couldn't return customers to bus stop\n{e.Message}\n{e.StackTrace}");
                this.EndCustomers(group, force: true);
            }
        }

        return true;
    }

    private List<NPC>? CreateRandomCustomerGroup(int count)
    {
        List<CustomerModel> models = [];

        for (int i = 0; i < count; i++)
        {
            CustomerModel? model = this._getModelFunc(); // Generate from CharGen
            if (model == null)
                return null;
            models.Add(model);
        }

        if (!models.Any())
            return null;

        List<NPC> customers = [];
        foreach (CustomerModel model in models)
        {
            Texture2D portrait = Game1.content.Load<Texture2D>(model.Portrait);
            AnimatedSprite sprite = new AnimatedSprite(model.Spritesheet, 0, 16, 32);
            NPC c = new NPC(sprite, new Vector2(10, 12) * 64f, "BusStop", 2, model.Name, false, portrait);
            customers.Add(c);
        }

        return customers;
    }

    private void DeleteNpc(NPC npc)
    {
        npc.currentLocation?.characters.Remove(npc);
        this.TryRemoveRandomNpcData(npc.Name);
    }

    private void TryRemoveRandomNpcData(string name)
    {
        Match findRandomGuid = new Regex($@"{ModKeys.CUSTOMER_NPC_NAME_PREFIX}Random(.*)").Match(name);
        if (findRandomGuid.Success)
        {
            Log.Trace("Deleting random customer and its generated sprite");
            string guid = findRandomGuid.Groups[1].Value;
            if (Mod.Cafe.GeneratedSprites.Remove(guid) == false)
                Log.Trace("Tried to remove GUID for random customer but it wasn't registered.");
        }
    }
}
