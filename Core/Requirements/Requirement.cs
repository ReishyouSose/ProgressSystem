using ProgressSystem.Core.Interfaces;
using ProgressSystem.Core.NetUpdate;
using ProgressSystem.Core.StaticData;
using System.IO;
using Terraria.Localization;

namespace ProgressSystem.Core.Requirements;

/// <summary>
/// <br/>达成成就所需的条件
/// <br/>继承它的非抽象类需要有一个无参构造 (用以读取静态数据)
/// </summary>
public abstract class Requirement : IWithStaticData, ILoadable, INetUpdate, IProgressable, IAchievementNode
{
    public Achievement Achievement = null!;
    public TextGetter DisplayName;
    public TextGetter Tooltip;
    public Texture2DGetter Texture;
    public Rectangle? SourceRect => GetSourceRect?.Invoke() ?? null;
    public Func<Rectangle?>? GetSourceRect;
    protected virtual object?[] DisplayNameArgs => [];
    protected virtual object?[] TooltipArgs => [];

    #region 构造函数与初始化
    public Requirement(ListenTypeEnum listenType = ListenTypeEnum.None, MultiplayerTypeEnum multiplayerType = MultiplayerTypeEnum.LocalPlayer) : this()
    {
        ListenType = listenType;
        MultiplayerType = multiplayerType;
    }
    protected Requirement()
    {
        WouldNetUpdate = WouldNetUpdateInitial;
    }
    /// <summary>
    /// 初始化, 在被加入 <see cref="RequirementList"/> 时被调用
    /// </summary>
    public virtual void Initialize(Achievement achievement)
    {
        Achievement = achievement;
        AchievementManager.DoAfterPostSetup(InitializeByDefinedMod);
    }
    public virtual void PostInitialize()
    {
        BeginListenHook();
        EndListenHook();
        CloseHook();
    }
    public virtual IEnumerable<ConstructInfoTable<Requirement>> GetConstructInfoTables()
    {
        ConstructInfoTable<Achievement>.TryAutoCreate<Requirement>(GetType(), null, out var constructors);
        return constructors;
    }
    #endregion

    #region 重置与开始

    public static event Action<Requirement>? OnResetStatic;
    public event Action? OnReset;
    public virtual void Reset()
    {
        State = StateEnum.Idle;
        OnResetStatic?.Invoke(this);
        OnReset?.Invoke();
    }

    public static event Action<Requirement>? OnStartStatic;
    public event Action? OnStart;
    public virtual void Start()
    {
        OnStartStatic?.Invoke(this);
        OnStart?.Invoke();
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
            tag.SetWithDefault("State", State.ToString(), StateEnum.Idle.ToString());
        }
    }
    public virtual void LoadDataInPlayer(TagCompound tag)
    {
        if (MultiplayerType == MultiplayerTypeEnum.LocalPlayer)
        {
            if (Enum.TryParse<StateEnum>(tag.GetWithDefault("State", StateEnum.Idle.ToString()), out var state))
            {
                State = state;
            }
        }
    }
    public virtual void SaveDataInWorld(TagCompound tag)
    {
        if (MultiplayerType is MultiplayerTypeEnum.AnyPlayer or MultiplayerTypeEnum.World)
        {
            tag.SetWithDefault("State", State.ToString(), StateEnum.Idle.ToString());
        }
    }
    public virtual void LoadDataInWorld(TagCompound tag)
    {
        if (MultiplayerType is MultiplayerTypeEnum.AnyPlayer or MultiplayerTypeEnum.World)
        {
            if (Enum.TryParse<StateEnum>(tag.GetWithDefault("State", StateEnum.Idle.ToString()), out var state))
            {
                State = state;
            }
        }
    }
    public bool ShouldSaveStaticData { get => Achievement.ShouldSaveStaticData; set { } }
    public virtual void SaveStaticData(TagCompound tag)
    {
        if (!ShouldSaveStaticData)
        {
            return;
        }
        // tag["SaveStatic"] = true;
        tag["Type"] = GetType().FullName;
        /*
        tag.SetWithDefault("DisplayNameKey", DisplayName.LocalizedTextValue?.Key);
        tag.SetWithDefault("DisplayName", DisplayName.StringValue);
        tag.SetWithDefault("TooltipKey", Tooltip.LocalizedTextValue?.Key);
        tag.SetWithDefault("Tooltip", Tooltip.StringValue);
        tag.SetWithDefault("Texture", Texture.AssetPath);
        */
    }
    public virtual void LoadStaticData(TagCompound tag)
    {
        // ShouldSaveStaticData = tag.GetWithDefault<bool>("SaveStatic");
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
        if (tag.TryGet<string>("Texture", out var texture))
        {
            Texture = texture;
        }
        */

    }
    #endregion

    #region 网络同步
    protected bool _netUpdate;
    public bool NetUpdate { get => _netUpdate; set => DoIf(_netUpdate = value, AchievementManager.SetNeedNetUpdate); }
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
    public virtual IEnumerable<IProgressable> ProgressChildren() => [];
    public virtual float GetProgress() => (State >= StateEnum.Completed).ToInt();
    public virtual void UpdateProgress()
    {
        if (State >= StateEnum.Completed)
        {
            if (1 > Progress)
            {
                Progress = 1;
                Achievement.UpdateProgress();
            }
            return;
        }
        float oldProgress = Progress;
        Progress = GetProgress();
        if (oldProgress != Progress)
        {
            Achievement.UpdateProgress();
        }
    }
    #endregion

    #region 状态
    public enum StateEnum
    {
        Disabled = -1,
        Idle,
        Completed,
        Closed
    }
    public StateEnum State;

    #region 完成
    [Obsolete("使用 State 代替", false)]
    public bool Completed => State == StateEnum.Completed;
    public static event Action<Requirement>? OnCompleteStatic;
    public event Action? OnComplete;
    /// <summary>
    /// 一般通过在监听时调用 CompleteSafe 以完成条件
    /// </summary>
    public void CompleteSafe()
    {
        if (State is StateEnum.Completed or StateEnum.Disabled)
        {
            return;
        }
        State = StateEnum.Completed;
        Complete();
        OnCompleteStatic?.Invoke(this);
        OnComplete?.Invoke();
    }
    protected virtual void Complete() { }
    #endregion

    #region 关闭
    public static event Action<Requirement>? OnCloseStatic;
    public event Action? OnClose;
    public virtual bool CloseCondition() => true;
    protected virtual void CloseHook()
    {
        OnStart += ToDo(DoIf, Achievement.State == Achievement.StateEnum.Completed, CloseSafe);
    }
    public virtual bool EndListenOnClose => true;
    public void CloseSafe()
    {
        if (State is not StateEnum.Idle)
        {
            return;
        }
        State = StateEnum.Closed;
        Close();
        OnCloseStatic?.Invoke(this);
        OnClose?.Invoke();
    }
    protected virtual void Close() { }
    #endregion

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

    public virtual bool BeginListenCondition()
    {
        return ListenType switch
        {
            ListenTypeEnum.OnAchievementUnlocked => Achievement.State.IsUnlocked(),
            ListenTypeEnum.OnPageUnlocked => Achievement.Page.State >= AchievementPage.StateEnum.Unlocked,
            ListenTypeEnum.OnStart => true,
            _ => false,
        };
    }
    protected virtual void BeginListenHook()
    {
        OnStart += TryBeginListen;
        switch (ListenType)
        {
        case ListenTypeEnum.OnAchievementUnlocked:
            Achievement.OnUnlock += TryBeginListen;
            break;
        case ListenTypeEnum.OnPageUnlocked:
            Achievement.Page.OnUnlock += TryBeginListen;
            break;
        }
    }
    protected virtual void EndListenHook()
    {
        OnReset += EndListenSafe;
        OnComplete += EndListenSafe;
        if (EndListenOnClose)
        {
            OnClose += EndListenSafe;
        }
    }
    public virtual void TryBeginListen()
    {
        if (BeginListenCondition())
        {
            BeginListenSafe();
        }
    }
    public static event Action<Requirement>? OnBeginListenStatic;
    public event Action? OnBeginListen;
    public static event Action<Requirement>? OnEndListenStatic;
    public event Action? OnEndListen;
    public void BeginListenSafe()
    {
        if (Listening)
        {
            return;
        }
        Listening = true;
        BeginListen();
        OnBeginListenStatic?.Invoke(this);
        OnBeginListen?.Invoke();
    }
    public void EndListenSafe()
    {
        if (!Listening)
        {
            return;
        }
        Listening = false;
        EndListen();
        OnEndListenStatic?.Invoke(this);
        OnEndListen?.Invoke();
    }
    protected virtual void BeginListen() { }
    protected virtual void EndListen() { }
    #endregion

    public override string ToString()
    {
        return $"{GetType().Name}: {nameof(State)}: {State}, {nameof(Listening)}: {Listening}";
    }

    /// <summary>
    /// 获取对应类型的条件的定义在哪个mod
    /// </summary>
    public static IReadOnlyDictionary<Type, Mod> DefinedMod => definedMod;
    protected static Dictionary<Type, Mod> definedMod = [];
    public virtual void Load(Mod mod)
    {
        definedMod.Add(GetType(), mod);
        InitializeByDefinedMod(mod);
    }
    protected void InitializeByDefinedMod() => InitializeByDefinedMod(null);
    protected virtual void InitializeByDefinedMod(Mod? mod)
    {
        mod ??= definedMod[GetType()];
        if (DisplayName.IsNone)
        {
            DisplayName = mod.GetLocalization($"Requirements.{GetType().Name}.DisplayName").WithFormatArgs(DisplayNameArgs);
        }
        if (Tooltip.IsNone)
        {
            Tooltip = mod.GetLocalization($"Requirements.{GetType().Name}.Tooltip").WithFormatArgs(TooltipArgs);
        }
        if (Texture.IsNone)
        {
            Texture = $"{mod.Name}/Assets/Textures/Requirements/{GetType().Name}";
        }
        if (Texture.IsNone)
        {
            Texture = $"{mod.Name}/Assets/Textures/Requirements/Default";
        }
        if (Texture.IsNone)
        {
            Texture = $"{mod.Name}/Assets/Textures/Default";
        }
    }

    public virtual void Unload() { }
}
