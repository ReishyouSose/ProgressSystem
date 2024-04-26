using ProgressSystem.Core.Interfaces;

namespace ProgressSystem.Core.Rewards;

public class CombineReward : Reward
{
    public readonly RewardList Rewards = [];
    private readonly HashSet<int> seleted = [];
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

    public bool? TrySelect(Reward reward)
    {
        int index = Rewards.IndexOf(reward);
        if (index == -1)
            return null;
        if (seleted.Contains(index))
        {
            seleted.Remove(index);
            return false;
        }
        if (seleted.Count >= Count)
        {
            Main.NewText("已达上限");
            return false;
        }
        seleted.Add(index);
        return true;
    }
    public bool Contains(int index) => seleted.Contains(index);
    protected override bool Receive()
    {
        int i = 0;
        foreach (Reward reward in Rewards)
        {
            if (seleted.Contains(i) && !reward.ReceiveSafe())
                return false;
            i++;
        }
        return true;
    }

    public IEnumerable<IAchievementNode> NodeChildren => Rewards;
}
