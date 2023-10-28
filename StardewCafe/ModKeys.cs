using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// ReSharper disable InconsistentNaming

namespace StardewCafe
{
    internal static class ModKeys
    {
        internal static readonly string ASSETS_NPCSCHEDULE_PREFIX = $"{ModEntry.ModManifest.UniqueID}/NPCSchedules/";
        internal static readonly string SIGNBOARD_BUILDING_CLICK_EVENT_KEY = $"{ModEntry.ModManifest.UniqueID}_OpenCafeMenu";
        internal static readonly string CAFE_SIGNBOARD_BUILDING_ID = $"{ModEntry.ModManifest.UniqueID}_CafeSignboard";

        internal static readonly string MODDATA_MENUITEMSLIST = "VisitorFrameworkMenuItems";
        internal static readonly string MODDATA_OPENCLOSETIMES = "VisitorFrameworkOpenCloseTimes";
        internal static readonly string MODDATA_NPCSLASTVISITEDDATES = "VisitorFrameworkNPCsLastVisited";
        internal static readonly string MODDATA_CHAIRRESERVED = "VisitorFrameworkChairIsReserved";
        internal static readonly string MODDATA_TABLERESERVED = "VisitorFrameworkTableIsReserved";
        internal static readonly string MODDATA_CHAIRTABLE = "VisitorFrameworkChairTable";

        internal static readonly string MAPSEATS_TILEPROPERTY = "VisitorFrameworkSeats";
    }
}
