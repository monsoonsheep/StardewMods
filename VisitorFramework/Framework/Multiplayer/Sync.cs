using System.Collections.Generic;
using System.Linq;
using VisitorFramework.Framework.Characters;
using StardewValley;
using StardewValley.Objects;

namespace VisitorFramework.Framework.Multiplayer
{
    internal class Sync
    {
        internal static void AddVisitorGroup(VisitorGroup group)
        {
            ModEntry.ModHelper.Multiplayer.SendMessage(group.Members.Select(c => c.Name).ToList(), "UpdateVisitors", modIDs: new[] { ModEntry.ModManifest.UniqueID });
        }

        internal static void RemoveAllVisitors()
        {
            ModEntry.ModHelper.Multiplayer.SendMessage(new List<string>() {}, "RemoveVisitors", modIDs: new[] { ModEntry.ModManifest.UniqueID });
        }

        internal static void UpdateVisitorInfo(Visitor Visitor, string fieldName, object value)
        {
            ModEntry.ModHelper.Multiplayer.SendMessage(value, $"UpdateVisitorInfo/{Visitor.Name}/{fieldName}", modIDs: new[] { ModEntry.ModManifest.UniqueID });
        }
    }
}
