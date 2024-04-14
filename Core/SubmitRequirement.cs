namespace ProgressSystem.Core;

// TODO
/// <summary>
/// 需要玩家在成就页面自行提交
/// </summary>
public class SubmitRequirement : Requirement
{
    public SubmitRequirement()
    {
        Completed = true;
    }
    public override void Reset()
    {
        base.Reset();
        Completed = true;
    }
}
