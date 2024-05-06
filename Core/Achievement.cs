using Microsoft.Xna.Framework.Graphics;
using ProgressSystem.Common.Configs;
using ProgressSystem.Core.Interfaces;
using ProgressSystem.Core.NetUpdate;
using ProgressSystem.Core.Requirements;
using ProgressSystem.Core.Rewards;
using ProgressSystem.Core.StaticData;
using System.IO;

namespace ProgressSystem.Core;

/// <summary>
/// <br/>成就
/// <br/>Tips: 在 UI 中修改了相关数据时需要设置 <see cref="ShouldSaveStaticData"/> 为真,
/// <br/>对于 <see cref="AchievementPage"/> 等其它实现了 <see cref="IWithStaticData"/> 接口的也是如此
/// <br/>修改的数据是它本身的数据时才设置, 如设置了条件但成就原来就存在的话
/// <br/>就不需要设置 <see cref="ShouldSaveStaticData"/>, 而只需要设置条件的就好
/// </summary>
public class Achievement : IWithStaticData, INetUpdate, IProgressable, IAchievementNode
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
    /// <summary>
    /// 本地化路径, 默认 [page.FullName].[achievement.Name]
    /// </summary>
    public string LocalizedKey
    {
        get => _localizedKey ??= string.Join('.', Page.FullName, Name);
        set => _localizedKey = value;
    }
    protected string? _localizedKey;

    /// <summary>
    /// 图片默认路径, 默认 [page.Mod.Name]/[page.Name]/[achievement.Name]
    /// </summary>
    public string TexturePath
    {
        get => _texturePath ??= string.Join('/', Page.Mod.Name, Page.Name, Name);
        set => _texturePath = value;
    }
    protected string? _texturePath;
    #endregion

    #endregion

    #region 给 UI 提供的
    /// <summary>
    /// 显示的名字, 默认通过对应 Mod 的 Achievements.[LocalizedKey].DisplayName 获取
    /// </summary>
    public TextGetter DisplayName
    {
        get => !_displayName.IsNone ? _displayName : _displayName = Mod.GetLocalization($"Achievements.{LocalizedKey}.DisplayName");
        set => _displayName = value;
    }
    /// <summary>
    /// 鼠标移上去时显示的提示, 默认通过对应 Mod 的 Achievements.[LocalizedKey].Tooltip 获取
    /// </summary>
    public TextGetter Tooltip
    {
        get => !_tooltip.IsNone ? _tooltip : _tooltip = Mod.GetLocalization($"Achievements.{LocalizedKey}.Tooltip");
        set => _tooltip = value;
    }
    /// <summary>
    /// 详细说明, 默认通过对应 Mod 的 Achievements.[LocalizedKey].Description 获取
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
        get
        {
            if (!_texture.IsNone)
            {
                return _texture;
            }
            _texture = $"{Mod.Name}/Assets/Textures/Achievements/{TexturePath}";
            return !_texture.IsNone ? _texture : (_texture = $"{Mod.Name}/Assets/Textures/Achievements/Default");
        }

        set => _texture = value;
    }
    public Rectangle? SourceRect => GetSourceRect?.Invoke() ?? null;
    public Func<Rectangle?>? GetSourceRect;
    public static Rectangle? DefaultGetSourceRect() => null;

    protected TextGetter _displayName;
    protected TextGetter _tooltip;
    protected TextGetter _description;
    protected Texture2DGetter _texture;

    /// <summary>
    /// 在 UI 上的位置, 当为空时 UI 上的默认排列则自动给出
    /// </summary>
    public Vector2? Position;

    #region Visible
    protected bool _visible = true;
    public bool Visible
    {
        get => _visible;
        set
        {
            if (_visible == value)
            {
                return;
            }
            _visible = value;
            OnVisibleChangedStatic?.Invoke(this);
            OnVisibleChanged?.Invoke();
        }
    }
    public static event Action<Achievement>? OnVisibleChangedStatic;
    /// <summary>
    /// 在此成就的可见性改变时被调用
    /// 通过 <see cref="Visible"/> 获得改变后的可见性
    /// </summary>
    public event Action? OnVisibleChanged;

    public enum VisibleTypeEnum
    {
        /// <summary>
        /// 自定义, 需要重写 <see cref="UpdateVisible"/> 和 <see cref="UpdateVisibleHook"/> 以定义何时可见
        /// </summary>
        Customed = -1,
        /// <summary>
        /// 几乎总是可见, 除非处于 <see cref="StateEnum.Disabled"/> 状态
        /// </summary>
        WhenEnabled,
        /// <summary>
        /// 在任意前置解锁后可见 (若没有前置则总是可见)
        /// </summary>
        WhenAnyPredecessorUnlocked,
        /// <summary>
        /// 在解锁后 (前置完成后) 可见
        /// </summary>
        WhenUnlocked,
        WhenCompleted,
        /// <summary>
        /// 在任意时候可见, 即使在 <see cref="StateEnum.Disabled"/> 状态
        /// </summary>
        Always
    }
    public VisibleTypeEnum VisibleType { get; private init; }

    protected virtual void UpdateVisible()
    {
        switch (VisibleType)
        {
        case VisibleTypeEnum.WhenEnabled:
            Visible = State != StateEnum.Disabled;
            break;
        case VisibleTypeEnum.WhenAnyPredecessorUnlocked:
            Visible = Predecessors.Count == 0 || Predecessors.Any(p => p.State >= StateEnum.Unlocked);
            break;
        case VisibleTypeEnum.WhenUnlocked:
            Visible = State >= StateEnum.Unlocked;
            break;
        case VisibleTypeEnum.WhenCompleted:
            Visible = State >= StateEnum.Completed;
            break;
        case VisibleTypeEnum.Always:
            Visible = true;
            break;
        }
    }
    protected virtual void UpdateVisibleHook()
    {
        OnStart += UpdateVisible;

        if (VisibleType != VisibleTypeEnum.Always)
        {
            OnDisable += UpdateVisible;
        }

        switch (VisibleType)
        {
        case VisibleTypeEnum.WhenAnyPredecessorUnlocked:
            foreach (var predecessor in Predecessors)
            {
                predecessor.OnUnlock += UpdateVisible;
            }
            break;
        case VisibleTypeEnum.WhenUnlocked:
            OnUnlock += UpdateVisible;
            break;
        case VisibleTypeEnum.WhenCompleted:
            OnComplete += UpdateVisible;
            break;
        }
    }
    #endregion

    #region 以条件的图片作为成就的图片

    #region 以特定的条件的图片作为成就的图片
    public void SetTextureToRequirementTexture(int? index)
    {
        _useRequirementTextureIndex = index;
        Texture = index.HasValue ?
            new(() => Requirements.GetS(index.Value)?.Texture.Value) :
            Texture2DGetter.Default;
        GetSourceRect = index.HasValue ? () => Requirements.GetS(index.Value)?.SourceRect : null;
    }
    public int? UseRequirementTextureIndex
    {
        get => _useRequirementTextureIndex;
        set
        {
            if (_useRequirementTextureIndex == value)
            {
                return;
            }
            SetTextureToRequirementTexture(value);
        }
    }
    protected int? _useRequirementTextureIndex;
    #endregion

    #region 滚动显示条件的图片
    /// <summary>
    /// 让图片为轮流显示条件的图片
    /// </summary>
    /// <param name="rollTime">显示下一个条件图片的时间间隔, 单位为帧</param>
    public void SetTextureToRollingRequirementTexture(int? rollTime = 60)
    {
        _useRequirementTextureRollTime = rollTime;
        Texture = rollTime.HasValue ?
            new(() => Requirements.Count <= 0 ?
                null : Requirements[
                    Modular(AchievementManager.GeneralTimer, Requirements.Count * rollTime.Value) / rollTime.Value
                ].Texture.Value
            ) : Texture2DGetter.Default;
        GetSourceRect = rollTime.HasValue ?
            () => Requirements.Count <= 0 ?
                null : Requirements[
                    Modular(AchievementManager.GeneralTimer, Requirements.Count * rollTime.Value) / rollTime.Value
                ].SourceRect
            : null;

    }
    public bool UseRollingRequirementTexture
    {
        get => _useRequirementTextureRollTime.HasValue;
        set
        {
            int? valueToSet = value ? 60 : null;
            if (valueToSet == _useRequirementTextureRollTime)
            {
                return;
            }
            SetTextureToRollingRequirementTexture(valueToSet);
        }
    }
    public int? UseRequirementTextureRollTime
    {
        get => _useRequirementTextureRollTime;
        set
        {
            if (value == _useRequirementTextureRollTime)
            {
                return;
            }
            SetTextureToRollingRequirementTexture(value);
        }
    }
    protected int? _useRequirementTextureRollTime;
    #endregion

    #endregion

    #region 自定义绘制

    #region 绘制图标
    public delegate bool PreDrawDelegate(SpriteBatch sb, Rectangle slotRectangle);
    public delegate void PostDrawDelegate(SpriteBatch sb, Rectangle slotRectangle);
    /// <summary>
    /// 自定义绘制图标
    /// 返回 false 以取消原本的绘制
    /// </summary>
    public PreDrawDelegate? PreDraw;
    public PostDrawDelegate? PostDraw;
    #endregion

    #region 绘制连线
    public delegate bool PreDrawLineDelegate(SpriteBatch sb, Rectangle startRectangle, Rectangle endRectangle, Achievement predecessor);
    public delegate void PostDrawLineDelegate(SpriteBatch sb, Rectangle startRectangle, Rectangle endRectangle, Achievement predecessor);
    /// <summary>
    /// 自定义此成就与前置成就的连线绘制
    /// 返回 false 以取消原本的绘制
    /// </summary>
    public PreDrawLineDelegate? PreDrawLine;
    public PostDrawLineDelegate? PostDrawLine;
    #endregion

    #endregion

    #endregion

    #region 前后置相关
    /// <summary>
    /// 前置(都在同一页)
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
    /// 后置(都在同一页)
    /// </summary>
    public IReadOnlyList<Achievement> Successors => successors;

    /// <summary>
    /// 设置所有的前置名(覆盖)
    /// </summary>
    /// <returns>自身</returns>
    public Achievement SetPredecessorNames(List<string>? predecessorNames, bool isFullName = false)
    {
        predecessors?.ForEach(p => p.successors.Remove(this));
        predecessors = null;
        _predecessorNames = predecessorNames == null ? [] : [.. predecessorNames.Select(p => (p, isFullName))];
        return this;
    }
    /// <summary>
    /// 添加一个前置
    /// </summary>
    /// <returns>自身</returns>
    public Achievement AddPredecessor(string predecessorName, bool isFullName)
    {
        if (predecessors == null)
        {
            (_predecessorNames ??= []).Add((predecessorName, isFullName));
            return this;
        }
        Achievement? predecessor = isFullName ? Page.GetAchievement(predecessorName) : Page.GetAchievementByName(predecessorName);
        if (predecessor == null)
        {
            return this;
        }
        predecessor.successors.Add(this);
        predecessors.Add(predecessor);
        return this;
    }
    /// <summary>
    /// 移除一个前置
    /// </summary>
    /// <returns>自身</returns>
    public Achievement RemovePredecessor(string predecessorName, bool isFullName)
    {
        if (predecessors == null)
        {
            if (_predecessorNames == null)
            {
                return this;
            }
            _predecessorNames.Remove((predecessorName, isFullName));
            return this;
        }
        foreach (int i in predecessors.Count)
        {
            Achievement predecessor = predecessors[i];
            if (predecessor.Name != predecessorName)
            {
                continue;
            }
            predecessor.successors.Remove(this);
            predecessors.RemoveAt(i);
            break;
        }
        predecessors.Remove(isFullName ? a => a.FullName == predecessorName : a => a.Name == predecessorName);
        return this;
    }
    protected void InitializePredecessorsSafe()
    {
        if (predecessors == null)
        {
            predecessors = [];
            _predecessorNames?.ForEach(p => AddPredecessor(p.Value, p.IsFullName));
        }
    }

    protected List<Achievement>? predecessors;
    protected readonly List<Achievement> successors = [];
    protected List<(string Value, bool IsFullName)>? _predecessorNames;

    /// <summary>
    /// <br/>需要多少个前置才能开始此成就
    /// <br/>默认 <see langword="null"/> 代表需要所有前置完成
    /// <br/>如果此值大于前置数, 那么以前置数为准
    /// <br/>1 代表只需要任意前置完成即可
    /// <br/>0 代表实际上不需要前置完成, 前置只是起提示作用
    /// <br/>负数(-n)代表有 n 个前置完成时此成就封闭, 不可再完成
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
    public RequirementList Requirements { get; protected init; }

    protected int _requirementCountNeeded;
    /// <summary>
    /// <br/>需要多少个条件才能完成此成就
    /// <br/>默认 0 代表需要所有条件完成
    /// <br/>如果此值大于条件数, 那么以条件数为准
    /// <br/>例如 1 代表只需要任意条件完成即可
    /// </summary>
    public int RequirementCountNeeded
    {
        get => _requirementCountNeeded;
        set => _requirementCountNeeded = value.WithMin(0);
    }

    #region 提交
    /// <summary>
    /// 是否需要在 UI 中提交才算做达成
    /// </summary>
    public bool NeedSubmit;

    public void Submit()
    {
        if (State != StateEnum.Unlocked)
        {
            return;
        }
        InSubmitting = true;
        TryComplete();
        InSubmitting = false;
    }
    protected bool InSubmitting;
    #endregion

    public Achievement SetRequirements(IEnumerable<Requirement> requirements)
    {
        Requirements.Clear();
        Requirements.AddRange(requirements);
        return this;
    }
    public Achievement AddRequirements(IEnumerable<Requirement> requirements)
    {
        Requirements.AddRange(requirements);
        return this;
    }

    #endregion

    #region 奖励
    /// <summary>
    /// 奖励
    /// </summary>
    public RewardList Rewards { get; protected init; }

    /// <summary>
    /// 是否全部领取
    /// </summary>
    public bool AllRewardsReceived { get; protected set; }
    protected void UpdateAllRewardsReceived()
    {
        AllRewardsReceived = Rewards.All(r => r.State >= Reward.StateEnum.Received || r.State == Reward.StateEnum.Disabled);
    }

    public void TryReceiveAllReward()
    {
        if (!AllRewardsReceived && State.IsCompleted())
        {
            ReceiveAllRewardSafe();
            UpdateAllRewardsReceived();
        }
    }
    /// <summary>
    /// 一键获得所有奖励
    /// </summary>
    /// <returns>是否全部获取</returns>
    protected void ReceiveAllRewardSafe()
    {
        foreach (var reward in Rewards)
        {
            reward.ReceiveSafe();
        }
    }

    public Achievement SetRewards(IEnumerable<Reward> rewards)
    {
        Rewards.Clear();
        Rewards.AddRange(rewards);
        return this;
    }
    public Achievement AddRewards(IEnumerable<Reward> rewards)
    {
        Rewards.AddRange(rewards);
        return this;
    }
    #endregion

    #region 初始化
    static Achievement()
    {
        // 在成就页解锁时尝试解锁成就
        AchievementPage.OnUnlockStatic += p => p.Achievements.Values.ForeachDo(a => a.TryUnlock());
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
        // 在完成时更新进度
        OnCompleteStatic += a => a.UpdateProgress();

        // 在完成时处理条件和奖励
        OnCompleteStatic += a =>
        {
            foreach (var requirement in a.Requirements)
            {
                requirement.CloseSafe();
            }
            foreach (var reward in a.Rewards)
            {
                reward.TryUnlock();
            }
            if (ClientConfig.Instance.AutoReceive)
            {
                a.TryReceiveAllReward();
            }
        };

        // 在奖励有变化时检查是否全部领取
        static void UpdateRewardAchievementAllRewardsReceived(Reward r) => r.Achievement.UpdateAllRewardsReceived();
        Reward.OnTotallyReceivedStatic += UpdateRewardAchievementAllRewardsReceived;
        Reward.OnCloseStatic += UpdateRewardAchievementAllRewardsReceived;
        Reward.OnDisableStatic += UpdateRewardAchievementAllRewardsReceived;
    }

    /// <summary>
    /// <br/>创建一个成就, 需要通过 page.Add (或其变体) 将其加入到页面中
    /// <para/>其它一些初始化设置:
    /// <para/><see cref="SetPredecessorNames(List{string}?, bool)"/>: 设置前置
    /// <para/><see cref="NeedSubmit"/>: 是否需要在 UI 中提交才算作达成
    /// <br/><see cref="PredecessorCountNeeded"/>: 需要多少个前置才能开始此成就
    /// <br/><see cref="RequirementCountNeeded"/>: 需要多少个条件才能完成此成就
    /// <br/><see cref="Repeatable"/>: 是否可重复完成
    /// <br/><see cref="VisibleType"/>: 可见类型, 在什么时候可见
    /// <para/><see cref="TexturePath"/>: 图片的默认路径, 默认 [page.Mod.Name]/[page.Name]/[achievement.Name]
    /// <br/><see cref="Texture"/>: 图片, 默认从 [Mod]/Assets/Textures/Achievements/[TexturePath] 获取
    /// <br/><see cref="GetSourceRect"/>: 获取 SourceRect, 默认 null 代表全图
    /// <br/><see cref="DisplayName"/>: 显示的名字, 默认通过对应 Mod 的 Achievements.[LocalizedKey].DisplayName 获取
    /// <br/><see cref="Tooltip"/>: 鼠标移上去时显示的提示, 默认通过对应 Mod 的 Achievements.[LocalizedKey].Tooltip 获取
    /// <br/><see cref="Description"/>: 详细说明, 默认通过对应 Mod 的 Achievements.[LocalizedKey].Description 获取
    /// <br/><see cref="Position"/>: 默认位置
    /// <para/><see cref="IsPredecessorsMet"/>: 自定义前置检索条件
    /// <br/><see cref="UnlockCondition"/>: 自定义解锁条件
    /// <br/><see cref="CompleteCondition"/>: 自定义完成条件
    /// <br/><see cref="CloseCondition"/>: 自定义关闭条件
    /// <br/><see cref="ReachedStableState"/>: 自定义是否达到稳定状态
    /// </summary>
    /// <param name="page">属于哪一页</param>
    /// <param name="mod">所属模组, 用于查找对应资源, 注意不是 <paramref name="page"/> 的模组, 而是添加此成就的模组</param>
    /// <param name="name">内部名, 同一成就页内不允许有相同模组与内部名的成就</param>
    /// <param name="requirements">条件</param>
    /// <param name="rewards">奖励</param>
    public Achievement(AchievementPage page, Mod mod, string name, IEnumerable<Requirement>? requirements = null, IEnumerable<Reward>? rewards = null) : this(requirements, rewards)
    {
        Page = page;
        Mod = mod;
        Name = name;
    }
    protected Achievement(IEnumerable<Requirement>? requirements = null, IEnumerable<Reward>? rewards = null)
    {
        ReachedStableState = DefaultReachedStableState;
        CloseCondition = DefaultCloseCondition;
        IsPredecessorsMet = DefaultIsPredecessorsMet;
        UnlockCondition = DefaultUnlockCondition;
        CompleteCondition = DefaultCompleteCondition;
        GetSourceRect = DefaultGetSourceRect;
        WouldNetUpdate = WouldNetUpdateInitial;
        Requirements = new(this, requirements);
        Rewards = new(this, rewards);
    }
    protected Achievement() : this(null, null) { }

    public void PostInitialize()
    {
        _ = Predecessors;
        _ = DisplayName;
        _ = Tooltip;
        _ = Description;
        _ = Texture;
        UpdateVisibleHook();
    }

    public virtual IEnumerable<ConstructInfoTable<Achievement>> GetConstructInfoTables()
    {
        ConstructInfoTable<Achievement>.TryAutoCreate<Achievement>(GetType(), null, out var constructors);
        return constructors;
    }
    #endregion

    #region 重置与开始
    public IEnumerable<IAchievementNode> NodeChildren => Requirements.Concat<IAchievementNode>(Rewards);
    public static event Action<Achievement>? OnResetStatic;
    public event Action? OnReset;
    public virtual void Reset()
    {
        State = StateEnum.Locked;
        AllRewardsReceived = false;
        OnResetStatic?.Invoke(this);
        OnReset?.Invoke();
    }
    public static event Action<Achievement>? OnStartStatic;
    public event Action? OnStart;
    public virtual void Start()
    {
        CheckState();
        UpdateAllRewardsReceived();
        OnStartStatic?.Invoke(this);
        OnStart?.Invoke();
    }
    #endregion

    #region 状态
    public enum StateEnum
    {
        Disabled = -1,
        Locked = 0,
        Unlocked = 1,
        Completed = 2,
        Closed = 3
    }
    public StateEnum State { get; protected set; }
    public Func<bool> ReachedStableState;
    public bool DefaultReachedStableState()
    {
        return PredecessorCountNeeded < 0 ? State.IsClosed() : State.IsCompleted();
    }
    public virtual void CheckState()
    {
        if (ReachedStableState())
        {
            return;
        }
        TryUnlock();
        TryComplete();
        TryClose();
    }

    #region 解锁
    public Func<bool> UnlockCondition;
    public bool DefaultUnlockCondition()
    {
        return Page.State is AchievementPage.StateEnum.Unlocked or AchievementPage.StateEnum.Completed && State.IsLocked() && IsPredecessorsMet();
    }
    public static event Action<Achievement>? OnUnlockStatic;
    public event Action? OnUnlock;
    public virtual void UnlockSafe()
    {
        if (!State.IsLocked())
        {
            return;
        }
        State = StateEnum.Unlocked;
        OnUnlockStatic?.Invoke(this);
        OnUnlock?.Invoke();
    }
    public virtual void TryUnlock()
    {
        if (UnlockCondition())
        {
            UnlockSafe();
        }

    }
    #endregion

    #region 完成
    public Func<bool> CompleteCondition;
    public bool DefaultCompleteCondition()
    {
        return (!NeedSubmit || InSubmitting) &&
            (RequirementCountNeeded == 0 || RequirementCountNeeded >= Requirements.Count(r => r.State != Requirement.StateEnum.Disabled) ?
            Requirements.All(r => r.State is Requirement.StateEnum.Completed or Requirement.StateEnum.Disabled) :
            Requirements.Sum(r => (r.State == Requirement.StateEnum.Completed).ToInt()) >= RequirementCountNeeded);
    }

    public static event Action<Achievement>? OnCompleteStatic;
    public event Action? OnComplete;
    public virtual void CompleteSafe()
    {
        if (!State.IsUnlocked())
        {
            return;
        }
        State = StateEnum.Completed;
        NetHandler.TryShowPlayerCompleteMessage(this);
        OnCompleteStatic?.Invoke(this);
        OnComplete?.Invoke();
    }
    public virtual void TryComplete()
    {
        if (State.IsUnlocked() && CompleteCondition())
        {
            CompleteSafe();
        }
    }
    #endregion

    #region 关闭
    public Func<bool> CloseCondition;
    public bool DefaultCloseCondition()
    {
        return PredecessorCountNeeded < 0 && Predecessors.Sum(p => p.State.IsCompleted().ToInt()) >= -PredecessorCountNeeded;
    }
    public static event Action<Achievement>? OnCloseStatic;
    public event Action? OnClose;
    public virtual void CloseSafe()
    {
        if (!State.IsCompleted())
        {
            return;
        }
        State = StateEnum.Closed;
        OnCloseStatic?.Invoke(this);
        OnClose?.Invoke();
    }
    public virtual void TryClose()
    {
        if (State.IsCompleted() && CloseCondition())
        {
            CloseSafe();
        }
    }
    #endregion

    #region 重复
    /// <summary>
    /// 是否可重复完成
    /// </summary>
    public virtual bool Repeatable { get; set; }
    public virtual void TryRepeat()
    {
        if (!Repeatable && State != StateEnum.Completed)
        {
            return;
        }
        RepeatSafe();
    }
    public bool InRepeat;
    public virtual void RepeatSafe()
    {
        InRepeat = true;
        ((IAchievementNode)this).ResetTree();
        ((IAchievementNode)this).StartTree();
        InRepeat = false;
    }
    #endregion

    #region 禁用
    public static event Action<Achievement>? OnDisableStatic;
    public event Action? OnDisable;
    public void Disable()
    {
        State = StateEnum.Disabled;
        OnDisableStatic?.Invoke(this);
        OnDisable?.Invoke();
    }
    #endregion

    #endregion

    #region 数据存取
    public virtual void SaveDataInWorld(TagCompound tag)
    {
        tag.SaveListData("Requirements", Requirements, (r, t) => r.SaveDataInWorld(t));
        tag.SaveListData("Rewards", Rewards, (r, t) => r.SaveDataInWorld(t));
    }
    public virtual void LoadDataInWorld(TagCompound tag)
    {
        tag.LoadListData("Requirements", Requirements, (r, t) => r.LoadDataInWorld(t));
        tag.LoadListData("Rewards", Rewards, (r, t) => r.LoadDataInWorld(t));
    }
    public virtual void SaveDataInPlayer(TagCompound tag)
    {
        tag.SetWithDefault("State", State.ToString(), StateEnum.Locked.ToString());
        tag.SaveListData("Requirements", Requirements, (r, t) => r.SaveDataInPlayer(t));
        tag.SaveListData("Rewards", Rewards, (r, t) => r.SaveDataInPlayer(t));
    }
    public virtual void LoadDataInPlayer(TagCompound tag)
    {
        if (Enum.TryParse(tag.GetWithDefault("State", StateEnum.Locked.ToString()), out StateEnum state))
        {
            State = state;
        }
        tag.LoadListData("Requirements", Requirements, (r, t) => r.LoadDataInPlayer(t));
        tag.LoadListData("Rewards", Rewards, (r, t) => r.LoadDataInPlayer(t));
    }
    public bool ShouldSaveStaticData { get; set; }
    public virtual void SaveStaticData(TagCompound tag)
    {
        this.SaveStaticDataListTemplate(Requirements, "Requirements", tag, (a, t) =>
        {
            /*
            tag.SetWithDefault("DisplayNameKey", DisplayName.LocalizedTextValue?.Key);
            tag.SetWithDefault("DisplayName", DisplayName.StringValue);
            tag.SetWithDefault("TooltipKey", Tooltip.LocalizedTextValue?.Key);
            tag.SetWithDefault("Tooltip", Tooltip.StringValue);
            tag.SetWithDefault("Texture", Texture.AssetPath);
            tag.SetWithDefaultN("UseRequirementTextureIndex", UseRequirementTextureIndex);
            tag.SetWithDefaultN("UseRequirementTextureRollTime", UseRequirementTextureRollTime);
            tag.SetWithDefault("Repeatable", Repeatable);
            */
            tag.SetWithDefault("RequirementCountNeeded", RequirementCountNeeded);
            tag.SetWithDefaultN("PredecessorCountNeeded", PredecessorCountNeeded);
            tag.SetWithDefault("NeedSubmit", NeedSubmit);
            if (Predecessors.Any())
            {
                tag["Predecessors"] = Predecessors.Select(p => p.FullName).ToList();
            }
        });
        tag.SetWithDefaultN("Position", Position);
        this.SaveStaticDataListTemplate(Rewards, "Rewards", tag);
    }
    public virtual void LoadStaticData(TagCompound tag)
    {
        this.LoadStaticDataListTemplate(Requirements.GetS, Requirements!.SetFSF, "Requirements", tag, (a, t) =>
        {
            /*
            if (tag.TryGet("DisplayNameKey", out string displayNameKey))
            {
                DisplayName = Language.GetText(displayNameKey);
            }
            else if (tag.TryGet("DisplayName", out string displayName))
            {
                DisplayName = displayName;
            }
            if (tag.TryGet("TooltipKey", out string tooltipKey))
            {
                Tooltip = Language.GetText(tooltipKey);
            }
            else if (tag.TryGet("Tooltip", out string tooltip))
            {
                Tooltip = tooltip;
            }
            if (tag.TryGet("DescriptionKey", out string descriptionKey))
            {
                Description = Language.GetText(descriptionKey);
            }
            else if (tag.TryGet("Description", out string description))
            {
                Description = description;
            }
            else if (tag.TryGet("Texture", out string texture))
            {
                Texture = texture;
            }
            UseRequirementTextureIndex = tag.GetWithDefaultN<int>("UseRequirementTextureIndex");
            UseRequirementTextureRollTime = tag.GetWithDefaultN<int>("UseRequirementTextureRollTime");
            Repeatable = tag.GetWithDefault<bool>("Repeatable");
            */
            RequirementCountNeeded = tag.GetWithDefault<int>("RequirementCountNeeded");
            PredecessorCountNeeded = tag.GetWithDefaultN<int>("PredecessorCountNeeded");
            tag.GetWithDefault("NeedSubmit", out NeedSubmit);
            if (tag.TryGet("Predecessors", out List<string> predecessorNames))
            {
                SetPredecessorNames(predecessorNames, true);
            }
        });
        if (tag.TryGet<Vector2>("Position", out var position))
        {
            Position = position;
        }
        this.LoadStaticDataListTemplate(Rewards.GetS, Rewards!.SetFSF, "Rewards", tag);
    }
    #endregion

    #region 网络同步
    protected bool _netUpdate;
    public bool NetUpdate { get => _netUpdate; set => DoIf(_netUpdate = value, AchievementManager.SetNeedNetUpdate); }
    public IEnumerable<INetUpdate> GetNetUpdateChildren() => Requirements.Concat<INetUpdate>(Rewards);
    public virtual void WriteMessageFromServer(BinaryWriter writer, BitWriter bitWriter) { }
    public virtual void ReceiveMessageFromServer(BinaryReader reader, BitReader bitReader) { }
    public virtual void WriteMessageFromClient(BinaryWriter writer, BitWriter bitWriter) { }
    public virtual void ReceiveMessageFromClient(BinaryReader reader, BitReader bitReader) { }

    public bool WouldNetUpdate { get; set; }
    public virtual bool WouldNetUpdateInitial => false;
    public virtual void WriteMessageToEnteringPlayer(BinaryWriter writer, BitWriter bitWriter) => WriteMessageFromServer(writer, bitWriter);
    public virtual void ReceiveMessageToEnteringPlayer(BinaryReader reader, BitReader bitReader) => ReceiveMessageFromServer(reader, bitReader);
    public virtual void WriteMessageFromEnteringPlayer(BinaryWriter writer, BitWriter bitWriter) => WriteMessageFromClient(writer, bitWriter);
    public virtual void ReceiveMessageFromEnteringPlayer(BinaryReader reader, BitReader bitReader) => ReceiveMessageFromClient(reader, bitReader);
    #endregion

    #region 进度
    public float Progress { get; protected set; }
    public float ProgressWeight { get; set; } = 1f;
    IEnumerable<IProgressable> IProgressable.ProgressChildren => Requirements;
    public virtual void UpdateProgress()
    {
        if (State is StateEnum.Completed or StateEnum.Closed)
        {
            if (Progress < 1)
            {
                Progress = 1;
                Page.UpdateProgress();
            }
            return;
        }
        Progress = ((IProgressable)this).GetProgressOfChildren();
        Page.UpdateProgress();
    }
    #endregion

    public override string ToString()
    {
        string requirementsString = string.Join(",\n    ", Requirements.Select(r => r.ToString()));
        string rewardsString = string.Join(",\n    ", Rewards.Select(r => r.ToString()));
        string predecessorsString = string.Join(",\n    ", Predecessors.Select(r => r.FullName));
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
    public static bool IsDisabled(this Achievement.StateEnum self) => self == Achievement.StateEnum.Disabled;
    public static bool IsLocked(this Achievement.StateEnum self) => self == Achievement.StateEnum.Locked;
    public static bool IsUnlocked(this Achievement.StateEnum self) => self == Achievement.StateEnum.Unlocked;
    public static bool IsCompleted(this Achievement.StateEnum self) => self == Achievement.StateEnum.Completed;
    public static bool IsClosed(this Achievement.StateEnum self) => self == Achievement.StateEnum.Closed;
}
