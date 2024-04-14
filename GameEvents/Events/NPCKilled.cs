using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;

namespace ProgressSystem.GameEvents.Events;

public class NPCKilled : CountInt
{
    /// <summary>
    /// The value may be -1. If it is -1, it is invalid
    /// </summary>
    public int Type { get; private set; }
    /// <summary>
    /// The value may be -1. If it is -1, it is invalid
    /// </summary>
    public int NetID { get; private set; }
    public static NPCKilled Create(int type, int netID, int target = 1)
    {
        target = Math.Max(target, 1);
        NPCKilled @event = new()
        {
            Type = type,
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
    public static NPCKilled CreateAndSetUp(int type, int netID, int target = 1)
    {
        target = Math.Max(target, 1);
        NPCKilled @event = Create(type, netID, target);
        SetUp(@event);
        return @event;
    }

    public override void Load(TagCompound tag)
    {
        if (tag.TryGet(nameof(IsCompleted), out bool isCompleted))
        {
            IsCompleted = isCompleted;
        }
        if (tag.TryGet(nameof(Type), out string type))
        {
            if (int.TryParse(type, out int num))
            {
                Type = num;
            }
            else
            {
                if (ModContent.TryFind(type, out ModNPC modNPC))
                {
                    Type = modNPC.Type;
                }
                else
                {
                    Type = -1;
                }
            }
        }
        if(tag.TryGet(nameof(NetID), out int netID))
        {
            NetID = netID;
        }
        else
        {
            NetID = -1;
        }
        base.Load(tag);
    }
    public override void Save(TagCompound tag)
    {
        tag[nameof(IsCompleted)] = IsCompleted;
        tag[nameof(Type)] = Type >= NPCID.Count ? NPCLoader.GetNPC(Type).FullName : Type;
        tag[nameof(NetID)] = NetID;
        base.Save(tag);
    }
    public override (Texture2D, Rectangle?) DrawData()
    {
        Main.instance.LoadNPC(NetID);
        Texture2D tex = TextureAssets.Npc[ContentSamples.NpcsByNetId[NetID].type].Value;
        int frame = Math.Max(Main.npcFrameCount[Type], 1);
        return (tex, new Rectangle(0, 0, tex.Width, tex.Height / frame));
    }
    public void TryComplete(Player player, NPC npc)
    {
        if (NetID != -1 && npc.netID != NetID)
        {
            return;
        }
        if (npc.type == Type)
        {
            Increase(1);
        }
    }
    public override IEnumerable<ConstructInfoTable<GameEvent>> GetConstructInfoTables()
    {
        var table = new ConstructInfoTable<GameEvent>(t =>
        {
            var e = t.GetEnumerator();
            e.MoveNext();
            int type = e.Current.GetValue<int>();
            e.MoveNext();
            int netID = e.Current.GetValue<int>();
            e.MoveNext();
            int target = e.Current.GetValue<int>();
            return Create(type, target);
        }, nameof(NPCKilled));
        table.AddEntry(new(typeof(int), "type"));
        table.AddEntry(new(typeof(int), "netID"));
        table.AddEntry(new(typeof(int), "target"));
        table.Close();
        yield return table;
        yield break;
    }
}
