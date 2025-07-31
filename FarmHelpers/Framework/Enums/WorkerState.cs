using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewMods.FarmHelpers.Framework.Enums;
internal enum WorkerState
{
    OffDuty, MovingToFarm, MovingToJob, DoingJob, MovingOutOfFarm, GoingHome, FailedToGoToWork
}
