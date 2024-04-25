using ProgressSystem.Core.Interfaces;

namespace ProgressSystem.Core.Rewards;

public class CombineReward : Reward
{
    public RewardList Rewards = [];
    private int count = 1;
    /// <summary>
    /// 可选择的奖励数, 默认 1
    /// </summary>
    public int Count { get => count; set => count = Math.Max(value, 1); }
    
    public CombineReward() : base() { }
    public CombineReward(int count) : this()
    {
        Count = count;
    }
    [SpecializeAutoConstruct(Disabled = true)]
    public CombineReward(int count, params Reward[]? rewards) : this(count)
    {
        if (rewards != null)
        {
            Rewards.AddRange(rewards);
        }
    }
    public override void Initialize(Achievement achievement)
    {
        base.Initialize(achievement);
        Rewards.AddOnAddAndDo(r => r.Initialize(achievement));
    }

    // TODO: 选择奖励领取
    protected override bool Receive() => Rewards.All(x => x.Received);

    public IEnumerable<IAchievementNode> NodeChildren => Rewards;

    // TODO: 完善

}
