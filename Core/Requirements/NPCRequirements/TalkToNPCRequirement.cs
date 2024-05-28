using Terraria.GameContent;
using Terraria.Localization;

namespace ProgressSystem.Core.Requirements.NPCRequirements;

public class TalkToNPCRequirement : Requirement
{
    #region 钩子
    static TalkToNPCRequirement()
    {
        On_Player.SetTalkNPC += Hook;
    }
    static void Hook(On_Player.orig_SetTalkNPC orig, Player self, int npcIndex, bool fromNet = false)
    {
        orig(self, npcIndex, fromNet);
        if (OnTalkToNPC != null && Main.npc.IndexInRange(npcIndex))
        {
            OnTalkToNPC(Main.npc[npcIndex]);
        }
    }
    public static Action<NPC>? OnTalkToNPC;
    #endregion

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

    #region 监听
    protected override void BeginListen()
    {
        OnTalkToNPC += ListenTalkToNPC;
    }
    protected override void EndListen()
    {
        OnTalkToNPC -= ListenTalkToNPC;
    }
    void ListenTalkToNPC(NPC npc)
    {
        if (Condition?.Invoke(npc) != false)
        {
            CompleteSafe();
        }
    }
    #endregion

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
