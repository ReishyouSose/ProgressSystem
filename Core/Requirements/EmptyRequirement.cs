namespace ProgressSystem.Core.Requirements;

public class EmptyRequirement : Requirement
{
    protected EmptyRequirement() : base() { }
    public override void Reset()
    {
        base.Reset();
        Completed = true;
    }
}
