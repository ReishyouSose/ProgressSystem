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
        tag.SetWithDefault("Completed", Completed);
        if (!Completed)
        {
            tag.SetWithDefault("CountNow", CountNow);
        }
    }
    public override void LoadDataInWorld(TagCompound tag)
    {
        Completed = tag.GetWithDefault<bool>("Competed");
        if (!Completed)
        {
            CountNow = tag.GetWithDefault<int>("CountNow");
        }
        if (Main.netMode == NetmodeID.Server)
        {
            NetUpdate = true;
        }
    }
    public override void WriteMessageFromServer(BinaryWriter writer)
    {
        base.WriteMessageFromServer(writer);
        writer.Write7BitEncodedInt(CountNow);
    }
    public override void ReceiveMessageFromServer(BinaryReader reader)
    {
        base.ReceiveMessageFromServer(reader);
        CountNow = reader.Read7BitEncodedInt();
    }
    public override void WriteMessageFromClient(BinaryWriter writer)
    {
        base.WriteMessageFromClient(writer);
        writer.Write7BitEncodedInt(countToAdd);
    }
    public override void ReceiveMessageFromClient(BinaryReader reader)
    {
        base.ReceiveMessageFromClient(reader);
        int cta = reader.Read7BitEncodedInt();
        if (cta > 0)
        {
            CountNow += cta;
            NetUpdate = true;
        }
    }
}
