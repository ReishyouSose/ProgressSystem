namespace ProgressSystem.Core.Rewards;

public class EmptyReward : Reward
{
    public override bool Receive() => Received = true;
}
