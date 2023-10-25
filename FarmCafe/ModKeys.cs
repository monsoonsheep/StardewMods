using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// ReSharper disable InconsistentNaming

namespace FarmCafe
{
    internal static class ModKeys
    {
        internal static readonly string ASSETS_NPCSCHEDULE_PREFIX = $"{ModEntry.ModManifest.UniqueID}/NPCSchedules/";
        internal static readonly string SIGNBOARD_BUILDING_CLICK_EVENT_KEY = $"{ModEntry.ModManifest.UniqueID}_OpenCafeMenu";

        internal static readonly string MODDATA_MENUITEMSLIST = "FarmCafeMenuItems";
        internal static readonly string MODDATA_OPENCLOSETIMES = "FarmCafeOpenCloseTimes";
        internal static readonly string MODDATA_NPCSLASTVISITEDDATES = "FarmCafeNPCsLastVisited";
        internal static readonly string MODDATA_CHAIRRESERVED = "FarmCafeChairIsReserved";
        internal static readonly string MODDATA_TABLERESERVED = "FarmCafeTableIsReserved";
        internal static readonly string MODDATA_CHAIRTABLE = "FarmCafeChairTable";

        internal static readonly string MAPSEATS_TILEPROPERTY = "FarmCafeSeats";
    }
}
