using System;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MonsoonSheep.Stardew.Common;
using Monsoonsheep.StardewMods.MyCafe.Enums;
using Monsoonsheep.StardewMods.MyCafe.Game;
using Monsoonsheep.StardewMods.MyCafe.Locations.Objects;
using StardewValley;
using StardewValley.Network;
using StardewValley.Pathfinding;

namespace Monsoonsheep.StardewMods.MyCafe.Characters;

public static class NpcExtensions
{
    public static void SitDownBehavior(Character ch, GameLocation loc)
    {
        NPC c = (ch as NPC)!;

        Seat? seat = c.get_Seat();
        CustomerGroup? group = c.get_Group();

        if (seat != null && group is { ReservedTable: not null })
        {
            c.faceDirection(seat.SittingDirection);
            c.JumpTo(seat.SittingPosition);
            c.get_IsSittingDown().Set(true);

            if (c.Name.StartsWith(ModKeys.CUSTOMER_NPC_NAME_PREFIX))
            {
                // Is a custom customer model or randomly generated sprite.
                // Make them do the sitting frame
                int frame = seat.SittingDirection switch
                {
                    0 => 19,
                    1 => 17,
                    2 => 16,
                    3 => 18,
                    _ => -1
                };

                if (frame != -1)
                    c.Sprite.setCurrentAnimation([new FarmerSprite.AnimationFrame(frame, int.MaxValue)]);
            }
            else
            {
                // Is a villager NPC
                Mod.Instance.AddDialoguesOnArrivingAtCafe(c);
            }
            
            if (!group.Members.Any(other => !other.get_IsSittingDown().Value))
                group.ReservedTable.State.Set(TableState.CustomersThinkingOfOrder);
        }
    }

    public static void WarpThroughLocationsUntilNoFarmers(this NPC me)
    {
        GameLocation location = me.currentLocation;

        while (!me.currentLocation.Equals(Mod.Cafe.Signboard?.Location) &&
               !me.currentLocation.Name.Equals("Farm") &&
               me.controller.pathToEndPoint is { Count: > 2 })
        {
            if (!location.Equals(me.currentLocation))
                return;
            
            me.controller.pathToEndPoint.Pop();
            me.controller.handleWarps(new Rectangle(me.controller.pathToEndPoint.Peek().X * 64, me.controller.pathToEndPoint.Peek().Y * 64, 64, 64));
            me.Position = new Vector2(me.controller.pathToEndPoint.Peek().X * 64, me.controller.pathToEndPoint.Peek().Y * 64 + 16);
        }
    }

    public static void JumpOutOfChair(this NPC me)
    {
        me.get_IsSittingDown().Set(false);
        me.Sprite.ClearAnimation();

        me.JumpTo(me.controller.pathToEndPoint.First().ToVector2() * 64f);
        //me.Freeze();
        //me.set_AfterLerp(c => c.Unfreeze());
    }

    public static void Freeze(this NPC me)
    {
        AccessTools.Field(typeof(Character), "freezeMotion").SetValue(me, true);
    }

    public static void Unfreeze(this NPC me)
    {
        AccessTools.Field(typeof(Character), "freezeMotion").SetValue(me, false);
    }

    public static void JumpTo(this NPC me, Vector2 pixelPosition)
    {
        me.set_LerpStartPosition(me.Position);
        me.set_LerpEndPosition(pixelPosition);
        me.set_LerpPosition(0f);
        me.set_LerpDuration(0.3f);
    }
}
