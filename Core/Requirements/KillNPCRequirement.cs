using ProgressSystem.GameEvents;
using Terraria.Localization;

namespace ProgressSystem.Core.Requirements;

// TODO: NPC NetID
public class KillNPCRequirement : Requirement
{
    public int NPCType;
    public int Count;
    public Func<NPC, bool>? Condition;
    public LocalizedText? ConditionDescription;
    
    public int CountNow;

    public KillNPCRequirement(int npcType, int count = 1) : this(npcType, null, null, count) { }
    public KillNPCRequirement(Func<NPC, bool> condition, LocalizedText conditionDescription, int count = 1) : this(0, condition, conditionDescription, count) { }
    protected KillNPCRequirement(int npcType, Func<NPC, bool>? condition, LocalizedText? conditionDescription, int count) : base(ListenTypeEnum.OnStart)
    {
        NPCType = npcType;
        Condition = condition;
        ConditionDescription = conditionDescription;
        Count = count;
    }
    protected KillNPCRequirement() : base() { }

    protected override object?[] DisplayNameArgs => [ Count + " " + (NPCType > 0 ? SampleNPC(NPCType).TypeName : ConditionDescription?.Value ?? "?")];

    public override void Reset()
    {
        base.Reset();
        CountNow = 0;
    }

    public override void SaveDataInPlayer(TagCompound tag)
    {
        base.SaveDataInPlayer(tag);
        tag.SetWithDefault("countNow", CountNow);
    }
    public override void LoadDataInPlayer(TagCompound tag)
    {
        base.LoadDataInPlayer(tag);
        tag.GetWithDefault("countNow", out CountNow);
    }
    public override void SaveStaticData(TagCompound tag)
    {
        base.SaveStaticData(tag);
        if (ShouldSaveStaticData)
        {
            tag.SetWithDefault("NPCType", NPCType);
            tag.SetWithDefault("Count", Count);
        }
    }
    public override void LoadStaticData(TagCompound tag)
    {
        base.LoadStaticData(tag);
        if (ShouldSaveStaticData)
        {
            tag.GetWithDefault("NPCType", out NPCType);
            tag.GetWithDefault("Count", out Count);
        }
    }

    protected override void BeginListen()
    {
        base.BeginListen();
        GEListener.OnLocalPlayerKillNPC += ListenKillNPC;
        DoIf(CountNow >= Count, CompleteSafe);
    }
    protected override void EndListen()
    {
        base.EndListen();
        GEListener.OnLocalPlayerKillNPC -= ListenKillNPC;
    }
    private void ListenKillNPC(NPC npc)
    {
        if ((NPCType > 0 && npc.type != NPCType) || Condition?.Invoke(npc) == false)
        {
            return;
        }
        DoIf((CountNow += 1) >= Count, CompleteSafe);
    }
}

