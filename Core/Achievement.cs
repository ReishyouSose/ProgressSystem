using System.IO;

namespace ProgressSystem.Core;

/// <summary>
/// 成就
/// </summary>
public sealed class Achievement
{
    #region 不会轻易改变的 字段 / 属性
    /// <summary>
    /// 自己所归属的成就页
    /// </summary>
    public AchievementPage Page;

    /// <summary>
    /// 添加此成就的模组
    /// </summary>
    public Mod Mod;

    /// <summary>
    /// 内部名
    /// </summary>
    public string Name;

    /// <summary>
    /// 全名, 默认为模组名与内部名, 在同一成就页内不允许有相同全名的成就
    /// </summary>
    public string FullName => FullNameOverride ?? string.Join('.', Mod.Name, Name);
    public string? FullNameOverride;

    /// <summary>
    /// 包含页名的全名, 可作为标识符, 全局唯一
    /// </summary>
    public string FullNameWithPage => $"{Page.FullName}.{FullName}";
    #endregion

    #region 本地化文本和图片的储存路径
    public string? LocalizedKeyOverride;
    public string LocalizedKey => LocalizedKeyOverride ?? string.Join('.', Page.FullName, Name);

    public string? TexturePathOverride;
    public string TexturePath => TexturePathOverride ?? string.Join('/', Page.Name, Name);
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
        get => !_texture.IsNone ? _texture : _texture = $"{Mod.Name}/Achievements/Textures/{TexturePath}";
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
    public Vector2? PositionOverride;
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

    public Func<bool>? IsPredecessorsMetOverride;
    public bool IsPredecessorsMet()
    {
        if (IsPredecessorsMetOverride != null)
        {
            return IsPredecessorsMetOverride();
        }
        int count = Predecessors.Count;
        int needed = (PredecessorCountNeeded ?? count).WithMax(count);
        if (needed <= 0)
        {
            return true;
        }
        int sum = Predecessors.Sum(p => p.State.IsCompleted().ToInt());
        return sum >= needed;
    }
    public Func<bool> CloseCondition;
    public bool DefaultCloseCondition()
    {
        return PredecessorCountNeeded < 0 && Predecessors.Sum(p => p.State.IsCompleted().ToInt()) >= -PredecessorCountNeeded;
    }
    #endregion

    #region 条件
    /// <summary>
    /// 条件
    /// </summary>
    public List<Requirement> Requirements;
    public Func<bool>? IsRequirementsMetOverride;
    /// <summary>
    /// 条件是否满足
    /// </summary>
    /// <returns>默认返回是否所有条件都分别满足</returns>
    public bool IsRequirementsMet()
    {
        if (IsRequirementsMetOverride != null)
        {
            return IsRequirementsMetOverride();
        }
        return Requirements.All(r => r.Completed);
    }
    #endregion

    #region 奖励
    /// <summary>
    /// 奖励
    /// </summary>
    public List<Reward> Rewards;

    public event Action? OnGetAllReward;
    /// <summary>
    /// 一键获得所有奖励
    /// </summary>
    /// <returns>是否全部获取</returns>
    public bool GetAllReward()
    {
        bool result = true;
        Rewards.ForEach(r => result &= r.Receive());
        OnGetAllReward?.Invoke();
        return result;
    }
    #endregion

    #region 构造函数
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="mod">所属模组, 用于查找对应资源</param>
    /// <param name="page">属于哪一页</param>
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
    public Achievement(Mod mod, AchievementPage page, string name,
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
        Mod = mod;
        Page = page;
        Name = name;
        SetPredecessorNames(predecessorNames, isPredecessorFullName);
        Requirements = requirements ?? [];
        Requirements.ForEach(r => r.OnComplete += TryComplete);
        Rewards = rewards ?? [];
        _displayName = displayName;
        _tooltip = tooltip;
        _description = description;
        _texture = texture;
        PositionOverride = defaultPosition;
        ReachedStableState = DefaultReachedStableState;
        CloseCondition = DefaultCloseCondition;
    }
    #endregion

    #region 重置与开始
    public event Action? OnReset;
    public void Reset()
    {
        State = StateEnum.Locked;
        Requirements.ForEach(r => r.Reset());
        OnReset?.Invoke();
    }
    public event Action? OnStart;
    public void Start()
    {
        Requirements.ForEach(r => r.Start());
        OnStart?.Invoke();
    }
    #endregion

    #region 状态 (完成 / 解锁)
    #region 弃用的 (还可以用, 但不建议使用)
    [Obsolete($"使用{nameof(State)}")]
    public bool Completed { get => State == StateEnum.Completed; private set { State.AssignIf(value, StateEnum.Completed); } }
    [Obsolete($"使用{nameof(State)}")]
    public bool Unlocked { get => State == StateEnum.Unlocked; private set { State.AssignIf(value, StateEnum.Unlocked); } }
    #endregion
    public enum StateEnum
    {
        Locked,
        Unlocked,
        Completed,
        Closed
    }
    public StateEnum State;
    public Func<bool> ReachedStableState;
    public bool DefaultReachedStableState()
    {
        return PredecessorCountNeeded < 0 ? State.IsClosed() : State.IsCompleted();
    }
    public event Action? OnUnlock;
    public void UnlockSafe()
    {
        if (!State.IsLocked())
        {
            return;
        }
        State = StateEnum.Unlocked;
        Requirements.ForEach(r => DoIf(r.ListenType == Requirement.ListenTypeEnum.OnUnlocked, r.BeginListenSafe));
        CheckStateChanged(StateEnum.Locked, StateEnum.Unlocked);
        OnUnlock?.Invoke();
    }
    public void TryUnlock()
    {
        if (State.IsLocked() && IsPredecessorsMet())
        {
            UnlockSafe();
        }
        
    }
    public event Action? OnComplete;
    public void CompleteSafe()
    {
        if (!State.IsUnlocked())
        {
            return;
        }
        State = StateEnum.Completed;
        // !!!!!!!! Test
        // Main.NewText($"成就{DisplayName.Value}完成!");
        Requirements.ForEach(r => r.EndListenSafe());
        CheckStateChanged(StateEnum.Unlocked, StateEnum.Completed);
        OnComplete?.Invoke();
    }
    public void TryComplete()
    {
        if (State.IsUnlocked() && IsRequirementsMet())
        {
            CompleteSafe();
        }
    }
    public event Action? OnClose;
    public void CloseSafe()
    {
        if (!State.IsCompleted())
        {
            return;
        }
        State = StateEnum.Closed;
        CheckStateChanged(StateEnum.Completed, StateEnum.Closed);
        OnClose?.Invoke();
    }
    public void TryClose()
    {
        if (State.IsCompleted() && CloseCondition())
        {
            CloseSafe();
        }
    }

    public delegate void OnCheckStateChangedDelegate(StateEnum oldState, StateEnum newState);
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
    public event OnCheckStateChangedDelegate? OnCheckStateChanged;
    public void CheckStateChanged(StateEnum oldState, StateEnum newState)
    {
        if (newState is StateEnum.Completed or StateEnum.Closed)
        {
            Successors.ForeachDo(s => s.CheckState());
        }
        OnCheckStateChanged?.Invoke(oldState, newState);
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
        Requirements.ForEach(r => r.NetSend(writer));
    }
    public void NetReceive(BinaryReader reader)
    {
        Requirements.ForEach(r => r.NetReceive(reader));
    }
    #endregion
}

public static class StateEnumExtensions
{
    public static bool IsLocked(this Achievement.StateEnum self) => self == Achievement.StateEnum.Locked;
    public static bool IsUnlocked(this Achievement.StateEnum self) => self == Achievement.StateEnum.Unlocked;
    public static bool IsCompleted(this Achievement.StateEnum self) => self == Achievement.StateEnum.Completed;
    public static bool IsClosed(this Achievement.StateEnum self) => self == Achievement.StateEnum.Closed;
}
