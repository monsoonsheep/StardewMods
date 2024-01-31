using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.UI.Options;

namespace MyCafe.UI;
internal class TimingPage : OptionsPageBase
{
    public TimingPage(CafeMenu parent, Rectangle bounds, Texture2D sprites) : base("Timings", bounds, parent, sprites)
    {
        int optionNumber = 43430;
        Options.Add(new OptionTimeSet(I18n.Menu_OpeningTime(), Mod.Cafe.OpeningTime.Value, 0700, 1800, OptionSlotSize, optionNumber,
            (time) => Mod.Cafe.OpeningTime.Set(time), Sprites));
        Options.Add(new OptionTimeSet(I18n.Menu_ClosingTime(), Mod.Cafe.ClosingTime.Value, 1100, 2500, OptionSlotSize, optionNumber + OptionTimeSet.NumberOfComponents,
            (time) => Mod.Cafe.ClosingTime.Set(time), Sprites));

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
