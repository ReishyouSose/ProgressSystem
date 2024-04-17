using ProgressSystem.Core.NetUpdate;
using ProgressSystem.Core.StaticData;
using System.IO;

namespace ProgressSystem.Core;

/// <summary>
/// 成就页
/// 代表一个显示多个成就的界面
/// </summary>
public class AchievementPage : IWithStaticData, INetUpdate
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
    public Dictionary<string, Achievement> Achievements = [];

    /// <summary>
    /// UI 面板上是否可编辑
    /// 用于 modder 编辑页内成就位置
    /// 发布时请确保此值为假
    /// </summary>
    public bool Editable;
    #endregion

    #region 重置与开始
    public static event Action<AchievementPage>? OnResetStatic;
    public event Action? OnReset;
    /// <summary>
    /// 在初始化时就会调用一次
    /// </summary>
    public virtual void Reset()
    {
        Achievements.Values.ForeachDo(a => a.Reset());
        OnResetStatic?.Invoke(this);
        OnReset?.Invoke();
    }
    public static event Action<AchievementPage>? OnStartStatic;
    public event Action? OnStart;
    public virtual void Start()
    {
        CheckState();
        Achievements.Values.ForeachDo(a => a.Start());
        OnStartStatic?.Invoke(this);
        OnStart?.Invoke();
    }
    #endregion

    #region 状态 (锁定 / 完成)
    public enum StateEnum
    {
        Locked,
        Unlocked,
        Completed
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
    public void InitializeStateToUnlock()
    {
        State = StateEnum.Unlocked;
        OnReset += () => State = StateEnum.Unlocked;
    }

    /// <summary>
    /// 将解锁条件设置为所有的前置页已完成
    /// </summary>
    public void SetPredecessorsOfAllComplete(IEnumerable<AchievementPage> pages)
    {
        InitializeStateToUnlock();
        UnlockCondition = () => pages.All(p => p.State == StateEnum.Completed);
        pages.ForeachDo(p => p.OnComplete += TryUnlock);
        OnUnlock += () => pages.ForeachDo(p => p.OnComplete -= TryUnlock);
    }
    /// <summary>
    /// 将解锁条件设置为任意的前置页已完成
    /// </summary>
    public void SetPredecessorsOfAnyComplete(IEnumerable<AchievementPage> pages)
    {
        InitializeStateToUnlock();
        UnlockCondition = () => pages.Any(p => p.State == StateEnum.Completed);
        pages.ForeachDo(p => p.OnComplete += UnlockSafe);
        OnUnlock += () => pages.ForeachDo(p => p.OnComplete -= TryUnlock);
    }
    /// <summary>
    /// 将解锁条件设置为前置页已完成
    /// </summary>
    public void SetPredecessorComplete(AchievementPage page)
    {
        InitializeStateToUnlock();
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
        result.Reset();
        return result;
    }

    public void PostInitialize()
    {
        Achievements.Values.ForeachDo(a => a.PostInitialize());
        SetDefaultPositionForAchievements();
    }
    void SetDefaultPositionForAchievements()
    {
        var values = Achievements.Values;
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
        Achievements.Add(achievement.FullName, achievement);
        return true;
    }
    /// <summary>
    /// 强制向此成就页添加一个成就, 若有同名成就则报错
    /// </summary>
    /// <param name="achievement">要添加的成就</param>
    /// <returns>是否成功添加(当此成就页内有同名成就时失败)</returns>
    public void AddF(Achievement achievement)
    {
        Achievements.Add(achievement.FullName, achievement);
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
    /// 获得此成就页内某个名字的成就
    /// </summary>
    /// <param name="achievementFullName">成就名</param>
    /// <returns>找到的成就, 若没有这个名字的成就, 则返回<see langword="null"/></returns>
    public Achievement? Get(string achievementFullName)
    {
        return Achievements.TryGetValue(achievementFullName, out Achievement? result) ? result : null;
    }

    /// <summary>
    /// 强制获得此成就页内某个名字的成就
    /// </summary>
    /// <param name="achievementFullName">成就名</param>
    /// <returns>找到的成就, 若没有这个名字的成就, 则报错</returns>
    public Achievement GetF(string achievementFullName)
    {
        return Achievements[achievementFullName];
    }

    /// <summary>
    /// 获取一个成就, 若不存在则返回 <see langword="null"/>
    /// </summary>
    /// <param name="mod">此成就所在模组</param>
    /// <param name="name">此成就的名字 (<see cref="Achievement.Name"/>)</param>
    public Achievement? Get(Mod mod, string name)
    {
        return Achievements.Values.FirstOrDefault(a => a.Mod == mod && a.Name == name);
    }
    /// <summary>
    /// 强制获取一个成就, 若不存在则报错
    /// </summary>
    /// <param name="mod">此成就所在模组</param>
    /// <param name="name">此成就的名字 (<see cref="Achievement.Name"/>)</param>
    public Achievement GetF(Mod mod, string name)
    {
        return Achievements.Values.First(a => a.Mod == mod && a.Name == name);
    }
    /// <summary>
    /// 获取一个成就, 若不存在则返回 <see langword="null"/>
    /// </summary>
    /// <param name="name">此成就的名字 (<see cref="Achievement.Name"/>)</param>
    public Achievement? GetByName(string name)
    {
        return Achievements.Values.FirstOrDefault(a => a.Name == name);
    }
    /// <summary>
    /// 强制获取一个成就, 若不存在则报错
    /// </summary>
    /// <param name="name">此成就的名字 (<see cref="Achievement.Name"/>)</param>
    public Achievement GetByNameF(string name)
    {
        return Achievements.Values.First(a => a.Name == name);
    }
    #endregion

    #region 存取数据
    public virtual void SaveDataInWorld(TagCompound tag)
    {
        tag.SaveDictionaryData("Achievements", Achievements, (a, t) => a.SaveDataInWorld(t));
    }
    public virtual void LoadDataInWorld(TagCompound tag)
    {
        tag.LoadDictionaryData("Achievements", Achievements, (a, t) => a.LoadDataInWorld(t));
    }
    public virtual void SaveDataInPlayer(TagCompound tag)
    {
        tag.SetWithDefault("State", State.ToString(), StateEnum.Locked.ToString());
        tag.SaveDictionaryData("Achievements", Achievements, (a, t) => a.SaveDataInPlayer(t));
    }
    public virtual void LoadDataInPlayer(TagCompound tag)
    {
        if (Enum.TryParse(tag.GetWithDefault("State", StateEnum.Locked.ToString()), out StateEnum state))
        {
            State = state;
        }
        tag.LoadDictionaryData("Achievements", Achievements, (a, t) => a.LoadDataInPlayer(t));
    }
    public bool ShouldSaveStaticData { get; set; }
    public virtual void SaveStaticData(TagCompound tag)
    {
        this.SaveStaticDataTemplate(Achievements.Values, a => a.FullName, "Achievements", tag);
    }
    public virtual void LoadStaticData(TagCompound tag)
    {
        this.LoadStaticDataTemplate(fullName => Achievements.TryGetValue(fullName, out var a) ? a : null,
            (a, m, n) =>
            {
                a.Mod = m;
                a.Name = n;
                a.Page = this;
                a.LocalizedKey = string.Join('.', FullName, n);
                a.TexturePath = string.Join('/', Name, n);
            }, Achievements.Add, "Achievements", tag);
    }
    #endregion

    #region 网络同步
    // TODO: Page 自身的网络同步
    protected bool _netUpdate;
    public bool NetUpdate { get => _netUpdate; set => DoIf(_netUpdate = value, AchievementManager.SetNeedNetUpdate); }
    public IEnumerable<INetUpdate> GetNetUpdateChildren() => Achievements.Values;
    public virtual void WriteMessageFromServer(BinaryWriter writer) { }
    public virtual void ReceiveMessageFromServer(BinaryReader reader) { }
    public virtual void WriteMessageFromClient(BinaryWriter writer) { }
    public virtual void ReceiveMessageFromClient(BinaryReader reader) { }
    #endregion

    public override string ToString()
    {
        return $"{FullName}: {State}";
    }
}
