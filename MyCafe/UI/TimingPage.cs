using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.UI.Options;
using StardewValley;
using StardewValley.Menus;

namespace MyCafe.UI;
internal class TimingPage : OptionsPageBase
{
    public TimingPage(CafeMenu parent, Rectangle bounds) : base("Timings", bounds, parent)
    {
        _options.Add(new OptionTimeSet(I18n.Menu_OpeningTime(), Mod.Cafe.OpeningTime.Value, 0700, 1800, _optionSlotSize, 43430,
            (time) => Mod.Cafe.OpeningTime.Set(time)));
        _options.Add(new OptionTimeSet(I18n.Menu_ClosingTime(), Mod.Cafe.ClosingTime.Value, 1100, 2500, _optionSlotSize, 43432,
            (time) => Mod.Cafe.ClosingTime.Set(time)));
    }

    private void SetOpeningTime(int time)
    {
        Mod.Cafe.OpeningTime.Set(time);
    }

    private void SetClosingTime(int time)
    {
        Mod.Cafe.ClosingTime.Set(time);
    }
}
