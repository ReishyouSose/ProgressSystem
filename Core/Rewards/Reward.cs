using ProgressSystem.Core.NetUpdate;
using ProgressSystem.Core.StaticData;
using System.IO;
using Terraria.Localization;

namespace ProgressSystem.Core.Rewards;

public abstract class Reward : ILoadable, IWithStaticData, INetUpdate
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

    #region 获取奖励
    public virtual bool Received { get; protected set; }

    /// <summary>
    /// 可否重复获取，多用于属性型奖励
    /// </summary>
    public virtual bool Repeatable => false;
    /// <summary>
    /// 获取奖励
    /// </summary>
    /// <returns>是否全部获取</returns>
    public abstract bool Receive();
    #endregion

    #region 数据存取
    public virtual void SaveDataInPlayer(TagCompound tag)
    {
        tag.SetWithDefault("Received", Received);
    }
    public virtual void LoadDataInPlayer(TagCompound tag)
    {
        if (tag.GetWithDefault("Received", out bool received))
        {
            Received = received;
        }
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
        Texture = tag.GetWithDefault<string>("Texture");
    }
    #endregion

    #region 多人同步
    protected bool _netUpdate;
    public bool NetUpdate { get => _netUpdate; set => DoIf(_netUpdate = value, AchievementManager.SetNeedNetUpdate); }
    public virtual void WriteMessageFromServer(BinaryWriter writer) { }
    public virtual void ReceiveMessageFromServer(BinaryReader reader) { }
    public virtual void WriteMessageFromClient(BinaryWriter writer) { }
    public virtual void ReceiveMessageFromClient(BinaryReader reader) { }
    #endregion

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
