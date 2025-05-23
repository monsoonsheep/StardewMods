using SpaceCore.Events;

namespace StardewMods.FoodJoints.Framework.Services;
internal class EventManager
{
    internal static EventManager Instance = null!;

    internal EventManager()
    {
        Instance = this;

        SpaceEvents.OnEventFinished += this.OnEventFinished;
    }

    private void OnEventFinished(object? sender, EventArgs e)
    {
        if (Game1.CurrentEvent.id.Equals(Values.EVENT_CAFEINTRODUCTION) && !Game1.player.Items.ContainsId(Values.CAFE_SIGNBOARD_OBJECT_ID))
        {
            Game1.player.addItemByMenuIfNecessaryElseHoldUp(ItemRegistry.Create<StardewValley.Object>($"(BC){Values.CAFE_SIGNBOARD_OBJECT_ID}"));
        }
    }

    internal string GetCafeIntroductionEvent()
    {
        return string.Empty;

        //GameLocation eventLocation = Game1.locations.First(l => l.isBuildingConstructed(ModKeys.CAFE_SIGNBOARD_BUILDING_ID));
        //Building signboard = eventLocation.getBuildingByType(ModKeys.CAFE_SIGNBOARD_BUILDING_ID);
        //Point signboardTile = new Point(signboard.tileX.Value, signboard.tileY.Value + signboard.tilesHigh.Value);

        //string eventString = Game1.content.Load<Dictionary<string, string>>($"{ModKeys.MODASSET_EVENTS}")[ModKeys.EVENT_CAFEINTRODUCTION];

        //// Replace the encoded coordinates with the position of the signboard building
        //string substituted = Regex.Replace(
        //    eventString,
        //    @"(6\d{2})\s(6\d{2})",
        //    (m) => $"{(int.Parse(m.Groups[1].Value) - 650 + signboardTile.X)} {(int.Parse(m.Groups[2].Value) - 650 + signboardTile.Y)}");

        //return substituted;
    }
}
