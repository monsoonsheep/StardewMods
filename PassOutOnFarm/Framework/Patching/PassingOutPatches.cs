using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Network;
using Patch = CollapseOnFarmFix.Framework.Patching.Patch;

namespace CollapseOnFarmFix.Framework.Patching
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
            __state = __instance.Equals(RequestForBedWarp.request) 
                ? RequestForBedWarp.farmer.Money 
                : -1;

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

            Farmer farmer = RequestForBedWarp.farmer;
            string mailForPassout = farmer.mailForTomorrow.FirstOrDefault(m => m.StartsWith("passedOut"));
            if (string.IsNullOrEmpty(mailForPassout))
                return;

            // Undo changes done by the game's pass out method
            farmer.Money = __state;
            farmer.mailForTomorrow.Remove(mailForPassout);

            // Setup for next day's event. 
            if (Game1.isRaining)
                ModEntry.WeatherLastNight = "rain";
            else if (Game1.isSnowing)
                ModEntry.WeatherLastNight = "snow";
            else if (Game1.isLightning)
                ModEntry.WeatherLastNight = "lightning";
            else if (Game1.isDebrisWeather)
                ModEntry.WeatherLastNight = "debris";
            else
                ModEntry.WeatherLastNight = "sunny";

            // Only setup the event if player is married or has a roommate
            if (farmer.isMarried() || farmer.hasRoommate())
            {
                ModEntry.PartnerNpc = farmer.spouse;
                ModEntry.ToStartWakeUpEvent = true;
            }

            farmer = null;
            RequestForBedWarp.request = null;
        }
    }
}
