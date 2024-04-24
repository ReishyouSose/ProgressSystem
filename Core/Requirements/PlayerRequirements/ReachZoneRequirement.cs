using ProgressSystem.Core.Listeners;

namespace ProgressSystem.Core.Requirements.PlayerRequirements;

public class ReachZoneRequirement : Requirement
{
    public int ZoneId;
    public ReachZoneRequirement(int zoneId) : this()
    {
        ZoneId = zoneId;
    }
    public ReachZoneRequirement() : base(ListenTypeEnum.OnAchievementUnlocked) { }
    protected override void BeginListen()
    {
        base.BeginListen();
        BiomeListener.OnReachNewZone += ListenerReachNewZone;
        if (BiomeListener.ZoneReached(ZoneId))
        {
            CompleteSafe();
        }
    }
    protected override void EndListen()
    {
        base.EndListen();
        BiomeListener.OnReachNewZone -= ListenerReachNewZone;
    }
    private void ListenerReachNewZone(int zoneId)
    {
        if (ZoneId == zoneId)
        {
            CompleteSafe();
        }
    }

    public override void SaveStaticData(TagCompound tag)
    {
        base.SaveStaticData(tag);
        if (ShouldSaveStaticData)
        {
            tag.SetWithDefault("ZoneId", ZoneId);
        }
    }
    public override void LoadStaticData(TagCompound tag)
    {
        base.LoadStaticData(tag);
        if (ShouldSaveStaticData)
        {
            tag.GetWithDefault("ZoneId", out ZoneId);
        }
    }
}
