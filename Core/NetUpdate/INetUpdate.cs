using System.IO;

namespace ProgressSystem.Core.NetUpdate;

public interface INetUpdate
{
    void WriteMessageFromServer(BinaryWriter writer);
    void ReceiveMessageFromServer(BinaryReader reader);
    void WriteMessageFromClient(BinaryWriter writer);
    void ReceiveMessageFromClient(BinaryReader reader);
    bool NetUpdate { get; set; }
    IEnumerable<INetUpdate> GetNetUpdateChildren() => [];

    /// <summary>
    /// 需要先判断 <see cref="Main.netMode"/> 不为 <see cref="NetmodeID.SinglePlayer"/> 才能使用
    /// </summary>
    void WriteMessageTree(BinaryWriter writer)
    {
        if (Main.netMode == NetmodeID.Server)
        {
            WriteMessageFromServerTree(writer);
        }
        else
        {
            WriteMessageFromClient(writer);
        }
    }
    void WriteMessageFromServerTree(BinaryWriter writer)
    {
        int index = START_INDEX;
        WriteMessageFromServerTreeInner(writer, ref index);
    }
    private void WriteMessageFromServerTreeInner(BinaryWriter writer, ref int index)
    {
        if (NetUpdate)
        {
            NetUpdate = false;
            writer.Write7BitEncodedInt(index);
            WriteMessageFromServer(writer);
        }
        index += 1;
        foreach (var child in GetNetUpdateChildren())
        {
            child.WriteMessageFromServerTreeInner(writer, ref index);
        }
        writer.Write7BitEncodedInt(TO_THE_END);
    }
    void WriteMessageFromClientTree(BinaryWriter writer)
    {
        int index = START_INDEX;
        WriteMessageFromClientTreeInner(writer, ref index);
    }
    private void WriteMessageFromClientTreeInner(BinaryWriter writer, ref int index)
    {
        if (NetUpdate)
        {
            NetUpdate = false;
            writer.Write7BitEncodedInt(index);
            WriteMessageFromClient(writer);
        }
        index += 1;
        foreach (var child in GetNetUpdateChildren())
        {
            child.WriteMessageFromClientTreeInner(writer, ref index);
        }
        writer.Write7BitEncodedInt(TO_THE_END);
    }
    
    /// <summary>
    /// 需要先判断 <see cref="Main.netMode"/> 不为 <see cref="NetmodeID.SinglePlayer"/> 才能使用
    /// </summary>
    void ReceiveMessageTree(BinaryReader reader)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            ReceiveMessageFromServerTree(reader);
        }
        else
        {
            ReceiveMessageFromClientTree(reader);
        }
    }
    void ReceiveMessageFromServerTree(BinaryReader reader)
    {
        int index = START_INDEX;
        int readIndex = reader.Read7BitEncodedInt();
        ReceiveMessageFromServerTreeInner(reader, ref index, ref readIndex);
    }
    private void ReceiveMessageFromServerTreeInner(BinaryReader reader, ref int index, ref int readIndex)
    {
        if (readIndex == TO_THE_END)
        {
            return;
        }
        if (readIndex == index)
        {
            ReceiveMessageFromServer(reader);
            readIndex = reader.Read7BitEncodedInt();
            if (readIndex == TO_THE_END)
            {
                return;
            }
        }
        index += 1;
        foreach (var child in GetNetUpdateChildren())
        {
            child.ReceiveMessageFromServerTreeInner(reader, ref index, ref readIndex);
        }
    }
    void ReceiveMessageFromClientTree(BinaryReader reader)
    {
        int index = START_INDEX;
        int readIndex = reader.Read7BitEncodedInt();
        ReceiveMessageFromClientTreeInner(reader, ref index, ref readIndex);
    }
    private void ReceiveMessageFromClientTreeInner(BinaryReader reader, ref int index, ref int readIndex)
    {
        if (readIndex == TO_THE_END)
        {
            return;
        }
        if (readIndex == index)
        {
            ReceiveMessageFromClient(reader);
            readIndex = reader.Read7BitEncodedInt();
            if (readIndex == TO_THE_END)
            {
                return;
            }
        }
        index += 1;
        foreach (var child in GetNetUpdateChildren())
        {
            child.ReceiveMessageFromClientTreeInner(reader, ref index, ref readIndex);
        }
    }

    const int TO_THE_END = 0;
    const int START_INDEX = 1;
}
