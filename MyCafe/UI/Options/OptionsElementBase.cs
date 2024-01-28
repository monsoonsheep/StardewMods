using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley.Menus;

namespace MyCafe.UI.Options;
internal abstract class OptionsElementBase : OptionsElement
{
    internal int currentlySnapped = 0;

    protected OptionsElementBase(string label, Rectangle bounds) : base(label, bounds, -1)
    {

    }

    internal abstract Vector2 Snap(int direction);
}
