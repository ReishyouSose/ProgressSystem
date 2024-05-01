using System.IO;
using Terraria.Localization;

namespace ProgressSystem.Core.Requirements.ItemRequirements;

public abstract class ItemWorldRequirement : ItemRequirement
{
    protected int countToAdd;
    protected override MultiplayerTypeEnum MultiplayerTypeOverride => MultiplayerTypeEnum.World;

    public ItemWorldRequirement(int itemType, int count = 1) : base(itemType, count) { }
    public ItemWorldRequirement(Func<Item, bool> condition, LocalizedText conditionDescription, int count = 1) : base(condition, conditionDescription, count) { }
    protected ItemWorldRequirement() : base() { }

    public override void SaveDataInWorld(TagCompound tag)
    {
        base.SaveDataInWorld(tag);
        if (State != StateEnum.Completed)
        {
            tag.SetWithDefault("CountNow", CountNow);
        }
    }
    public override void LoadDataInWorld(TagCompound tag)
    {
        base.LoadDataInWorld(tag);
        if (State != StateEnum.Completed)
        {
            countNow = tag.GetWithDefault<int>("CountNow");
        }
        else
        {
            countNow = Count;
        }
        if (Main.netMode == NetmodeID.Server)
        {
            NetUpdate = true;
        }
    }

    public override bool WouldNetUpdateInitial => true;
    public override void WriteMessageFromServer(BinaryWriter writer, BitWriter bitWriter)
    {
        base.WriteMessageFromServer(writer, bitWriter);
        bool completed = CountNow >= Count;
        bitWriter.WriteBit(completed);
        if (!completed)
        {
            writer.Write7BitEncodedInt(CountNow);
        }
    }
    public override void ReceiveMessageFromServer(BinaryReader reader, BitReader bitReader)
    {
        base.ReceiveMessageFromServer(reader, bitReader);
        if (bitReader.ReadBit())
        {
            CountNow = Count;
        }
        else
        {
            CountNow = reader.Read7BitEncodedInt();
        }
    }
    public override void WriteMessageFromClient(BinaryWriter writer, BitWriter bitWriter)
    {
        base.WriteMessageFromClient(writer, bitWriter);
        writer.Write7BitEncodedInt(countToAdd);
        countToAdd = 0;
    }
    public override void ReceiveMessageFromClient(BinaryReader reader, BitReader bitReader)
    {
        base.ReceiveMessageFromClient(reader, bitReader);
        int cta = reader.Read7BitEncodedInt();
        if (cta > 0)
        {
            CountNow += cta;
            NetUpdate = true;
        }
    }
}
