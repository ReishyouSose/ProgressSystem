using ProgressSystem.Core.Listeners;

namespace ProgressSystem.Core.Requirements.PlayerRequirements;

public class ReachModBiomeRequirement : Requirement
{
    public string ModBiomeFullName;
    public ReachModBiomeRequirement(string modBiomeFullName) : base(ListenTypeEnum.OnAchievementUnlocked)
    {
        ModBiomeFullName = modBiomeFullName;
    }
    public ReachModBiomeRequirement() : this(string.Empty) { }
    public static ReachModBiomeRequirement Create<T>() where T : ModBiome => new(ModContent.GetInstance<T>().FullName);
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
            tag.GetWithDefault("ModBiomeFullName", out ModBiomeFullName, string.Empty);
        }
    }
}
