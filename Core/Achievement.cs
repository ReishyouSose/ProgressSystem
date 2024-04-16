using ProgressSystem.Core.StaticData;
using ProgressSystem.GameEvents;
using System;
using System.IO;
using Terraria.Localization;

namespace ProgressSystem.Core;

/// <summary>
/// <br/>成就
/// <br/>Tips: 在 UI 中修改了相关数据时需要设置 <see cref="ShouldSaveStaticData"/> 为真,
/// <br/>对于 <see cref="AchievementPage"/> 等其它实现了 <see cref="IWithStaticData"/> 接口的也是如此
/// <br/>修改的数据是它本身的数据时才设置, 如设置了条件但成就原来就存在的话
/// <br/>就不需要设置 <see cref="ShouldSaveStaticData"/>, 而只需要设置条件的就好
/// </summary>
public class Achievement : IWithStaticData
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
    public Rectangle? SourceRect => GetSourceRect();
    public Func<Rectangle?> GetSourceRect;
    public static Rectangle? DefaultGetSourceRect() => null;

    protected TextGetter _displayName;
    protected TextGetter _tooltip;
    protected TextGetter _description;
    protected Texture2DGetter _texture;

    /// <summary>
    /// 在 UI 上的位置, 当为空时 UI 上的默认排列则自动给出
    /// </summary>
    public Vector2? Position;

    #region 以条件的图片作为成就的图片
    public void SetTextureToRequirementTexture(int index)
    {
        _useRequirementTextureIndex = index;
        Texture = new(() => Requirements.GetS(index)?.Texture.Value);
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
            _useRequirementTextureIndex = value;
            if (value.HasValue)
            {
                int index = value.Value;
                Texture = new(() => Requirements.GetS(index)?.Texture.Value);
            }
        }
    }
    protected int? _useRequirementTextureIndex;
    #endregion

    /// <summary>
    /// 自定义绘制
    /// 返回 false 以取消原本的绘制
    /// </summary>
    public virtual bool PreDraw()   // TODO: 填入参数
    {
        return true;
    }
    public virtual void PostDraw() { }  // TODO: 填入参数

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

    protected List<Achievement>? predecessors;
    protected readonly List<Achievement> successors = [];
    protected List<(string Value, bool IsFullName)>? _predecessorNames;

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

    public int? RequirementCountNeeded;

    #region 提交
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
    public virtual bool GetAllReward()
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
        achievement.Position = defaultPosition;

        page.AddF(achievement);
        return achievement;
    }
    public static bool TryCreate(AchievementPage page, Mod mod, string name, out Achievement? achievement)
    {
        string fullName = string.Join('.', mod.Name, name);
        if (page.Achievements.ContainsKey(fullName))
        {
            achievement = null;
            return false;
        }
        achievement = Create(page, mod, name);
        return true;
    }

    protected Achievement()
    {
        ReachedStableState = DefaultReachedStableState;
        CloseCondition = DefaultCloseCondition;
        IsPredecessorsMet = DefaultIsPredecessorsMet;
        UnlockCondition = DefaultUnlockCondition;
        CompleteCondition = DefaultCompleteCondition;
        GetSourceRect = DefaultGetSourceRect;
    }

    public void PostInitialize()
    {
        InitializePredecessorsSafe();
        _ = DisplayName;
        _ = Tooltip;
        _ = Description;
        _ = Texture;
    }

    public virtual IEnumerable<ConstructInfoTable<Achievement>> GetConstructInfoTables()
    {
        yield return ConstructInfoTable<Achievement>.Create(Create);
    }
    #endregion

    #region 重置与开始
    public static event Action<Achievement>? OnResetStatic;
    public event Action? OnReset;
    public virtual void Reset()
    {
        State = StateEnum.Locked;
        Requirements.ForeachDo(r => r.Reset());
        OnResetStatic?.Invoke(this);
        OnReset?.Invoke();
    }
    public static event Action<Achievement>? OnStartStatic;
    public event Action? OnStart;
    public virtual void Start()
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
    public StateEnum State { get; protected set; }
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
        if (State.IsLocked() && IsPredecessorsMet())
        {
            UnlockSafe();
        }
        
    }

    public Func<bool> CompleteCondition;
    public bool DefaultCompleteCondition() => (!NeedSubmit || InSubmitting) && Requirements.All(r => r.Completed);
    public static event Action<Achievement>? OnCompleteStatic;
    public event Action? OnComplete;
    public virtual void CompleteSafe()
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
    public virtual void TryComplete()
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
    #endregion

    #region 数据存取
    // TODO: 成就本身与奖励相关的数据存取
    public virtual void SaveDataInWorld(TagCompound tag)
    {
        tag.SaveListData("Requirements", Requirements, (r, t) => r.SaveDataInWorld(t));
    }
    public virtual void LoadDataInWorld(TagCompound tag)
    {
        tag.LoadListData("Requirements", Requirements, (r, t) => r.LoadDataInWorld(t));
    }
    public virtual void SaveDataInPlayer(TagCompound tag)
    {
        tag.SetWithDefault("State", State.ToString(), StateEnum.Locked.ToString());
        tag.SaveListData("Requirements", Requirements, (r, t) => r.SaveDataInPlayer(t));
    }
    public virtual void LoadDataInPlayer(TagCompound tag)
    {
        if (Enum.TryParse(tag.GetWithDefault("State", StateEnum.Locked.ToString()), out StateEnum state))
        {
            State = state;
        }
        tag.LoadListData("Requirements", Requirements, (r, t) => r.LoadDataInPlayer(t));
    }
    public bool ShouldSaveStaticData { get; set; }
    public virtual void SaveStaticData(TagCompound tag)
    {
        this.SaveStaticDataListTemplate(Requirements, "Requirements", tag, (a, t) =>
        {
            tag.SetWithDefault("DisplayNameKey", DisplayName.LocalizedTextValue?.Key);
            tag.SetWithDefault("DisplayName", DisplayName.StringValue);
            tag.SetWithDefault("TooltipKey", Tooltip.LocalizedTextValue?.Key);
            tag.SetWithDefault("Tooltip", Tooltip.StringValue);
            tag.SetWithDefault("Texture", Texture.AssetPath);
            tag.SetWithDefault("Position", Position ?? Vector2.Zero);
            tag.SetWithDefaultN("UseRequirementTextureIndex", UseRequirementTextureIndex);
            tag.SetWithDefaultN("RequirementCountNeeded", RequirementCountNeeded);
            tag.SetWithDefaultN("PredecessorCountNeeded", PredecessorCountNeeded);
            tag.SetWithDefault("NeedSubmit", NeedSubmit);
        });
    }
    public virtual void LoadStaticData(TagCompound tag)
    {
        this.LoadStaticDataListTemplate(Requirements.GetS, Requirements!.SetFS, "Requirements", tag, (a, t) =>
        {
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
            Texture = tag.GetWithDefault<string>("Texture");
            Position = tag.GetWithDefault<Vector2>("Position");
            UseRequirementTextureIndex = tag.GetWithDefaultN<int>("UseRequirementTextureIndex");
            RequirementCountNeeded = tag.GetWithDefaultN<int>("RequirementCountNeeded");
            PredecessorCountNeeded = tag.GetWithDefaultN<int>("PredecessorCountNeeded");
            tag.GetWithDefault("NeedSubmit", out NeedSubmit);
            Requirements.Clear();
        });
    }
    #endregion

    #region 网络同步
    // todo: 成就本身与奖励相关的网络同步
    public virtual void NetSend(BinaryWriter writer)
    {
        Requirements.ForeachDo(r => r.NetSend(writer));
    }
    public virtual void NetReceive(BinaryReader reader)
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
