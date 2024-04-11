using System.IO;

namespace ProgressSystem.Core;

/// <summary>
/// 成就
/// </summary>
public class Achievement
{
    #region Vars
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

    public string? LocalizedKeyOverride;
    public string LocalizedKey => LocalizedKeyOverride ?? string.Join('.', Page.FullName, Name);

    public string? TexturePathOverride;
    public string TexturePath => TexturePathOverride ?? string.Join('/', Page.Name, Name);

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
            if (predecessors == null)
            {
                predecessors = [];
                _predecessorNames?.ForEach(AddPredecessor);
            }
            return predecessors;
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
    public void SetPredecessorNames(List<string>? predecessorNames)
    {
        predecessors?.ForEach(p => p.successors.Remove(this));
        predecessors = null;
        _predecessorNames = predecessorNames;
    }
    /// <summary>
    /// 添加一个前置
    /// </summary>
    /// <param name="predecessorName"></param>
    public void AddPredecessor(string predecessorName)
    {
        if (predecessors == null)
        {
            (_predecessorNames ??= []).Add(predecessorName);
            return;
        }
        var predecessor = Page.Get(predecessorName);
        predecessor?.successors.Add(this);
        predecessors.AddIfNotNull(predecessor);
    }
    /// <summary>
    /// 移除一个前置
    /// </summary>
    /// <param name="predecessorName"></param>
    public void RemovePredecessor(string predecessorName)
    {
        if (predecessors == null)
        {
            if (_predecessorNames == null)
            {
                return;
            }
            _predecessorNames.Remove(predecessorName);
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
    }

    protected List<Achievement>? predecessors;
    protected List<Achievement> successors = [];
    protected List<string>? _predecessorNames;

    /// <summary>
    /// 需要多少个前置才能开始此任务
    /// 默认 <see langword="null"/> 代表需要所有前置完成
    /// 如果此值大于前置数, 那么以前置数为准
    /// 1 代表只需要任意前置完成即可
    /// 0 代表实际上不需要前置完成, 前置只是起提示作用
    /// 负数(-n)代表有 n 个前置完成时此任务封闭, 不可再完成
    /// </summary>
    public int? PredecessorCountNeeded;

    /// <summary>
    /// 是否不记忆前置完成状态
    /// 若<see cref="PredecessorCountNeeded"/>小于 0 则为前置不符合条件的状态
    /// </summary>
    public bool PredecessorMetNotSaved;

    /// <summary>
    /// 记忆下的前置状态
    /// 一般代表前置是否完成过
    /// <see cref="PredecessorCountNeeded"/>小于 0 时代表前置是否达成封闭条件
    /// </summary>
    protected bool _predecessorsSaved;

    /// <summary>
    /// 判断前置是否完成
    /// </summary>
    public virtual bool IsPredecessorsMet()
    {
        int count = Predecessors.Count;
        int needed = (PredecessorCountNeeded ?? count).WithMax(count);
        if (_predecessorsSaved)
        {
            return needed >= 0;
        }
        int sum = Predecessors.Sum(p => p.Completed.ToInt());
        bool saveCondition = sum >= Math.Abs(needed);
        if (!PredecessorMetNotSaved)
        {
            _predecessorsSaved = saveCondition;
        }
        return needed < 0 ^ saveCondition;
    }
    #endregion

    #region 条件
    /// <summary>
    /// 条件
    /// </summary>
    public List<Requirement> Requirements;

    /// <summary>
    /// 条件是否满足
    /// </summary>
    /// <returns>默认返回是否所有条件都分别满足</returns>
    public virtual bool IsRequirementsMet()
    {
        return !Requirements.ForeachDoB(r => !r.Completed);
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
    /// <param name="predecessorNames">前置的名字(需要在同一页)</param>
    /// <param name="requirements">条件</param>
    /// <param name="rewards">奖励</param>
    /// <param name="texture">图片</param>
    /// <param name="displayName">显示的名字, 默认通过对应 Mod 的 Achievements.[ModName].[PageName].[AcievementName].DisplayName 获取</param>
    /// <param name="tooltip">鼠标移上去时显示的提示, 默认通过对应 Mod 的 Achievements.[ModName].[PageName].[AcievementName].Tooltip 获取</param>
    /// <param name="description">详细说明, 默认通过对应 Mod 的 Achievements.[ModName].[PageName].[AcievementName].Description 获取</param>
    public Achievement(Mod mod, AchievementPage page, string name,
        List<string>? predecessorNames = null,
        List<Requirement>? requirements = null,
        List<Reward>? rewards = null,
        TextGetter displayName = default,
        TextGetter tooltip = default,
        TextGetter description = default,
        Texture2DGetter texture = default)
    {
        Mod = mod;
        Page = page;
        Name = name;
        SetPredecessorNames(predecessorNames);
        Requirements = requirements ?? [];
        Requirements.ForEach(r => r.OnComplete += TryComplete);
        Rewards = rewards ?? [];
        _displayName = displayName;
        _tooltip = tooltip;
        _description = description;
        _texture = texture;
    }
    #endregion

    #region 开始
    public bool Started;
    public void Reset() {
        Started = false;
        Requirements.ForEach(r => r.Reset());
    }
    public void Start() {
        if (Started) {
            return;
        }
        Started = true;
        Predecessors.ForeachDo(p => p.Start());
        if (IsPredecessorsMet()) {
            Unlock();
        }
        Requirements.ForEach(r => r.Start());
    }
    #endregion

    #region 完成相关
    public bool Completed { get; protected set; }
    public event Action? OnComplete;
    public void Complete()
    {
        if (Completed)
        {
            return;
        }
        Completed = true;
        OnComplete?.Invoke();
        Successors.ForeachDo(s => s.PredecessorCompleted(this));
    }
    /// <summary>
    /// 在每一个前置完成时被调用
    /// </summary>
    /// <param name="predecessor">前置</param>
    public virtual void PredecessorCompleted(Achievement predecessor)
    {
        TryUnlock();
    }
    public virtual void TryComplete()
    {
        if (!Completed && IsPredecessorsMet() && IsRequirementsMet())
        {
            Complete();
        }
    }
    #endregion

    #region 解锁
    public bool Unlocked;
    public void TryUnlock() {
        if (Unlocked) {
            return;
        }
        if (IsPredecessorsMet()) {
            Unlock();
        }
    }
    public virtual void Unlock() {
        Unlocked = true;
        Requirements.ForEach(r => DoIf(r.ListenType == Requirement.ListenTypeEnum.OnUnlocked, r.BeginListenSafe));
    }
    #endregion

    #region 数据存取
    // todo: 成就本身与奖励相关的数据存取
    public void SaveDataInWorld(TagCompound tag) {
        tag.SetWithDefault("Unlocked", Unlocked);
        tag.SetWithDefault("Completed", Completed);
        var requirementsData = Requirements.Select(r => new TagCompound().WithAction(r.SaveDataInWorld)).ToArray();
        if (requirementsData.Any(t => t.Count > 0)) {
            tag["Requirements"] = requirementsData;
        }
    }
    public void LoadDataInWorld(TagCompound tag) {
        if (tag.GetWithDefault<bool>("Unlocked")) {
            Unlock();
        }
        if (tag.GetWithDefault<bool>("Completed")) {
            Complete();
        }
        if (tag.TryGet("Requirements", out TagCompound[] requirementsData)) {
            foreach (int i in Requirements.Count) {
                Requirements[i].LoadDataInWorld(requirementsData.GetS(i, []));
            }
        }
    }
    public void SaveDataInPlayer(TagCompound tag) {
        var requirementsData = Requirements.Select(r => new TagCompound().WithAction(r.SaveDataInPlayer)).ToArray();
        if (requirementsData.Any(t => t.Count > 0)) {
            tag["Requirements"] = requirementsData;
        }
    }
    public void LoadDataInPlayer(TagCompound tag) {
        if (tag.TryGet("Requirements", out TagCompound[] requirementsData)) {
            foreach (int i in Requirements.Count) {
                Requirements[i].LoadDataInPlayer(requirementsData.GetS(i, []));
            }
        }
    }
    #endregion

    #region 网络同步
    // todo: 成就本身与奖励相关的网络同步
    public void NetSend(BinaryWriter writer) {
        Requirements.ForEach(r => r.NetSend(writer));
    }
    public void NetReceive(BinaryReader reader) {
        Requirements.ForEach(r => r.NetReceive(reader));
    }
    #endregion
}
