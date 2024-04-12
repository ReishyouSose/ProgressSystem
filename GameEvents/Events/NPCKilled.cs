﻿using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;

namespace ProgressSystem.GameEvents.Events;

public class NPCKilled : CountInt
{
    public int NetID { get; private set; }
    public static NPCKilled Create(int netID, int target = 1)
    {
        target = Math.Max(target, 1);
        NPCKilled @event = new()
        {
            NetID = netID,
            _target = target
        };
        return @event;
    }
    public static void SetUp(NPCKilled @event)
    {
        GEListener.OnNPCKilled += @event.TryComplete;
        @event.OnCompleted += e => GEListener.OnNPCKilled -= @event.TryComplete;
    }
    public static NPCKilled CreateAndSetUp(int netID, int target = 1)
    {
        target = Math.Max(target, 1);
        NPCKilled @event = Create(netID, target);
        SetUp(@event);
        return @event;
    }
    public override void Load(TagCompound tag)
    {
        if (tag.TryGet(nameof(IsCompleted), out bool isCompleted))
        {
            IsCompleted = isCompleted;
        }
        if (tag.TryGet(nameof(NetID), out int netID))
        {
            NetID = netID;
        }
        base.Load(tag);
    }
    public override void Save(TagCompound tag)
    {
        tag[nameof(IsCompleted)] = IsCompleted;
        tag[nameof(NetID)] = NetID;
        base.Save(tag);
    }
    public override (Texture2D, Rectangle?) DrawData()
    {
        Main.instance.LoadNPC(NetID);
        Texture2D tex = TextureAssets.Npc[ContentSamples.NpcsByNetId[NetID].type].Value;
        int frame = Main.npcFrameCount[NetID];
        return (tex, new Rectangle(0, 0, tex.Width, tex.Height / frame));
    }
    public void TryComplete(Player player, NPC npc)
    {
        if (npc.netID == NetID)
        {
            Increase(1);
        }
    }
}
