﻿using ProgressSystem.Core.Listeners;
using Terraria.GameContent;
using Terraria.Localization;

namespace ProgressSystem.Core.Requirements.NPCRequirements;

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
    protected KillNPCRequirement(int npcType, Func<NPC, bool>? condition, LocalizedText? conditionDescription, int count) : this()
    {
        NPCType = npcType;
        Condition = condition;
        ConditionDescription = conditionDescription;
        Count = count;
    }
    protected KillNPCRequirement() : base(ListenTypeEnum.OnStart)
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

    protected override object?[] DisplayNameArgs => [Count + " " + (NPCType > 0 ? SampleNPC(NPCType).TypeName : ConditionDescription?.Value ?? "?")];

    public override void Reset()
    {
        base.Reset();
        CountNow = 0;
    }

    public override void SaveDataInPlayer(TagCompound tag)
    {
        base.SaveDataInPlayer(tag);
        tag.SetWithDefault("CountNow", CountNow);
    }
    public override void LoadDataInPlayer(TagCompound tag)
    {
        base.LoadDataInPlayer(tag);
        tag.GetWithDefault("CountNow", out CountNow);
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
        if (NPCType == 0)
        {
            PlayerListener.OnLocalPlayerKillNPC.Any += ListenKillNPC;
        }
        else
        {
            PlayerListener.OnLocalPlayerKillNPC.Add(NPCType, ListenKillNPC);
        }
        DoIf(CountNow >= Count, CompleteSafe);
    }
    protected override void EndListen()
    {
        base.EndListen();
        if (NPCType == 0)
        {
            PlayerListener.OnLocalPlayerKillNPC.Any -= ListenKillNPC;
        }
        else
        {
            PlayerListener.OnLocalPlayerKillNPC.Remove(NPCType, ListenKillNPC);
        }
    }
    private void ListenKillNPC(NPC npc)
    {
        if (Condition?.Invoke(npc) == false)
        {
            return;
        }
        DoIf((CountNow += 1) >= Count, CompleteSafe);
    }
}

public class KillAnyNPCRequirement : KillNPCRequirement
{
    public KillAnyNPCRequirement(int count, LocalizedText? conditionDescription) :
        base(npc  => true, conditionDescription ?? ModInstance.GetLocalization("Requirements.KillAnyNPCRequirement.ConditionDescription"), count)
    { }
    public KillAnyNPCRequirement(int count = 1) : this(count, null) { }
    public KillAnyNPCRequirement(LocalizedText? conditionDescription) : this(1, conditionDescription) { }
    protected KillAnyNPCRequirement() : base() { }
}
