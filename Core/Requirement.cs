using Humanizer;
using ProgressSystem.Core.StaticData;
using ProgressSystem.GameEvents;
using System.IO;
using Terraria.Localization;

namespace ProgressSystem.Core;

/// <summary>
/// 达成成就所需的条件
/// 继承它的非抽象类需要有一个无参构造 (用以读取静态数据)
/// </summary>
public abstract class Requirement : IWithStaticData, ILoadable
{
    public Achievement Achievement = null!;
    public TextGetter DisplayName;
    public TextGetter Tooltip;
    public Texture2DGetter Texture;
    protected virtual object?[] DisplayNameArgs => [];
    protected virtual object?[] TooltipArgs => [];
    #region 构造函数与初始化
    static Requirement()
    {
        // 在成就页解锁时尝试开始监听
        AchievementPage.OnUnlockStatic += p =>
            p.Achievements.Values.ForeachDo(a =>
                a.Requirements.ForeachDo(r =>
                    r.TryBeginListen()));
        // 在成就解锁时尝试开始监听
        Achievement.OnUnlockStatic += a =>
            a.Requirements.ForeachDo(r =>
                r.TryBeginListen());
        // 在开始时尝试开始监听写在了 Start() 中

        // 在成就完成时结束监听
        Achievement.OnCompleteStatic += a =>
        {
            foreach (var requirement in a.Requirements)
            {
                requirement.EndListenSafe();
            }
        };
    }
    public Requirement(ListenTypeEnum listenType = ListenTypeEnum.None, MultiplayerTypeEnum multiplayerType = MultiplayerTypeEnum.LocalPlayer)
    {
        ListenType = listenType;
        MultiplayerType = multiplayerType;
        Reset();
    }
    /// <summary>
    /// 初始化, 在被加入 <see cref="RequirementList"/> 时被调用
    /// </summary>
    public virtual void Initialize(Achievement achievement)
    {
        Achievement = achievement;
        // TODO
        if (DisplayName.IsNone)
        {
            Tooltip = achievement.Mod.GetLocalization($"Requirements.{GetType().Name}.DisplayName".FormatWith(DisplayNameArgs));
        }
        if (DisplayName.IsNone)
        {
            Tooltip = achievement.Mod.GetLocalization($"Requirements.{GetType().Name}.Tooltip".FormatWith(TooltipArgs));
        }
        if (Texture.IsNone)
        {
            Texture = $"{achievement.Mod.Name}/Assets/Textures/Requirements/{GetType().Name}";
        }
    }
    public virtual IEnumerable<ConstructInfoTable<Requirement>> GetConstructInfoTables()
    {
        ConstructInfoTable<Achievement>.TryAutoCreate<Requirement>(GetType(), null, out var constructors);
        return constructors;
    }
    #endregion
    #region 重置与开始
    /// <summary>
    /// 重置
    /// 初始化时也会被调用
    /// </summary>
    public virtual void Reset()
    {
        EndListenSafe();
        Completed = false;
    }

    public virtual void Start()
    {
        TryBeginListen();
    }
    #endregion
    #region 多人类型
    public enum MultiplayerTypeEnum
    {
        /// <summary>
        /// 只处理本地玩家, 每个玩家的条件分别处理
        /// 数据会储存在玩家处
        /// </summary>
        LocalPlayer,
        /// <summary>
        /// 每个玩家分别推进, 只要有任意玩家完成了条件即算全部玩家完成
        /// 数据会储存在玩家处, 而完成情况则储存在世界处
        /// </summary>
        AnyPlayer,
        /// <summary>
        /// 世界的条件, 条件在服务器处理
        /// 数据会储存在世界中
        /// </summary>
        World
    }
    /// <summary>
    /// 多人模式类型
    /// </summary>
    public MultiplayerTypeEnum MultiplayerType;
    #endregion
    #region 数据存取
    public virtual void SaveDataInPlayer(TagCompound tag)
    {
        if (MultiplayerType == MultiplayerTypeEnum.LocalPlayer)
        {
            tag.SetWithDefault("Completed", Completed);
        }
    }
    public virtual void LoadDataInPlayer(TagCompound tag)
    {
        if (MultiplayerType == MultiplayerTypeEnum.LocalPlayer)
        {
            Completed = tag.GetWithDefault<bool>("Completed");
        }
    }
    public virtual void SaveDataInWorld(TagCompound tag)
    {
        if (MultiplayerType is MultiplayerTypeEnum.AnyPlayer or MultiplayerTypeEnum.World)
        {
            tag.SetWithDefault("Completed", Completed);
        }
    }
    public virtual void LoadDataInWorld(TagCompound tag)
    {
        if (MultiplayerType is MultiplayerTypeEnum.AnyPlayer or MultiplayerTypeEnum.World)
        {
            Completed = tag.GetWithDefault<bool>("Completed");
        }
    }
    public bool ShouldSaveStaticData { get; set; }
    public virtual void SaveStaticData(TagCompound tag)
    {
        if (!ShouldSaveStaticData)
        {
            return;
        }
        tag["SaveStatic"] = true;
        tag["Type"] = GetType().FullName;
        tag.SetWithDefault("DisplayNameKey", DisplayName.LocalizedTextValue?.Key);
        tag.SetWithDefault("DisplayName", DisplayName.StringValue);
        tag.SetWithDefault("TooltipKey", Tooltip.LocalizedTextValue?.Key);
        tag.SetWithDefault("Tooltip", Tooltip.StringValue);
        tag.SetWithDefault("Texture", Texture.AssetPath);
    }
    public virtual void LoadStaticData(TagCompound tag)
    {
        ShouldSaveStaticData = tag.GetWithDefault<bool>("SaveStatic");
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
        Texture = tag.GetWithDefault<string>("Texture");
    }
    #endregion
    #region 多人同步
    public virtual void NetSend(BinaryWriter writer)
    {
        if (MultiplayerType is MultiplayerTypeEnum.AnyPlayer or MultiplayerTypeEnum.World)
        {
            writer.Write(Completed);
        }
    }
    public virtual void NetReceive(BinaryReader reader)
    {
        if (MultiplayerType is MultiplayerTypeEnum.AnyPlayer or MultiplayerTypeEnum.World)
        {
            if (reader.ReadBoolean())
            {
                CompleteSafe();
            }
        }
    }
    #endregion
    #region 监听
    public enum ListenTypeEnum
    {
        /// <summary>
        /// 不监听
        /// </summary>
        None,
        /// <summary>
        /// 在成就解锁时开始监听
        /// </summary>
        OnAchievementUnlocked,
        /// <summary>
        /// 在成就页解锁时开始监听
        /// </summary>
        OnPageUnlocked,
        /// <summary>
        /// 在进入世界时就开始监听
        /// </summary>
        OnStart,
    }
    /// <summary>
    /// 什么时候开始监听
    /// </summary>
    public ListenTypeEnum ListenType;
    public bool Listening { get; protected set; }
    public void TryBeginListen()
    {
        if (Listening || Completed)
        {
            return;
        }
        if (ListenType == ListenTypeEnum.None)
        {
            return;
        }
        if (ListenType == ListenTypeEnum.OnAchievementUnlocked && !Achievement.State.IsUnlocked())
        {
            return;
        }
        if (ListenType == ListenTypeEnum.OnPageUnlocked && Achievement.Page.State == AchievementPage.StateEnum.Locked)
        {
            return;
        }
        BeginListenSafe();
    }
    public void BeginListenSafe()
    {
        if (Listening)
        {
            return;
        }
        BeginListen();
    }
    public void EndListenSafe()
    {
        if (!Listening)
        {
            return;
        }
        EndListen();
    }
    protected virtual void BeginListen()
    {
        Listening = true;
    }
    protected virtual void EndListen()
    {
        Listening = false;
    }
    #endregion
    #region 完成状况
    public bool Completed { get; protected set; }
    public event Action? OnComplete;
    public static event Action<Requirement>? OnCompleteStatic;
    protected void DoOnComplete()
    {
        OnComplete?.Invoke();
        OnCompleteStatic?.Invoke(this);
    }
    public void CompleteSafe()
    {
        if (Completed)
        {
            return;
        }
        Complete();
    }
    protected virtual void Complete()
    {
        Completed = true;
        EndListenSafe();
        DoOnComplete();
    }
    #endregion

    public override string ToString()
    {
        return $"{GetType().Name}: {nameof(Completed)}: {Completed}, {nameof(Listening)}: {Listening}";
    }

    public virtual void Load(Mod mod) { }

    public virtual void Unload() { }
}
public abstract class RequirementCombination : Requirement
{
    public List<Requirement> Requirements = [];
    protected RequirementCombination() { }
    public RequirementCombination(IEnumerable<Requirement> requirements)
    {
        Requirements = [.. requirements];
        foreach (int i in Requirements.Count)
        {
            Requirements[i].OnComplete += () => ElementComplete(i);
        }
    }
    public override void Reset()
    {
        base.Reset();
        Requirements.ForEach(r => r.Reset());
    }
    #region 数据存取
    public override void SaveDataInPlayer(TagCompound tag)
    {
        base.SaveDataInPlayer(tag);
        tag.SaveListData("Requirements", Requirements, (r, t) => r.SaveDataInPlayer(t));
    }
    public override void LoadDataInPlayer(TagCompound tag)
    {
        base.LoadDataInPlayer(tag);
        tag.LoadListData("Requirements", Requirements, (r, t) => r.LoadDataInPlayer(t));
    }
    public override void SaveDataInWorld(TagCompound tag)
    {
        base.SaveDataInWorld(tag);
        tag.SaveListData("Requirements", Requirements, (r, t) => r.SaveDataInWorld(t));
    }
    public override void LoadDataInWorld(TagCompound tag)
    {
        base.LoadDataInWorld(tag);
        tag.LoadListData("Requirements", Requirements, (r, t) => r.LoadDataInWorld(t));
    }
    public override void SaveStaticData(TagCompound tag)
    {
        base.SaveStaticData(tag);
        this.SaveStaticDataListTemplate(Requirements, "Requirements", tag);
    }
    public override void LoadStaticData(TagCompound tag)
    {
        base.LoadStaticData(tag);
        this.LoadStaticDataListTemplate(Requirements.GetS, Requirements!.SetFS, "Requirements", tag, (r, t) => Requirements.Clear());
    }
    #endregion
    #region 多人同步
    public override void NetSend(BinaryWriter writer)
    {
        base.NetSend(writer);
        foreach (var requirement in Requirements)
        {
            requirement.NetSend(writer);
        }
    }
    public override void NetReceive(BinaryReader reader)
    {
        base.NetReceive(reader);
        foreach (var requirement in Requirements)
        {
            requirement.NetReceive(reader);
        }
    }
    #endregion
    #region 监听
    protected override void BeginListen()
    {
        base.BeginListen();
        foreach (var requirement in Requirements)
        {
            requirement.BeginListenSafe();
        }
    }
    protected override void EndListen()
    {
        base.EndListen();
        foreach (var requirement in Requirements)
        {
            requirement.EndListenSafe();
        }
    }
    #endregion
    #region 完成状况
    protected abstract void ElementComplete(int elementIndex);
    #endregion
}
public class AllOfRequirements : RequirementCombination
{
    public AllOfRequirements(IEnumerable<Requirement> requirements) : base(requirements) { }
    [SpecializeAutoConstruct(EnableEvenNonPublic = true)]
    protected AllOfRequirements() : base() { }
    protected override void ElementComplete(int elementIndex)
    {
        if (Requirements.All(r => r.Completed))
        {
            CompleteSafe();
        }
    }
}
public class AnyOfRequirements : RequirementCombination
{
    public AnyOfRequirements(IEnumerable<Requirement> requirements) : base(requirements) { }
    [SpecializeAutoConstruct(EnableEvenNonPublic = true)]
    protected AnyOfRequirements() : base() { }
    protected override void ElementComplete(int elementIndex)
    {
        CompleteSafe();
    }
}
public class SomeOfRequirements : RequirementCombination
{
    public SomeOfRequirements(IEnumerable<Requirement> requirements, int count) : base(requirements)
    {
        Count = count;
    }
    [SpecializeAutoConstruct(EnableEvenNonPublic = true)]
    protected SomeOfRequirements(int count) : base()
    {
        Count = count;
    }
    protected SomeOfRequirements() : base() { }
    public int Count;
    protected override void ElementComplete(int elementIndex)
    {
        if (Requirements.Sum(r => r.Completed.ToInt()) >= Count)
        {
            CompleteSafe();
        }
    }
    public override void SaveStaticData(TagCompound tag)
    {
        base.SaveStaticData(tag);
        if (ShouldSaveStaticData)
        {
            tag.SetWithDefault("Count", Count);
        }
    }
    public override void LoadDataInPlayer(TagCompound tag)
    {
        base.LoadDataInPlayer(tag);
        if (ShouldSaveStaticData)
        {
            tag.GetWithDefault("Count", out Count);
        }
    }
}
