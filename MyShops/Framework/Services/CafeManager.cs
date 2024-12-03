using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewMods.MyShops.Framework.Services;
internal class CafeManager : Service
{
    public CafeManager(
        ILogger logger,
        IManifest manifest
        ) : base(logger, manifest)
    {
    }
}
