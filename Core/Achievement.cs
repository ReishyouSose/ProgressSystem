using System.IO;

namespace ProgressSystem.Core;

/// <summary>
/// 成就
/// </summary>
public sealed class Achievement
{
    #region 不会在正常游玩时改变的 字段 / 属性
    /// <summary>
    /// 自己所归属的成就页
    /// </summary>
    public AchievementPage Page = null!;

    /// <summary>
    /// 添加此成就的模组
    /// </summary>
    public Mod Mod = null!;

    /// <summary>
    /// 内部名
    /// </summary>
    public string Name = null!;

    /// <summary>
    /// 全名, 在同一成就页内不允许有相同全名的成就
    /// </summary>
    public string FullName => string.Join('.', Mod.Name, Name);

    /// <summary>
    /// 包含页名的全名, 可作为标识符, 全局唯一
    /// </summary>
    public string FullNameWithPage => $"{Page.FullName}.{FullName}";

    #region 本地化文本和图片的储存路径
    public string LocalizedKey = null!;

    public string TexturePath = null!;
    #endregion

    #endregion

    #region 给 UI 提供的
    /// <summary>
    /// 显示的名字
    /// </summary>
    public TextGetter DisplayName
    {
        get => !_displayName.IsNone ? _displayName : _displayName = Mod.GetLocalization($"Achievements.{LocalizedKey}.DisplayName");
        set => _displayName = value;
    }
    /// <summary>
    /// 鼠标移上去时显示的提示
    /// </summary>
    public TextGetter Tooltip
    {
        get => !_tooltip.IsNone ? _tooltip : _tooltip = Mod.GetLocalization($"Achievements.{LocalizedKey}.Tooltip");
        set => _tooltip = value;
    }
    /// <summary>
    /// 详细说明
    /// </summary>
    public TextGetter Description
    {
        get => !_description.IsNone ? _description : _description = Mod.GetLocalization($"Achievements.{LocalizedKey}.Description");
        set => _description = value;
    }
    /// <summary>
    /// 图片
    /// </summary>
    public Texture2DGetter Texture
    {
        get => !_texture.IsNone ? _texture : _texture = $"{Mod.Name}/Assets/Textures/Achievements/{TexturePath}";
        set => _texture = value;
    }

    private TextGetter _displayName;
    private TextGetter _tooltip;
    private TextGetter _description;
    private Texture2DGetter _texture;

    /// <summary>
    /// 在 UI 上的默认排列, 当为空时 UI 上的默认排列则自动给出
    /// 当在 UI 上设置了此成就的位置时此项失效
    /// </summary>
    public Vector2? DefaultPosition;
    #endregion

    #region 前后置相关
    /// <summary>
    /// 前置任务(都在同一页)
    /// </summary>
    public IReadOnlyList<Achievement> Predecessors
    {
        get
        {
            InitializePredecessorsSafe();
            return predecessors!;
        }
    }

    /// <summary>
    /// 后置任务(都在同一页)
    /// </summary>
    public IReadOnlyList<Achievement> Successors => successors;

    /// <summary>
    /// 设置所有的前置名(覆盖)
    /// </summary>
    /// <param name="predecessorNames"></param>
    public void SetPredecessorNames(List<string>? predecessorNames, bool isFullName)
    {
        predecessors?.ForEach(p => p.successors.Remove(this));
        predecessors = null;
        _predecessorNames = predecessorNames == null ? [] : [..predecessorNames.Select(p => (p, isFullName))];
    }
    /// <summary>
    /// 添加一个前置
    /// </summary>
    /// <param name="predecessorFullName"></param>
    public void AddPredecessor(string predecessorName, bool isFullName)
    {
        if (predecessors == null)
        {
            (_predecessorNames ??= []).Add((predecessorName, isFullName));
            return;
        }
        var predecessor = isFullName ? Page.Get(predecessorName) : Page.GetByName(predecessorName);
        if (predecessor == null)
        {
            return;
        }
        predecessor.successors.Add(this);
        predecessors.Add(predecessor);
    }
    /// <summary>
    /// 移除一个前置
    /// </summary>
    /// <param name="predecessorName"></param>
    public void RemovePredecessor(string predecessorName, bool isFullName)
    {
        if (predecessors == null)
        {
            if (_predecessorNames == null)
            {
                return;
            }
            _predecessorNames.Remove((predecessorName, isFullName));
            return;
        }
        foreach (var i in predecessors.Count)
        {
            var predecessor = predecessors[i];
            if (predecessor.Name != predecessorName)
            {
                continue;
            }
            predecessor.successors.Remove(this);
            predecessors.RemoveAt(i);
            break;
        }
        predecessors.Remove(isFullName ? a => a.FullName == predecessorName : a => a.Name == predecessorName);
    }
    public void InitializePredecessorsSafe()
    {
        if (predecessors == null)
        {
            predecessors = [];
            _predecessorNames?.ForEach(p => AddPredecessor(p.Value, p.IsFullName));
        }
    }

    private List<Achievement>? predecessors;
    private readonly List<Achievement> successors = [];
    private List<(string Value, bool IsFullName)>? _predecessorNames;

    /// <summary>
    /// <br/>需要多少个前置才能开始此任务
    /// <br/>默认 <see langword="null"/> 代表需要所有前置完成
    /// <br/>如果此值大于前置数, 那么以前置数为准
    /// <br/>1 代表只需要任意前置完成即可
    /// <br/>0 代表实际上不需要前置完成, 前置只是起提示作用
    /// <br/>负数(-n)代表有 n 个前置完成时此任务封闭, 不可再完成
    /// </summary>
    public int? PredecessorCountNeeded;

    public Func<bool> IsPredecessorsMet;
    public bool DefaultIsPredecessorsMet()
    {
        int count = Predecessors.Count;
        int needed = (PredecessorCountNeeded ?? count).WithMax(count);
        if (needed <= 0)
        {
            return true;
        }
        int sum = Predecessors.Sum(p => p.State.IsCompleted().ToInt());
        return sum >= needed;
    }
    #endregion

    #region 条件
    /// <summary>
    /// 条件
    /// </summary>
    public RequirementList Requirements = null!;
    #endregion

    #region 奖励
    /// <summary>
    /// 奖励
    /// </summary>
    public RewardList Rewards = null!;

    public delegate void OnGetAllRewardDelegate(bool allReceived);
    public event OnGetAllRewardDelegate? OnGetAllReward;
    /// <summary>
    /// 一键获得所有奖励
    /// </summary>
    /// <returns>是否全部获取</returns>
    public bool GetAllReward()
    {
        bool result = true;
        Rewards.ForeachDo(r => result &= r.Receive());
        OnGetAllReward?.Invoke(result);
        return result;
    }
    #endregion

    #region 初始化
    static Achievement()
    {
        // 在成就页解锁时尝试解锁成就
        AchievementPage.OnUnlockStatic += p => p.Achievements.Values.ForeachDo(a => a.TryComplete());
        // 在条件完成时尝试完成成就
        Requirement.OnCompleteStatic += r => r.Achievement.TryComplete();
        // 在成就解锁时尝试完成成就
        OnUnlockStatic += a => a.TryComplete();
        // 在成就完成时尝试关闭成就, 然后检查后置的状态 (这两步不能颠倒)
        OnCompleteStatic += a =>
        {
            a.TryClose();
            a.Successors.ForeachDo(s => s.CheckState());
        };
    }
    /// <summary>
    /// 创建一个成就, 若页内有同名成就则报错
    /// </summary>
    /// <param name="page">属于哪一页</param>
    /// <param name="mod">所属模组, 用于查找对应资源, 注意不是 <paramref name="page"/> 的模组, 而是添加此成就的模组</param>
    /// <param name="name">内部名, 同一成就页内不允许有相同内部名的成就</param>
    /// <param name="predecessorNames">
    /// <br/>前置的名字(需要在同一页)
    /// <br/>在 Load 阶段不必需要前置在此时就存在
    /// <br/>但在 PostSetup 阶段及之后就需要了
    /// <br/>也可以通过 <see cref="AddPredecessor"/> 添加前置, 条件相同
    /// </param>
    /// <param name="requirements">条件</param>
    /// <param name="rewards">奖励</param>
    /// <param name="texture">图片</param>
    /// <param name="displayName">显示的名字, 默认通过对应 Mod 的 Achievements.[ModName].[PageName].[AcievementName].DisplayName 获取</param>
    /// <param name="tooltip">鼠标移上去时显示的提示, 默认通过对应 Mod 的 Achievements.[ModName].[PageName].[AcievementName].Tooltip 获取</param>
    /// <param name="description">详细说明, 默认通过对应 Mod 的 Achievements.[ModName].[PageName].[AcievementName].Description 获取</param>
    public static Achievement Create(AchievementPage page, Mod mod, string name,
        List<string>? predecessorNames = null,
        bool isPredecessorFullName = false,
        List<Requirement>? requirements = null,
        List<Reward>? rewards = null,
        TextGetter displayName = default,
        TextGetter tooltip = default,
        TextGetter description = default,
        Texture2DGetter texture = default,
        Vector2? defaultPosition = null)
    {
        Achievement achievement = new();
        achievement.Mod = mod;
        achievement.Page = page;
        achievement.Name = name;
        achievement.LocalizedKey = string.Join('.', page.FullName, name);
        achievement.TexturePath = string.Join('/', page.Name, name);

        achievement.SetPredecessorNames(predecessorNames, isPredecessorFullName);
        achievement.Requirements = new(achievement, requirements);
        achievement.Rewards = new(achievement, rewards);

        achievement.DisplayName = displayName;
        achievement.Tooltip = tooltip;
        achievement.Description = description;
        achievement.Texture = texture;
        achievement.DefaultPosition = defaultPosition;

        page.AddF(achievement);
        return achievement;
    }

    private Achievement()
    {
        ReachedStableState = DefaultReachedStableState;
        CloseCondition = DefaultCloseCondition;
        IsPredecessorsMet = DefaultIsPredecessorsMet;
        UnlockCondition = DefaultUnlockCondition;
        CompleteCondition = DefaultCompleteCondition;
    }

    public void PostInitialize()
    {
        InitializePredecessorsSafe();
        _ = DisplayName;
        _ = Tooltip;
        _ = Description;
        _ = Texture;
    }
    #endregion

    #region 重置与开始
    public static event Action<Achievement>? OnResetStatic;
    public event Action? OnReset;
    public void Reset()
    {
        State = StateEnum.Locked;
        Requirements.ForeachDo(r => r.Reset());
        OnResetStatic?.Invoke(this);
        OnReset?.Invoke();
    }
    public static event Action<Achievement>? OnStartStatic;
    public event Action? OnStart;
    public void Start()
    {
        CheckState();
        Requirements.ForeachDo(r => r.Start());
        OnStartStatic?.Invoke(this);
        OnStart?.Invoke();
    }
    #endregion

    #region 状态 (完成 / 解锁)
    public enum StateEnum
    {
        Locked,
        Unlocked,
        Completed,
        Closed
    }
    public StateEnum State { get; private set; }
    public Func<bool> ReachedStableState;
    public bool DefaultReachedStableState()
    {
        return PredecessorCountNeeded < 0 ? State.IsClosed() : State.IsCompleted();
    }

    public Func<bool> UnlockCondition;
    public bool DefaultUnlockCondition()
    {
        return Page.State != AchievementPage.StateEnum.Locked && IsPredecessorsMet();
    }
    public static event Action<Achievement>? OnUnlockStatic;
    public event Action? OnUnlock;
    public void UnlockSafe()
    {
        if (!State.IsLocked())
        {
            return;
        }
        State = StateEnum.Unlocked;
        OnUnlockStatic?.Invoke(this);
        OnUnlock?.Invoke();
    }
    public void TryUnlock()
    {
        if (State.IsLocked() && IsPredecessorsMet())
        {
            UnlockSafe();
        }
        
    }

    public Func<bool> CompleteCondition;
    public bool DefaultCompleteCondition() => Requirements.All(r => r.Completed);
    public static event Action<Achievement>? OnCompleteStatic;
    public event Action? OnComplete;
    public void CompleteSafe()
    {
        if (!State.IsUnlocked())
        {
            return;
        }
        State = StateEnum.Completed;
        // !!!!!!!! Test
        Main.NewText($"成就{DisplayName.Value}完成!");
        OnCompleteStatic?.Invoke(this);
        OnComplete?.Invoke();
    }
    public void TryComplete()
    {
        if (State.IsUnlocked() && CompleteCondition())
        {
            CompleteSafe();
        }
    }
    
    public Func<bool> CloseCondition;
    public bool DefaultCloseCondition()
    {
        return PredecessorCountNeeded < 0 && Predecessors.Sum(p => p.State.IsCompleted().ToInt()) >= -PredecessorCountNeeded;
    }
    public static event Action<Achievement>? OnCloseStatic;
    public event Action? OnClose;
    public void CloseSafe()
    {
        if (!State.IsCompleted())
        {
            return;
        }
        State = StateEnum.Closed;
        OnCloseStatic?.Invoke(this);
        OnClose?.Invoke();
    }
    public void TryClose()
    {
        if (State.IsCompleted() && CloseCondition())
        {
            CloseSafe();
        }
    }

    public void CheckState()
    {
        if (ReachedStableState())
        {
            return;
        }
        TryUnlock();
        TryComplete();
        TryClose();
    }
    #endregion

    #region 数据存取
    // todo: 成就本身与奖励相关的数据存取
    public void SaveDataInWorld(TagCompound tag)
    {
        tag.SaveListData("Requirements", Requirements, (r, t) => r.SaveDataInWorld(t));
    }
    public void LoadDataInWorld(TagCompound tag)
    {
        tag.LoadListData("Requirements", Requirements, (r, t) => r.LoadDataInWorld(t));
    }
    public void SaveDataInPlayer(TagCompound tag)
    {
        tag.SetWithDefault("State", State.ToString(), StateEnum.Locked.ToString());
        tag.SaveListData("Requirements", Requirements, (r, t) => r.SaveDataInPlayer(t));
    }
    public void LoadDataInPlayer(TagCompound tag)
    {
        if (Enum.TryParse(tag.GetWithDefault("State", StateEnum.Locked.ToString()), out StateEnum state))
        {
            State = state;
        }
        tag.LoadListData("Requirements", Requirements, (r, t) => r.LoadDataInPlayer(t));
    }
    #endregion

    #region 网络同步
    // todo: 成就本身与奖励相关的网络同步
    public void NetSend(BinaryWriter writer)
    {
        Requirements.ForeachDo(r => r.NetSend(writer));
    }
    public void NetReceive(BinaryReader reader)
    {
        Requirements.ForeachDo(r => r.NetReceive(reader));
    }
    #endregion

    public override string ToString()
    {
        var requirementsString = string.Join(",\n    ", Requirements.Select(r => r.ToString()));
        var rewardsString = string.Join(",\n    ", Rewards.Select(r => r.ToString()));
        var predecessorsString = string.Join(",\n    ", Predecessors.Select(r => r.FullName));
        return $"""
            {FullName}: {State}, {nameof(Predecessors)}: [{predecessorsString}], {nameof(Requirements)}: [
                {requirementsString}
            ], {nameof(Rewards)}: [
                {rewardsString}
            ]
            """;
    }
}

public static class AchievementStateEnumExtensions
{
    public static bool IsLocked(this Achievement.StateEnum self) => self == Achievement.StateEnum.Locked;
    public static bool IsUnlocked(this Achievement.StateEnum self) => self == Achievement.StateEnum.Unlocked;
    public static bool IsCompleted(this Achievement.StateEnum self) => self == Achievement.StateEnum.Completed;
    public static bool IsClosed(this Achievement.StateEnum self) => self == Achievement.StateEnum.Closed;
}
