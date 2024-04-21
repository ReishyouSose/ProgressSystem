namespace ProgressSystem.Core.Requirements;

public class EmptyRequirement : Requirement
{
    public EmptyRequirement() : base() {
        Texture = Texture2DGetter.Default;
    }
    public override void Reset()
    {
        base.Reset();
        Completed = true;
    }
}
