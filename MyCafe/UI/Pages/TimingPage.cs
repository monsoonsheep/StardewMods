using Microsoft.Xna.Framework;
using Monsoonsheep.StardewMods.MyCafe.UI.Options;

namespace Monsoonsheep.StardewMods.MyCafe.UI.Pages;
internal class TimingPage : OptionsPageBase
{
    public TimingPage(CafeMenu parent, Rectangle bounds) : base("Timings", bounds, parent)
    {
        int optionNumber = 43430;
        this.Options.Add(new OptionTimeSet(I18n.Menu_OpeningTime(), Mod.Cafe.OpeningTime, 0700, 1800, this.OptionSlotSize, optionNumber,
            (time) =>
            {
                if (Mod.Cafe.Enabled != 2)
                    Mod.Cafe.OpeningTime = time;
            }));
        this.Options.Add(new OptionTimeSet(I18n.Menu_ClosingTime(), Mod.Cafe.ClosingTime, 1100, 2500, this.OptionSlotSize, optionNumber + OptionTimeSet.NumberOfComponents,
            (time) =>
            {
                if (Mod.Cafe.Enabled != 2)
                    Mod.Cafe.ClosingTime = time;
            }));
    }
}
