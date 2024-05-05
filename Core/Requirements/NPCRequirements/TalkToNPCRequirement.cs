using Terraria.GameContent;
using Terraria.Localization;

namespace ProgressSystem.Core.Requirements.NPCRequirements;

// TODO: Listen Talk to npc
public class TalkToNPCRequirement : Requirement
{
    public int NPCType;
    public Func<NPC, bool>? Condition;
    public LocalizedText? ConditionDescription;
    public TalkToNPCRequirement(int npcType) : this(npcType, null, null) { }
    public TalkToNPCRequirement(Func<NPC, bool> condition, LocalizedText conditionDescription) : this(0, condition, conditionDescription) { }
    protected TalkToNPCRequirement(int npcType, Func<NPC, bool>? condition, LocalizedText? conditionDescription) : this()
    {
        NPCType = npcType;
        Condition = condition;
        ConditionDescription = conditionDescription;
    }
    protected TalkToNPCRequirement() : base(ListenTypeEnum.OnStart)
    {
        Texture = new(() =>
        {
            if (NPCType <= 0)
            {
                return null;
            }
            Main.instance.LoadNPC(NPCType);
            return TextureAssets.Npc[NPCType].Value;
        });
        GetSourceRect = () =>
        {
            var type = NPCType;
            if (type <= 0)
            {
                return null;
            }
            if (type != dummyNPC?.type)
            {
                dummyNPC ??= new();
                dummyNPC.SetDefaults(type);
                dummyNPC.IsABestiaryIconDummy = true;
            }

            dummyNPC.FindFrame();
            return dummyNPC.frame;
        };
    }
    protected NPC? dummyNPC;
    protected override object?[] DisplayNameArgs => [NPCType > 0 ? SampleNPC(NPCType).TypeName : ConditionDescription?.Value ?? "?"];
    public override void SaveStaticData(TagCompound tag)
    {
        base.SaveStaticData(tag);
        if (!ShouldSaveStaticData)
        {
            return;
        }
        tag["NPCType"] = NPCType;
    }
    public override void LoadStaticData(TagCompound tag)
    {
        base.LoadStaticData(tag);
        if (!ShouldSaveStaticData)
        {
            return;
        }
        if (tag.TryGet("NPCType", out int npcType))
        {
            NPCType = npcType;
        }
    }
}

public class TalkToAnyNPCRequirement : TalkToNPCRequirement
{
    public TalkToAnyNPCRequirement(LocalizedText? conditionDescription = null)
        : base(npc => true, conditionDescription ?? ModInstance.GetLocalization("Requirements.TalkToAnyNPCRequirement.ConditionDescription")) { }
    protected TalkToAnyNPCRequirement() : base() { }
}
