using HarmonyLib;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using Microsoft.Xna.Framework;
using StardewValley.Locations;
using StardewValley.Network;
using xTile.Dimensions;

namespace PassOutOnFarm.Framework.Patching
{
    internal class PassingOutPatches : PatchList
    {
        internal static (Farmer farmer, LocationRequest request) RequestForBedWarp;

        public PassingOutPatches()
        {
            Patches = new List<Patch>
            {
                new (
                    typeof(Farmer),
                    "performPassoutWarp",
                    new[] { typeof(Farmer), typeof(string), typeof(Point), typeof(bool) },
                    postfix: nameof(PerformPassoutWarpPostfix)),
                new (
                    typeof(LocationRequest),
                    "Warped",
                    new[] { typeof(GameLocation) },
                    prefix: nameof(LocationRequestWarpedPrefix),
                    postfix: nameof(LocationRequestWarpedPostfix)),
            };
        }
        
        /// <summary>
        /// performPassoutWarp() initiates a LocationRequest and (through a call chain) stores it in Game1.locationRequest.
        /// This is here to store that in a field only if the player's location is Farm
        /// </summary>
        private static void PerformPassoutWarpPostfix(Farmer who, string bed_location_name, Point bed_point, bool has_bed)
        {
            GameLocation passoutLocation = ModEntry.ModHelper.Reflection.GetField<NetLocationRef>(who, "currentLocationRef").GetValue().Value;
            if (passoutLocation is Farm)
            {
                RequestForBedWarp = (who, Game1.locationRequest);
            }
        }

        /// <summary>
        /// This determines if the warp request was meant as a "passed out, warp to bed" request.
        /// <see cref="RequestForBedWarp"/> is only set by the postfix for <see cref="Farmer.performPassoutWarp"/>.
        /// </summary>
        /// <remarks>
        /// If that postfix sets it to a <see cref="LocationRequest"/> object, then we set out state to the player's money,
        /// as a handoff to this method's postfix.
        /// </remarks>
        private static bool LocationRequestWarpedPrefix(LocationRequest __instance, GameLocation location, out int __state)
        {
            __state = __instance.Equals(RequestForBedWarp.request) ? RequestForBedWarp.farmer.Money : -1;

            return true;
        }

        /// <summary>
        /// If __state was set to the player's money value, it means the LocationRequest saved in our static field is a
        /// location request to warp the player to bed after they've been tired.
        /// </summary>
        private static void LocationRequestWarpedPostfix(LocationRequest __instance, GameLocation location, int __state)
        {
            if (__state == -1 || __instance.Location is not FarmHouse)
                return;

            Farmer who = RequestForBedWarp.farmer;
            string mailForPassout = who.mailForTomorrow.FirstOrDefault(m => m.StartsWith("passedOut"));
            if (string.IsNullOrEmpty(mailForPassout))
                return;

            // Undo changes done by the game's pass out method
            who.Money = __state;
            who.mailForTomorrow.Remove(mailForPassout);

            // Set the trigger for the "I carried you to bed" event for next day.
            if (who.isMarried())
            {
                NPC spouse = who.getSpouse();
                ModEntry.toStartWakeUpEvent = true;
            }

            who = null;
            RequestForBedWarp.request = null;
        }
    }
}
