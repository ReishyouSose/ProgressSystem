using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgressSystem.Core.Requirements.MiscRequirements;

// TODO: Listen
public class OpenBackpackRequirement : Requirement
{
    public OpenBackpackRequirement() : base(ListenTypeEnum.OnStart) { }
}
