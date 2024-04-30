using ProgressSystem.Common.Configs;
using ProgressSystem.Core.Interfaces;
using ProgressSystem.Core.NetUpdate;
using ProgressSystem.Core.StaticData;
using System.IO;
using Terraria.Localization;

namespace ProgressSystem.Core.Rewards;

public abstract class Reward : ILoadable, IWithStaticData, INetUpdate, IAchievementNode
{
    public Achievement Achievement = null!;
    public TextGetter DisplayName;
    public TextGetter Tooltip;
    public Texture2DGetter Texture;
    protected virtual object?[] DisplayNameArgs => [];
    protected virtual object?[] TooltipArgs => [];

    public virtual bool ReportDetails(out string details)
    {
        details = "";
        return false;
    }

    #region 状态
    public enum StateEnum
    {
        Disabled = -1,
        Locked = 0,
        Unlocked = 1,
        Receiving = 2,
        Received = 3,
        Closed = 4
    }
    public StateEnum State { get; protected set; }

    #region 解锁
    public static Action<Reward>? OnUnlockStatic;
    public Action? OnUnlock;
    public void TryUnlock()
    {
        if (State == StateEnum.Locked && Achievement.State.IsCompleted())
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
    #endregion

    #region 领取
    /// <summary>
    /// <br/>在 <see cref="ReceiveSafe"/> 中是否在调用 <see cref="Receive"/> 后直接改变 <see cref="State"/>
    /// <br/>若重写为 false 则需要在 <see cref="Receive"/> 中自己设置
    /// <br/><see cref="State"/> 为 <see cref="StateEnum.Receiving"/> 或 <see cref="StateEnum.Received"/>
    /// <br/>(一点都没有领取可以不设置)
    /// </summary>
    protected virtual bool AutoAssignReceived => true;

    public static event Action<Reward>? OnStartReceivedStatic;
    public event Action? OnStartReceived;
    public static event Action<Reward>? OnTotallyReceivedStatic;
    public event Action? OnTotallyReceived;
    /// <summary>
    /// 尝试领取奖励 (当成就完成时)
    /// </summary>
    public void TryReceive()
    {
        if (Achievement.State.IsCompleted())
        {
            ReceiveSafe();
        }
    }
    /// <summary>
    /// 获取奖励
    /// 不会重复领取
    /// </summary>
    public void ReceiveSafe()
    {
        bool unlock = State == StateEnum.Unlocked;
        bool receiving = State == StateEnum.Receiving;
        if (!unlock && !receiving)
        {
            return;
        }
        Receive();
        if (AutoAssignReceived)
        {
            State = StateEnum.Received;
        }
        if (unlock && State is StateEnum.Receiving or StateEnum.Received)
        {
            OnStartReceivedStatic?.Invoke(this);
            OnStartReceived?.Invoke();
        }
        if (State == StateEnum.Received)
        {
            OnTotallyReceivedStatic?.Invoke(this);
            OnTotallyReceived?.Invoke();
        }
    }
    /// <summary>
    /// <br/>获取奖励
    /// <br/>若可能不能直接领完需重写 <see cref="AutoAssignReceived"/> 为 false
    /// </summary>
    protected abstract void Receive();
    #endregion

    #region 关闭
    public static event Action<Reward>? OnCloseStatic;
    public event Action? OnClose;
    public void CloseSafe()
    {
        if (State is StateEnum.Disabled or StateEnum.Closed)
        {
            return;
        }

        Close();
        OnCloseStatic?.Invoke(this);
        OnClose?.Invoke();
    }
    protected virtual void Close()
    {
        State = StateEnum.Closed;
    }
    #endregion

    #region 禁用
    public static Action<Reward>? OnDisableStatic;
    public Action? OnDisable;
    public void DisableSafe()
    {
        if (State == StateEnum.Disabled)
        {
            return;
        }
        Disable();
        OnDisableStatic?.Invoke(this);
        OnDisable?.Invoke();
    }
    protected virtual void Disable()
    {
        State = StateEnum.Disabled;
    }
    #endregion

    public bool IsReceiving() => State == StateEnum.Receiving;
    public bool IsReceived() => State == StateEnum.Received;

    #endregion

    #region 数据存取
    public virtual void SaveDataInPlayer(TagCompound tag)
    {
        tag.SetWithDefault("State", State.ToString(), StateEnum.Locked.ToString());
    }
    public virtual void LoadDataInPlayer(TagCompound tag)
    {
        State = Enum.TryParse(tag.GetWithDefault("State", StateEnum.Locked.ToString()), out StateEnum state) ? state : StateEnum.Locked;
    }
    public virtual void SaveDataInWorld(TagCompound tag) { }
    public virtual void LoadDataInWorld(TagCompound tag) { }

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
        if (tag.TryGet<string>("Texture", out var textureString))
        {
            Texture = textureString;
        }
    }
    #endregion

    #region 多人同步
    protected bool _netUpdate;
    public bool NetUpdate { get => _netUpdate; set => DoIf(_netUpdate = value, AchievementManager.SetNeedNetUpdate); }
    public virtual void WriteMessageFromServer(BinaryWriter writer, BitWriter bitWriter) { }
    public virtual void ReceiveMessageFromServer(BinaryReader reader, BitReader bitReader) { }
    public virtual void WriteMessageFromClient(BinaryWriter writer, BitWriter bitWriter) { }
    public virtual void ReceiveMessageFromClient(BinaryReader reader, BitReader bitReader) { }
    #endregion

    #region 重置与开始
    public bool Repeatable { get; set; } = true;
    void IAchievementNode.Reset()
    {
        if (Repeatable || !Achievement.InRepeat)
        {
            Reset();
        }
    }
    public static Action<Reward>? OnResetStatic;
    public Action? OnReset;
    public virtual void Reset()
    {
        State = StateEnum.Locked;
        OnResetStatic?.Invoke(this);
        OnReset?.Invoke();
    }
    public static Action<Reward>? OnStartStatic;
    public Action? OnStart;
    public virtual void Start()
    {
        OnStartStatic?.Invoke(this);
        OnStart?.Invoke();
    }
    #endregion

    public virtual void PostInitialize()
    {
        OnStart += TryUnlock;
        Achievement.OnComplete += TryUnlock;
    }
    public virtual void Initialize(Achievement achievement)
    {
        Achievement = achievement;
        AchievementManager.DoAfterPostSetup(InitializeByDefinedMod);
    }
    protected virtual void InitializeByDefinedMod() => InitializeByDefinedMod(null);
    protected virtual void InitializeByDefinedMod(Mod? mod)
    {
        mod ??= definedMod[GetType()];
        if (DisplayName.IsNone)
        {
            DisplayName |= mod.GetLocalization($"Rewards.{GetType().Name}.DisplayName").WithFormatArgs(DisplayNameArgs);
        }
        if (DisplayName.IsNone)
        {
            Tooltip = mod.GetLocalization($"Rewards.{GetType().Name}.Tooltip").WithFormatArgs(TooltipArgs);
        }
        Texture |= $"{mod.Name}/Assets/Textures/Rewards/{GetType().Name}";
        Texture |= $"{mod.Name}/Assets/Textures/Rewards/Default";
        Texture |= $"{mod.Name}/Assets/Textures/Default";
    }
    public virtual IEnumerable<ConstructInfoTable<Reward>> GetConstructInfoTables()
    {
        ConstructInfoTable<Achievement>.TryAutoCreate<Reward>(GetType(), null, out var constructors);
        return constructors;
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

    public virtual void Unload() { }
}
