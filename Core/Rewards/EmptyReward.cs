namespace ProgressSystem.Core.Rewards;

public class EmptyReward : Reward
{
    protected override bool Receive() => true;
}
