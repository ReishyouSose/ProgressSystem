using ProgressSystem.Core.Interfaces;

namespace ProgressSystem.Core.Rewards;

public class CombineReward : Reward
{
    public readonly RewardList Rewards = [];
    private readonly HashSet<int> selected = [];
    private int count = 1;
    /// <summary>
    /// 可选择的奖励数, 默认 1
    /// </summary>
    public int Count { get => count; set => count = Math.Max(value, 1); }
    private bool selectLocked;

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
        if (selectLocked)
        {
            Main.NewText("已锁定");
            return null;
        }
        int index = Rewards.IndexOf(reward);
        if (index == -1)
            return null;
        if (selected.Contains(index))
        {
            selected.Remove(index);
            return false;
        }
        if (selected.Count >= Count)
        {
            Main.NewText("已达上限");
            return false;
        }
        selected.Add(index);
        return true;
    }
    public bool Contains(int index) => selected.Contains(index);
    protected override bool Receive()
    {
        selectLocked = true;
        int i = 0;
        foreach (Reward reward in Rewards)
        {
            if (selected.Contains(i) && !reward.ReceiveSafe())
                return false;
            i++;
        }
        return true;
    }

    public IEnumerable<IAchievementNode> NodeChildren => Rewards;
}
