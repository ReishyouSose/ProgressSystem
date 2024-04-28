using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Localization;

namespace ProgressSystem.Core.Requirements.TileRequirements;

// TODO: Listen
public class InteractWithChestRequirement : Requirement
{
    public Func<Tile, bool>? Condition;
    public LocalizedText? ConditionDescription;
    public InteractWithChestRequirement(Func<Tile, bool> condition, LocalizedText conditionDescription) : this()
    {
        Condition = condition;
        ConditionDescription = conditionDescription;
    }
    protected InteractWithChestRequirement() : base() { }
    protected override object?[] DisplayNameArgs => [ConditionDescription?.Value ?? "?"];
}

public class InteractWithAnyChestRequirement : InteractWithChestRequirement
{
    public InteractWithAnyChestRequirement(LocalizedText? conditionDescription = null) :
        base(tile => true, conditionDescription ?? ModInstance.GetLocalization("Requirements.InteractWithAnyChestRequirement.ConditionDescription"))
    { }
    protected InteractWithAnyChestRequirement() : base() { }
}
