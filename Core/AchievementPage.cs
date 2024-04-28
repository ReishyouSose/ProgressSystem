using ProgressSystem.Core.Interfaces;
using ProgressSystem.Core.NetUpdate;
using ProgressSystem.Core.StaticData;
using System.Collections;

namespace ProgressSystem.Core;

/// <summary>
/// 成就页
/// 代表一个显示多个成就的界面
/// </summary>
public class AchievementPage : ICollection<Achievement>, IWithStaticData, INetUpdate, IProgressable, IAchievementNode
{
    #region Vars
    public Mod Mod = null!;

    /// <summary>
    /// 此成就页的内部名
    /// </summary>
    public string Name = null!;

    /// <summary>
    /// 全名, 全局唯一, 可作为标识符
    /// </summary>
    public string FullName => string.Join('.', Mod.Name, Name);

    /// <summary>
    /// 成就页包含的所有成就<br/>
    /// 键为<see cref="Achievement.FullName"/>
    /// </summary>
    public IReadOnlyDictionary<string, Achievement> Achievements => achievements;
    private readonly Dictionary<string, Achievement> achievements = [];

    /// <summary>
    /// UI 面板上是否可编辑
    /// 用于 modder 编辑页内成就位置
    /// 发布时请确保此值为假
    /// </summary>
    public bool Editable;
    #endregion

    #region 重置与开始
    public IEnumerable<IAchievementNode> NodeChildren => Achievements.Values;
    public static event Action<AchievementPage>? OnResetStatic;
    public event Action? OnReset;
    public virtual void Reset()
    {
        OnResetStatic?.Invoke(this);
        OnReset?.Invoke();
    }
    public static event Action<AchievementPage>? OnStartStatic;
    public event Action? OnStart;
    public virtual void Start()
    {
        CheckState();
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
        Completed = 2
    }
    public StateEnum State { get; protected set; }

    public virtual void CheckState()
    {
        if (State == StateEnum.Completed)
        {
            return;
        }
        TryUnlock();
        TryComplete();
    }

    #region 解锁条件
    /// <summary>
    /// <br/>如果想自定义解锁条件则修改这个并调用<see cref="InitializeStateToUnlock"/>
    /// <br/>并且在合适的地方挂<see cref="TryUnlock"/> 或 <see cref="UnlockSafe"/> 的钩子
    /// <br/>最好再在 <see cref="OnUnlock"/> 中卸掉钩子
    /// <br/>可以看看 <see cref="SetPredecessorComplete"/> 等方法作为示例, 也可直接调用
    /// </summary>
    public Func<bool> UnlockCondition;
    public static bool DefaultUnlockCondition() => true;

    /// <summary>
    /// 将解锁条件设置为所有的前置页已完成
    /// </summary>
    public void SetPredecessorsOfAllComplete(IEnumerable<AchievementPage> pages)
    {
        UnlockCondition = () => pages.All(p => p.State == StateEnum.Completed);
        pages.ForeachDo(p => p.OnComplete += TryUnlock);
        OnUnlock += () => pages.ForeachDo(p => p.OnComplete -= TryUnlock);
    }
    /// <summary>
    /// 将解锁条件设置为任意的前置页已完成
    /// </summary>
    public void SetPredecessorsOfAnyComplete(IEnumerable<AchievementPage> pages)
    {
        UnlockCondition = () => pages.Any(p => p.State == StateEnum.Completed);
        pages.ForeachDo(p => p.OnComplete += UnlockSafe);
        OnUnlock += () => pages.ForeachDo(p => p.OnComplete -= TryUnlock);
    }
    /// <summary>
    /// 将解锁条件设置为前置页已完成
    /// </summary>
    public void SetPredecessorComplete(AchievementPage page)
    {
        UnlockCondition = () => page.State == StateEnum.Completed;
        page.OnComplete += TryUnlock;
        OnUnlock += () => page.OnComplete -= TryUnlock;
    }
    #endregion
    public static event Action<AchievementPage>? OnUnlockStatic;
    public event Action? OnUnlock;
    public virtual void TryUnlock()
    {
        if (State == StateEnum.Locked && UnlockCondition())
        {
            UnlockSafe();
        }
    }
    public virtual void UnlockSafe()
    {
        if (State != StateEnum.Locked)
        {
            return;
        }
        State = StateEnum.Unlocked;
        OnUnlockStatic?.Invoke(this);
        OnUnlock?.Invoke();
    }

    #region 完成条件
    /// <summary>
    /// <br/>设置这个以自定义完成条件
    /// <br/>默认的完成条件为页内的所有成就达到完成或关闭的状态
    /// <br/>默认会在每个成就完成时检测成就页的完成情况
    /// <br/>如果完成条件不仅与成就的完成状况有关
    /// <br/>则需要额外在对应的地方挂上<see cref="TryComplete"/>
    /// </summary>
    public Func<bool> CompleteCondition;
    public bool DefaultCompleteCondition() => Achievements.Values.All(a => a.State.IsCompleted() || a.State.IsClosed());
    #endregion
    public static event Action<AchievementPage>? OnCompleteStatic;
    public event Action? OnComplete;
    public virtual void TryComplete()
    {
        if (State == StateEnum.Unlocked && CompleteCondition())
        {
            CompleteSafe();
        }
    }
    public virtual void CompleteSafe()
    {
        if (State != StateEnum.Unlocked)
        {
            return;
        }
        State = StateEnum.Completed;
        OnCompleteStatic?.Invoke(this);
        OnComplete?.Invoke();
    }

    #region 禁用
    public void Disable()
    {
        State = StateEnum.Disabled;
    }
    #endregion

    #endregion

    #region 初始化与创建一个成就页
    static AchievementPage()
    {
        // 在成就完成时尝试完成成就页
        Achievement.OnCompleteStatic += a => a.Page.TryComplete();

        OnUnlockStatic += p => p.TryComplete();
    }
    /// <summary>
    /// 私有的构造方法
    /// </summary>
    /// <param name="mod">添加此成就页的模组</param>
    /// <param name="name">此成就页的内部名</param>
    protected AchievementPage(Mod mod, string name) : this()
    {
        Mod = mod;
        Name = name;
    }
    private AchievementPage()
    {
        UnlockCondition = DefaultUnlockCondition;
        CompleteCondition = DefaultCompleteCondition;
    }

    /// <summary>
    /// 创建或获得一个成就页
    /// </summary>
    /// <param name="mod">添加此成就页的模组</param>
    /// <param name="name">此成就页的内部名</param>
    /// <returns>创建的成就页, 若已有同名页则返回此同名页</returns>
    public static AchievementPage Create(Mod mod, string name)
    {
        string fullName = string.Join('.', mod.Name, name);
        if (AchievementManager.Pages.ContainsKey(fullName))
        {
            return AchievementManager.Pages[fullName];
        }
        AchievementPage result = new(mod, name);
        AchievementManager.AddPage(result);
        return result;
    }

    public void PostInitialize()
    {
        Achievements.Values.ForeachDo(a => a.PostInitialize());
        SetDefaultPositionForAchievements();
    }
    public void SetDefaultPositionForAchievements()
    {
        var values = achievements.Values;
        int achievementCount = values.Count;
        int sqrt = (int)Math.Ceiling(Math.Sqrt(achievementCount));
        bool[,] cell = new bool[sqrt, sqrt];
        int i = 0;
        var e = values.GetEnumerator();
        int pow2 = sqrt * sqrt;
        while (e.MoveNext())
        {
            if (e.Current.Position.HasValue)
            {
                Vector2 position = e.Current.Position.Value;
                int x = (int)(position.X + 0.5f), y = (int)(position.Y + 0.5f);
                if (0 <= x && x < sqrt && 0 <= y && y < sqrt)
                {
                    cell[x, y] = true;
                }
                continue;
            }
            while (i < pow2 && cell[i / sqrt, i % sqrt])
            {
                i += 1;
            }
            e.Current.Position = new(i % sqrt, i / sqrt);
            i += 1;
        }
    }
    #endregion

    #region 添加及获取成就

    #region 添加成就
    /// <summary>
    /// 向此成就页添加一个成就
    /// </summary>
    /// <param name="achievement">要添加的成就</param>
    /// <returns>是否成功添加(当此成就页内有同名成就时失败)</returns>
    public bool Add(Achievement achievement)
    {
        if (Achievements.ContainsKey(achievement.FullName))
        {
            return false;
        }
        achievements.Add(achievement.FullName, achievement);
        return true;
    }
    /// <summary>
    /// 强制向此成就页添加一个成就, 若有同名成就则报错
    /// </summary>
    /// <param name="achievement">要添加的成就</param>
    public void AddF(Achievement achievement)
    {
        achievements.Add(achievement.FullName, achievement);
    }
    /// <summary>
    /// 强制向此成就页添加一个成就, 若有同名成就则替换它
    /// </summary>
    /// <param name="achievement">要添加的成就</param>
    /// <returns>被替换掉的成就</returns>
    public Achievement? AddR(Achievement achievement)
    {
        string key = achievement.FullName;
        achievements.TryGetValue(key, out var orig);
        achievements[key] = achievement;
        return orig;
    }
    /// <summary>
    /// 向此成就页添加一个成就
    /// </summary>
    /// <param name="achievement">要添加的成就</param>
    /// <returns>自身</returns>
    public AchievementPage AddL(Achievement achievement)
    {
        Add(achievement);
        return this;
    }
    /// <summary>
    /// 强制向此成就页添加一个成就, 若有同名成就则报错
    /// </summary>
    /// <param name="achievement">要添加的成就</param>
    /// <returns>自身</returns>
    public AchievementPage AddFL(Achievement achievement)
    {
        AddF(achievement);
        return this;
    }
    /// <summary>
    /// 强制向此成就页添加一个成就, 若有同名成就则替换它
    /// </summary>
    /// <param name="achievement">要添加的成就</param>
    /// <returns>自身</returns>
    public AchievementPage AddRL(Achievement achievement)
    {
        achievements[achievement.FullName] = achievement;
        return this;
    }
    void ICollection<Achievement>.Add(Achievement item) => AddF(item);
    #endregion

    #region 获取成就
    /// <summary>
    /// 获得此成就页内某个名字的成就
    /// </summary>
    /// <param name="achievementFullName">成就名</param>
    /// <returns>找到的成就, 若没有这个名字的成就, 则返回<see langword="null"/></returns>
    public Achievement? GetAchievement(string achievementFullName)
    {
        return Achievements.TryGetValue(achievementFullName, out Achievement? result) ? result : null;
    }
    /// <summary>
    /// 强制获得此成就页内某个名字的成就
    /// </summary>
    /// <param name="achievementFullName">成就名</param>
    /// <returns>找到的成就, 若没有这个名字的成就, 则报错</returns>
    public Achievement GetAchievementF(string achievementFullName)
    {
        return Achievements[achievementFullName];
    }
    /// <summary>
    /// 获取一个成就, 若不存在则返回 <see langword="null"/>
    /// </summary>
    /// <param name="mod">此成就所在模组</param>
    /// <param name="name">此成就的名字 (<see cref="Achievement.Name"/>)</param>
    public Achievement? GetAchievement(Mod mod, string name)
    {
        return Achievements.Values.FirstOrDefault(a => a.Mod == mod && a.Name == name);
    }
    /// <summary>
    /// 强制获取一个成就, 若不存在则报错
    /// </summary>
    /// <param name="mod">此成就所在模组</param>
    /// <param name="name">此成就的名字 (<see cref="Achievement.Name"/>)</param>
    public Achievement GetAchievementF(Mod mod, string name)
    {
        return Achievements.Values.First(a => a.Mod == mod && a.Name == name);
    }
    /// <summary>
    /// 获取一个成就, 若不存在则返回 <see langword="null"/>
    /// </summary>
    /// <param name="name">此成就的名字 (<see cref="Achievement.Name"/>)</param>
    public Achievement? GetAchievementByName(string name)
    {
        return Achievements.Values.FirstOrDefault(a => a.Name == name);
    }
    /// <summary>
    /// 强制获取一个成就, 若不存在则报错
    /// </summary>
    /// <param name="name">此成就的名字 (<see cref="Achievement.Name"/>)</param>
    public Achievement GetAchievementByNameF(string name)
    {
        return Achievements.Values.First(a => a.Name == name);
    }

    public int GetAchievementIndex(string achievementFullName)
    {
        return achievements.GetIndexByKey(achievementFullName);
    }
    public int GetAchievementIndex(Achievement achievement)
    {
        return achievements.GetIndexByKey(achievement.FullName);
    }
    public int GetAchievementIndexByName(string name)
    {
        var achievement = GetAchievementByName(name);
        if (achievement == null)
        {
            return -1;
        }
        return GetAchievementIndex(achievement);
    }
    public int GetAchievementIndexByNameF(string name)
    {
        return GetAchievementIndex(GetAchievementByName(name)!);
    }

    public Achievement GetAchievementByIndexF(int index) => achievements.GetValueByIndex(index);
    public Achievement? GetAchievementByIndexS(int index) => achievements.GetValueByIndexS(index);
    #endregion

    #region 移除成就
    public bool Remove(Achievement item)
    {
        return achievements.Remove(item.FullName);
    }
    public bool Remove(string achievementFullName)
    {
        return achievements.Remove(achievementFullName);
    }
    public void Clear() => achievements.Clear();
    #endregion

    #endregion

    #region 存取数据
    public virtual void SaveDataInWorld(TagCompound tag)
    {
        tag.SaveDictionaryData("Achievements", achievements, (a, t) => a.SaveDataInWorld(t));
    }
    public virtual void LoadDataInWorld(TagCompound tag)
    {
        tag.LoadDictionaryData("Achievements", achievements, (a, t) => a.LoadDataInWorld(t));
    }
    public virtual void SaveDataInPlayer(TagCompound tag)
    {
        tag.SetWithDefault("State", State.ToString(), StateEnum.Locked.ToString());
        tag.SaveDictionaryData("Achievements", achievements, (a, t) => a.SaveDataInPlayer(t));
    }
    public virtual void LoadDataInPlayer(TagCompound tag)
    {
        if (Enum.TryParse(tag.GetWithDefault("State", StateEnum.Locked.ToString()), out StateEnum state))
        {
            State = state;
        }
        tag.LoadDictionaryData("Achievements", achievements, (a, t) => a.LoadDataInPlayer(t));
    }
    public bool ShouldSaveStaticData { get; set; }
    public virtual void SaveStaticData(TagCompound tag)
    {
        this.SaveStaticDataTemplate(achievements.Values, a => a.FullName, "Achievements", tag);
    }
    public virtual void LoadStaticData(TagCompound tag)
    {
        this.LoadStaticDataTemplate(fullName => achievements.TryGetValue(fullName, out var a) ? a : null,
            (a, m, n) =>
            {
                a.Mod = m;
                a.Name = n;
                a.Page = this;
                a.LocalizedKey = string.Join('.', FullName, n);
                a.TexturePath = string.Join('/', Mod.Name, Name, n);
            }, achievements.Add, "Achievements", tag);
    }
    #endregion

    #region 网络同步
    // TODO: Page 自身的网络同步
    protected bool _netUpdate;
    public bool NetUpdate { get => _netUpdate; set => DoIf(_netUpdate = value, AchievementManager.SetNeedNetUpdate); }
    public IEnumerable<INetUpdate> GetNetUpdateChildren() => Achievements.Values;
    #endregion

    #region 进度
    public float Progress { get; protected set; }
    public Func<float>? ProgressWeightOverride;
    float IProgressable.ProgressWeight => ProgressWeightOverride?.Invoke() ?? Achievements.Count;
    IEnumerable<IProgressable> IProgressable.ProgressChildren => Achievements.Values;
    public void UpdateProgress()
    {
        if (State == StateEnum.Completed)
        {
            if (Progress < 1)
            {
                Progress = 1;
                AchievementManager.UpdateProgress();
            }
            return;
        }
        float oldProgress = Progress;
        Progress = ((IProgressable)this).GetProgressOfChildrenWithProgressHandler(p => (p >= 1).ToInt());
        if (oldProgress != Progress)
        {
            AchievementManager.UpdateProgress();
        }
    }
    #endregion

    public override string ToString()
    {
        return $"{FullName}: {State}";
    }

    #region ICollection 杂项的实现
    public int Count => achievements.Count;
    public bool IsReadOnly => false;
    public Achievement this[int index] { get => GetAchievementByIndexF(index); set => throw new NotImplementedException(); }
    public bool Contains(Achievement item) => Achievements.ContainsKey(item.FullName);
    public void CopyTo(Achievement[] array, int arrayIndex)
        => Achievements.Values.WithIndex().ForeachDo(pair => { array[arrayIndex + pair.index] = pair.value; });
    public IEnumerator<Achievement> GetEnumerator() => Achievements.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Achievements.Values.GetEnumerator();
    #endregion

    #region 杂项
    public void TryReceiveAllRewards()
    {
        foreach (var achievement in Achievements.Values)
        {
            achievement.TryReceiveAllReward();
        }
    }
    #endregion
}
