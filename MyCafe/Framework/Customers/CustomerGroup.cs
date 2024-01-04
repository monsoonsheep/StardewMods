using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.Framework.Objects;
using StardewValley;

namespace MyCafe.Framework.Customers
{
    internal class CustomerGroup
    {
        internal List<Customer> Members;
        internal Table ReservedTable;

        internal CustomerGroup(List<Customer> members)
        {
            Members = members;
        }

        internal bool ReserveTable(Table table) {
            if (table.Reserve(Members)) {
                ReservedTable = table;
                return true;
            }

            return false;
        }

        internal static CustomerGroup SpawnGroup(Table table, List<CustomerData> customersData) {
            List<Customer> list = customersData.Select(data => {
                Texture2D portrait = Game1.content.Load<Texture2D>(data.Model.PortraitName);
                AnimatedSprite sprite = new AnimatedSprite(data.Model.Spritesheet, 0, 16, 32);
                Customer c = new Customer($"CustomerNPC_{data.Model.Name}", new Vector2(10, 12) * 64f, "BusStop", sprite, portrait);
                return c;
            }).ToList();
            CustomerGroup group = new CustomerGroup(list);

            group.ReserveTable(table);

            GameLocation tableLocation = Utility.GetLocationFromName(table.CurrentLocation);
            GameLocation busStop = Game1.getLocationFromName("BusStop");
            
            bool failed = false;

            for (int i = 0; i < group.Members.Count; i++) {
                Customer c = group.Members[i];
                c.ReservedSeat.Reserve(c);

                c.PathTo(tableLocation, c.ReservedSeat.Position.ToPoint(), 3, null);
                if (c.controller == null 
                || c.controller.pathToEndPoint?.Count == 0 
                || c.controller.pathToEndPoint.Last().Equals(c.ReservedSeat.Position.ToPoint()))
                {
                    failed = true;
                    break;
                }

                busStop.addCharacter(c);
                
                int direction = Utility.DirectionIntFromVectors(c.controller.pathToEndPoint.Last().ToVector2(), c.ReservedSeat.Position);
                c.controller.endBehaviorFunction = (_, _) =>
                {
                    c.SitDown(direction);
                    c.faceDirection(c.ReservedSeat.SittingDirection);
                };
            }

            if (failed) {
                foreach (Customer c in group.Members) {
                    busStop.characters.Remove(c);
                }
                table.Free();
                // Revert changes
            }

            return group;
        }
    }
}
