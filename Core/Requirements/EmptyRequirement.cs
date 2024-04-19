namespace ProgressSystem.Core.Requirements;

public class EmptyRequirement : Requirement
{
    public EmptyRequirement() : base() { }
    public override void Reset()
    {
        base.Reset();
        Completed = true;
    }
}
