using System;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MonsoonSheep.Stardew.Common;
using MyCafe.Enums;
using MyCafe.Locations.Objects;
using MyCafe.Netcode;
using StardewValley;
using StardewValley.Network;
using StardewValley.Pathfinding;

namespace MyCafe.Characters;

public static class NpcExtensions
{
    public static void Freeze(this NPC me)
    {
        AccessTools.Field(typeof(Character), "freezeMotion").SetValue(me, true);
    }

    public static void Unfreeze(this NPC me)
    {
        AccessTools.Field(typeof(Character), "freezeMotion").SetValue(me, false);
    }

    public static void Jump(this NPC me, int direction)
    {
        Vector2 sitPosition = me.Position + CommonHelper.DirectionIntToDirectionVector(direction) * 64f;
        me.set_LerpStartPosition(me.Position);
        me.set_LerpEndPosition(sitPosition);
        me.set_LerpPosition(0f);
        me.set_LerpDuration(0.2f);
    }

    public static void JumpTo(this NPC me, Vector2 pixelPosition)
    {
        me.set_LerpStartPosition(me.Position);
        me.set_LerpEndPosition(pixelPosition);
        me.set_LerpPosition(0f);
        me.set_LerpDuration(0.2f);
    }

    public static PathFindController.endBehavior SitDownBehavior = delegate (Character ch, GameLocation _)
    {
        NPC c = (ch as NPC)!;

        Seat? seat = c.get_Seat();
        CustomerGroup? group = c.get_Group();

        if (seat != null && group is { ReservedTable: not null })
        {
            int direction = CommonHelper.DirectionIntFromVectors(c.Tile, seat.Position.ToVector2());
            c.faceDirection(seat.SittingDirection);

            c.JumpTo(seat.SittingPosition);

            c.get_IsSittingDown().Set(true);

            // Make them do the sitting frame if they are a customer model
            if (c.Name.StartsWith(ModKeys.CUSTOMER_NPC_NAME_PREFIX))
            {
                int frame = seat.SittingDirection switch
                {
                    0 => 19,
                    1 => 17,
                    2 => 16,
                    3 => 18,
                    _ => 15
                };
                c.Sprite.setCurrentAnimation([new FarmerSprite.AnimationFrame(frame, int.MaxValue)]);
            }
            
            if (!group.Members.Any(other => !other.get_IsSittingDown()))
                group.ReservedTable.State.Set(TableState.CustomersThinkingOfOrder);
        }
    };
}
