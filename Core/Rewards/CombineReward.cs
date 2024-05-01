using ProgressSystem.Common.Configs;
using ProgressSystem.Core.Interfaces;
using ProgressSystem.Core.NetUpdate;
using ProgressSystem.Core.StaticData;

namespace ProgressSystem.Core.Rewards;

public class CombineReward : Reward, IAchievementNode
{
    #region 数据
    public readonly RewardList Rewards;
    private int _count = 1;
    /// <summary>
    /// 可选择的奖励数, 默认 1
    /// </summary>
    public int Count { get => _count; set => _count = value.WithMin(1); }
    #endregion

    #region 初始化
    protected override object?[] DisplayNameArgs => [Count];
    public CombineReward() : base()
    {
        Rewards = new(null, r =>
        {
            r.OnStartReceived += () => ElementStartReceive(r);
            r.OnTotallyReceived += ElementTotallyReceived;
        });
    }
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
    [SpecializeAutoConstruct(Disabled = true)]
    public CombineReward(params Reward[]? rewards) : this(1, rewards) { }
    public override void Initialize(Achievement achievement)
    {
        base.Initialize(achievement);
        Rewards.AddOnAddAndDo(r => r.Initialize(achievement));
    }
    public override void PostInitialize()
    {
        base.PostInitialize();
        OnUnlock += () => Rewards.ForeachDo(r => r.TryUnlock());
    }
    public override void Reset()
    {
        base.Reset();
        startReceiveds.Clear();
    }
    #endregion

    #region 选择
    [Obsolete("不缓存选择")]
    private readonly HashSet<int> selected = [];
    [Obsolete("不缓存选择")]
    private bool selectLocked;
    [Obsolete("不缓存选择")]
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
    [Obsolete("不缓存选择")]
    public bool Contains(int index) => selected.Contains(index);
    #endregion

    protected override void Close()
    {
        base.Close();
        Rewards.ForeachDo(r => r.CloseSafe());
    }
    protected override void Disable()
    {
        base.Disable();
        Rewards.ForeachDo(r => r.DisableSafe());
    }

    #region 领取
    protected HashSet<int> startReceiveds = [];
    protected override bool AutoAssignReceived => false;
    protected bool suppressReceiveAll;
    protected override void Receive()
    {
        if (suppressReceiveAll)
        {
            goto AfterReceiveAll;
        }

        int count = Count;

        // 继续领取没领完的奖励
        foreach (var index in startReceiveds)
        {
            Rewards[index].ReceiveSafe();
        }

        // 尝试开始领取 index 下标的奖励, 返回是否已满
        bool Check(int index)
        {
            return startReceiveds.Count >= count
                || !startReceiveds.Contains(index)
                && DoIf(!startReceiveds.Contains(index)
                && startReceiveds.Count < count, Rewards[index].ReceiveSafe)
                && startReceiveds.Count >= count; // && false
        }

        // 尝试自动领取
        suppressUpdateState = true;
        switch (ClientConfig.Instance.AutoSelectReward)
        {
        case ClientConfig.AutoSelectRewardEnum.First:
            break;
        case ClientConfig.AutoSelectRewardEnum.Random:
            Range(count).ToList().Shuffle().ForeachDoB(Check);
            break;
        default:
            Range(count).ForeachDoB(Check);
            break;
        }
        suppressUpdateState = false;

    AfterReceiveAll:
        // 更新状态
        UpdateState();
    }

    protected bool suppressUpdateState;
    protected void UpdateState()
    {
        if (suppressUpdateState)
        {
            return;
        }
        int count = Count;
        if (startReceiveds.All(i => Rewards[i].State == StateEnum.Received)
             && (startReceiveds.Count >= count
            || !Range(count).Any(i => !startReceiveds.Contains(i)
            && Rewards[i].State is StateEnum.Locked or StateEnum.Unlocked)))
        {
            State = StateEnum.Received;
            return;
        }
        if (startReceiveds.Count > 0)
        {
            State = StateEnum.Receiving;
        }
    }

    protected void ElementStartReceive(Reward reward)
    {
        var index = Rewards.FindIndexOf(reward);
        if (index == -1)
        {
            return;
        }
        startReceiveds.Add(index);
        if (startReceiveds.Count >= Count)
        {
            for (int i = 0; i < Rewards.Count; ++i)
            {
                if (!startReceiveds.Contains(i))
                {
                    Rewards[i].CloseSafe();
                }
            }
        }
        suppressReceiveAll = true;
        ReceiveSafe();
        suppressReceiveAll = false;
    }
    protected void ElementTotallyReceived()
    {
        suppressReceiveAll = true;
        ReceiveSafe();
        suppressReceiveAll = false;
    }
    #endregion

    IEnumerable<IAchievementNode> IAchievementNode.NodeChildren => Rewards;

    #region 数据存取
    public override void SaveDataInPlayer(TagCompound tag)
    {
        base.SaveDataInPlayer(tag);
        tag.SaveListData("Rewards", Rewards, (r, t) => r.SaveDataInPlayer(t));
        tag["StartReceiveds"] = startReceiveds.ToList();
    }
    public override void LoadDataInPlayer(TagCompound tag)
    {
        base.LoadDataInPlayer(tag);
        tag.LoadListData("Rewards", Rewards, (r, t) => r.LoadDataInPlayer(t));
        startReceiveds = tag.GetWithDefault<List<int>>("StartReceiveds")?.ToHashSet() ?? [];
    }
    public override void SaveDataInWorld(TagCompound tag)
    {
        base.SaveDataInWorld(tag);
        tag.SaveListData("Rewards", Rewards, (r, t) => r.SaveDataInWorld(t));
    }
    public override void LoadDataInWorld(TagCompound tag)
    {
        base.LoadDataInWorld(tag);
        tag.LoadListData("Rewards", Rewards, (r, t) => r.LoadDataInWorld(t));
    }
    public override void SaveStaticData(TagCompound tag)
    {
        base.SaveStaticData(tag);
        if (ShouldSaveStaticData)
        {
            tag.SetWithDefault("Count", Count);
        }
        this.SaveStaticDataListTemplate(Rewards, "Rewards", tag);
    }
    public override void LoadStaticData(TagCompound tag)
    {
        base.LoadStaticData(tag);
        if (ShouldSaveStaticData)
        {
            Count = tag.GetWithDefault<int>("Count");
        }
        this.LoadStaticDataListTemplate(Rewards.GetS, Rewards!.SetFS, "Rewards", tag);
    }
    #endregion

    #region 多人同步
    public IEnumerable<INetUpdate> GetNetUpdateChildren() => Rewards;
    #endregion
}
