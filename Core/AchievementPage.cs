using System.IO;

namespace ProgressSystem.Core;

/// <summary>
/// 成就页
/// 代表一个显示多个成就的界面
/// </summary>
public sealed class AchievementPage
{
    #region Vars
    public Mod Mod;

    /// <summary>
    /// 此成就页的内部名
    /// </summary>
    public string Name;

    public string? FullNameOverride;
    public string FullName => FullNameOverride ?? string.Join('.', Mod.Name, Name);

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
    public event Action? OnReset;
    public void Reset() {
        State = StateEnum.Locked;
        if (UnlockCondition == DefaultUnlockCondition)
        {
            State = StateEnum.Unlocked;
        }
        Achievements.Values.ForeachDo(a => a.Reset());
        OnReset?.Invoke();
    }
    public event Action? OnStart;
    public void Start()
    {
        Achievements.Values.ForeachDo(a => a.Start());
        Achievements.Values.ForeachDo(a => a.CheckState());
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
    public StateEnum State;
    /// <summary>
    /// <br/>如果想自定义解锁条件则修改这个(如果不动这个的话会在<see cref="Reset"/>时自动解锁)
    /// <br/>并且在合适的地方挂<see cref="TryUnlock"/> 或 <see cref="UnlockSafe"/> 的钩子
    /// <br/>最好再在 <see cref="OnUnlock"/> 中卸掉钩子
    /// <br/>如果只是简单的
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
    public void SetPredecessorsOfAnyComplete(IEnumerable<AchievementPage> pages)
    {
        UnlockCondition = () => pages.Any(p => p.State == StateEnum.Completed);
        pages.ForeachDo(p => p.OnComplete += UnlockSafe);
        OnUnlock += () => pages.ForeachDo(p => p.OnComplete -= TryUnlock);
    }
    public event Action? OnUnlock;
    public void TryUnlock()
    {
        if (UnlockCondition())
        {
            UnlockSafe();
        }
    }
    public void UnlockSafe()
    {
        if (State != StateEnum.Locked)
        {
            return;
        }
        State = StateEnum.Unlocked;
        OnUnlock?.Invoke();
    }
    public event Action? OnComplete;
    public void CompleteSafe()
    {
        if (State != StateEnum.Unlocked)
        {
            return;
        }
        State = StateEnum.Completed;
        OnComplete?.Invoke();
    }
    #endregion

    #region 创建一个成就页
    /// <summary>
    /// 私有的构造方法
    /// </summary>
    /// <param name="mod">添加此成就页的模组</param>
    /// <param name="name">此成就页的内部名</param>
    private AchievementPage(Mod mod, string name)
    {
        Mod = mod;
        Name = name;
        UnlockCondition = DefaultUnlockCondition;
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
        AchievementManager.Pages.Add(fullName, result);
        return result;
    }
    public static AchievementPage Create(Mod mod, string name, string customFullName)
    {
        if (AchievementManager.Pages.ContainsKey(customFullName))
        {
            return AchievementManager.Pages[customFullName];
        }
        AchievementPage result = new(mod, name) { FullNameOverride = customFullName };
        AchievementManager.Pages.Add(customFullName, result);
        return result;
    }

    /// <summary>
    /// 强制创建一个成就页
    /// </summary>
    /// <param name="mod">添加此成就页的模组</param>
    /// <param name="name">此成就页的内部名</param>
    /// <returns>创建的成就页, 若已有同名页则替换它</returns>
    public static AchievementPage ForceCreate(Mod mod, string name)
    {
        return AchievementManager.Pages[string.Join('.', mod.Name, name)] = new(mod, name);
    }

    /// <summary>
    /// 强制创建一个成就页
    /// </summary>
    /// <param name="mod">添加此成就页的模组</param>
    /// <param name="name">此成就页的内部名</param>
    /// <returns>创建的成就页, 若已有同名页则替换它</returns>
    public static AchievementPage ForceCreate(Mod mod, string name, string customFullName)
    {
        return AchievementManager.Pages[customFullName] = new AchievementPage(mod, name) { FullNameOverride = customFullName };
    }
    #endregion

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
    /// 获得此成就页内某个名字的成就
    /// </summary>
    /// <param name="achievementFullName">成就名</param>
    /// <returns>找到的成就, 若没有这个名字的成就, 则返回<see langword="null"/></returns>
    public Achievement? Get(string achievementFullName)
    {
        return Achievements.TryGetValue(achievementFullName, out var result) ? result : null;
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

    #region 存取数据
    // TODO: 存取 Page 自身的数据
    public void SaveDataInWorld(TagCompound tag) {
        tag.SaveDictionaryData("Achievements", Achievements, (a, t) => a.SaveDataInWorld(t));
    }
    public void LoadDataInWorld(TagCompound tag) {
        tag.LoadDictionaryData("Achievements", Achievements, (a, t) => a.LoadDataInWorld(t));
    }
    public void SaveDataInPlayer(TagCompound tag) {
        tag.SetWithDefault("State", State.ToString(), StateEnum.Locked.ToString());
        tag.SaveDictionaryData("Achievements", Achievements, (a, t) => a.SaveDataInPlayer(t));
    }
    public void LoadDataInPlayer(TagCompound tag) {
        if (Enum.TryParse(tag.GetWithDefault("State", StateEnum.Locked.ToString()), out StateEnum state))
        {
            State = state;
        }
        tag.LoadDictionaryData("Achievements", Achievements, (a, t) => a.LoadDataInPlayer(t));
    }
    #endregion

    #region 网络同步
    // TODO: Page 自身的网络同步
    public void NetSend(BinaryWriter writer) {
        Achievements.Values.ForeachDo(a => a.NetSend(writer));
    }
    public void NetReceive(BinaryReader reader) {
        Achievements.Values.ForeachDo(a => a.NetReceive(reader));
    }
    #endregion
}
