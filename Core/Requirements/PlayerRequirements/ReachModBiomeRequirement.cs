using ProgressSystem.Core.Listeners;

namespace ProgressSystem.Core.Requirements.PlayerRequirements;

public class ReachModBiomeRequirement : Requirement
{
    protected string _modBiomeFullName = string.Empty;
    public string ModBiomeFullName
    {
        get => _modBiomeFullName;
        set {
            _modBiomeFullName = value;
            _modBiome = ModContent.TryFind<ModBiome>(_modBiomeFullName, out var biome) ? biome : null;
        }
    }
    protected ModBiome? _modBiome;
    public ModBiome? ModBiome
    {
        get => _modBiome;
        set
        {
            _modBiome = value;
            _modBiomeFullName = value?.FullName ?? string.Empty;
        }
    }

    public ReachModBiomeRequirement(ModBiome modBiome) : this() => ModBiome = modBiome;
    public ReachModBiomeRequirement(string modBiomeFullName) : this() => ModBiomeFullName = modBiomeFullName;
    public ReachModBiomeRequirement() : base(ListenTypeEnum.OnAchievementUnlocked) { }
    public static ReachModBiomeRequirement Create<T>() where T : ModBiome => new(ModContent.GetInstance<T>());
    protected override void BeginListen()
    {
        base.BeginListen();
        BiomeListener.OnReachNewModBiome += ListenerReachNewModBiome;
        if (BiomeListener.ModBiomeReached(ModBiomeFullName))
        {
            CompleteSafe();
        }
    }
    protected override void EndListen()
    {
        base.EndListen();
        BiomeListener.OnReachNewModBiome -= ListenerReachNewModBiome;
    }
    private void ListenerReachNewModBiome(ModBiome modBiome)
    {
        if (modBiome.FullName == ModBiomeFullName)
        {
            CompleteSafe();
        }
    }

    protected override object?[] DisplayNameArgs => [ModBiome?.DisplayName.Value ?? ModBiomeFullName];

    public override void SaveStaticData(TagCompound tag)
    {
        base.SaveStaticData(tag);
        if (ShouldSaveStaticData)
        {
            tag.SetWithDefault("ModBiomeFullName", ModBiomeFullName);
        }
    }
    public override void LoadStaticData(TagCompound tag)
    {
        base.LoadStaticData(tag);
        if (ShouldSaveStaticData)
        {
            if (tag.TryGet<string>("ModBiomeFullName", out var fullName))
            {
                ModBiomeFullName = fullName;
            }
        }
    }
}
