using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCafe.Data.Models.Appearances;
internal class OutfitModel : AppearanceModel
{
    public bool IncludesHair { get; set; } = false;
}
