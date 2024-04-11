namespace ProgressSystem.Core;

/// <summary>
/// 成就页
/// 代表一个显示多个成就的界面
/// </summary>
public class AchievementPage {

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
    /// 键为<see cref="Achievement.Name"/>
    /// </summary>
    public Dictionary<string, Achievement> Achievements = [];

    /// <summary>
    /// UI 面板上是否可编辑
    /// 用于 modder 编辑页内成就位置
    /// 发布时请确保此值为假
    /// </summary>
    public bool Editable;
    #endregion

    #region 锁定
    public bool Locked { get; protected set; }
    public void Unlock() {
        if(!Locked) {
            return;
        }
        Locked = false;
    }
    #endregion

    #region 完成
    public bool Completed { get; protected set; }
    #endregion

    #region 创建一个成就页
    /// <summary>
    /// 私有的构造方法
    /// </summary>
    /// <param name="mod">添加此成就页的模组</param>
    /// <param name="name">此成就页的内部名</param>
    private AchievementPage(Mod mod, string name) {
        Mod = mod;
        Name = name;
    }

    /// <summary>
    /// 创建或获得一个成就页
    /// </summary>
    /// <param name="mod">添加此成就页的模组</param>
    /// <param name="name">此成就页的内部名</param>
    /// <returns>创建的成就页, 若已有同名页则返回此同名页</returns>
    public static AchievementPage Create(Mod mod, string name) {
        string fullName = string.Join('.', mod.Name, name);
        if(AchievementManager.Pages.ContainsKey(fullName)) {
            return AchievementManager.Pages[fullName];
        }
        AchievementPage result = new(mod, name);
        AchievementManager.Pages.Add(fullName, result);
        return result;
    }
    public static AchievementPage Create(Mod mod, string name, string customFullName) {
        if(AchievementManager.Pages.ContainsKey(customFullName)) {
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
    public static AchievementPage ForceCreate(Mod mod, string name) {
        return AchievementManager.Pages[string.Join('.', mod.Name, name)] = new(mod, name);
    }

    /// <summary>
    /// 强制创建一个成就页
    /// </summary>
    /// <param name="mod">添加此成就页的模组</param>
    /// <param name="name">此成就页的内部名</param>
    /// <returns>创建的成就页, 若已有同名页则替换它</returns>
    public static AchievementPage ForceCreate(Mod mod, string name, string customFullName) {
        return AchievementManager.Pages[customFullName] = new AchievementPage(mod, name) { FullNameOverride = customFullName };
    }
    #endregion

    /// <summary>
    /// 向此成就页添加一个成就
    /// </summary>
    /// <param name="achievement">要添加的成就</param>
    /// <returns>是否成功添加(当此成就页内有同名成就时失败)</returns>
    public bool Add(Achievement achievement) {
        if(Achievements.ContainsKey(achievement.Name)) {
            return false;
        }
        Achievements.Add(achievement.Name, achievement);
        return true;
    }

    /// <summary>
    /// 获得此成就页内某个名字的成就
    /// </summary>
    /// <param name="achievementName">成就名</param>
    /// <returns>找到的成就, 若没有这个名字的成就, 则返回<see langword="null"/></returns>
    public Achievement? Get(string achievementName) {
        return Achievements.TryGetValue(achievementName, out var result) ? result : null;
    }

    /// <summary>
    /// 强制获得此成就页内某个名字的成就
    /// </summary>
    /// <param name="achievementName">成就名</param>
    /// <returns>找到的成就, 若没有这个名字的成就, 则报错</returns>
    public Achievement GetF(string achievementName) {
        return Achievements[achievementName];
    }
}
