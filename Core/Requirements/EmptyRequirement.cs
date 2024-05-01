namespace ProgressSystem.Core.Requirements;

public class EmptyRequirement : Requirement
{
    public EmptyRequirement() : base() {
        Texture = Texture2DGetter.Default;
        State = StateEnum.Completed;
    }
    public override void Reset()
    {
        base.Reset();
        State = StateEnum.Completed;
    }
}
