namespace ProgressSystem.Core.Requirements;

public class EmptyRequirement : Requirement
{
    public override void Reset()
    {
        base.Reset();
        Completed = true;
    }
}
