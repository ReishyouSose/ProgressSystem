using System.IO;

namespace ProgressSystem.Core.NetUpdate;

public interface INetUpdate
{
    void WriteMessageFromServer(BinaryWriter writer, BitWriter bitWriter);
    void ReceiveMessageFromServer(BinaryReader reader, BitReader bitReader);
    void WriteMessageFromClient(BinaryWriter writer, BitWriter bitWriter);
    void ReceiveMessageFromClient(BinaryReader reader, BitReader bitReader);
    bool NetUpdate { get; set; }
    IEnumerable<INetUpdate> GetNetUpdateChildren() => [];

    /// <summary>
    /// 需要先判断 <see cref="Main.netMode"/> 不为 <see cref="NetmodeID.SinglePlayer"/> 才能使用
    /// </summary>
    void WriteMessageTree(BinaryWriter writer, BitWriter bitWriter)
    {
        if (Main.netMode == NetmodeID.Server)
        {
            WriteMessageFromServerTree(writer, bitWriter);
        }
        else
        {
            WriteMessageFromClient(writer, bitWriter);
        }
    }
    void WriteMessageFromServerTree(BinaryWriter writer, BitWriter bitWriter)
    {
        int index = START_INDEX;
        WriteMessageFromServerTreeInner(writer, bitWriter, ref index);
    }
    private void WriteMessageFromServerTreeInner(BinaryWriter writer, BitWriter bitWriter, ref int index)
    {
        if (NetUpdate)
        {
            NetUpdate = false;
            writer.Write7BitEncodedInt(index);
            WriteMessageFromServer(writer, bitWriter);
        }
        index += 1;
        foreach (var child in GetNetUpdateChildren())
        {
            child.WriteMessageFromServerTreeInner(writer, bitWriter, ref index);
        }
        writer.Write7BitEncodedInt(TO_THE_END);
    }
    void WriteMessageFromClientTree(BinaryWriter writer, BitWriter bitWriter)
    {
        int index = START_INDEX;
        WriteMessageFromClientTreeInner(writer, bitWriter, ref index);
    }
    private void WriteMessageFromClientTreeInner(BinaryWriter writer, BitWriter bitWriter, ref int index)
    {
        if (NetUpdate)
        {
            NetUpdate = false;
            writer.Write7BitEncodedInt(index);
            WriteMessageFromClient(writer, bitWriter);
        }
        index += 1;
        foreach (var child in GetNetUpdateChildren())
        {
            child.WriteMessageFromClientTreeInner(writer, bitWriter, ref index);
        }
        writer.Write7BitEncodedInt(TO_THE_END);
    }
    
    /// <summary>
    /// 需要先判断 <see cref="Main.netMode"/> 不为 <see cref="NetmodeID.SinglePlayer"/> 才能使用
    /// </summary>
    void ReceiveMessageTree(BinaryReader reader, BitReader bitReader)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            ReceiveMessageFromServerTree(reader, bitReader);
        }
        else
        {
            ReceiveMessageFromClientTree(reader, bitReader);
        }
    }
    void ReceiveMessageFromServerTree(BinaryReader reader, BitReader bitReader)
    {
        int index = START_INDEX;
        int readIndex = reader.Read7BitEncodedInt();
        ReceiveMessageFromServerTreeInner(reader, bitReader, ref index, ref readIndex);
    }
    private void ReceiveMessageFromServerTreeInner(BinaryReader reader, BitReader bitReader, ref int index, ref int readIndex)
    {
        if (readIndex == TO_THE_END)
        {
            return;
        }
        if (readIndex == index)
        {
            ReceiveMessageFromServer(reader, bitReader);
            readIndex = reader.Read7BitEncodedInt();
            if (readIndex == TO_THE_END)
            {
                return;
            }
        }
        index += 1;
        foreach (var child in GetNetUpdateChildren())
        {
            child.ReceiveMessageFromServerTreeInner(reader, bitReader, ref index, ref readIndex);
        }
    }
    void ReceiveMessageFromClientTree(BinaryReader reader, BitReader bitReader)
    {
        int index = START_INDEX;
        int readIndex = reader.Read7BitEncodedInt();
        ReceiveMessageFromClientTreeInner(reader, bitReader, ref index, ref readIndex);
    }
    private void ReceiveMessageFromClientTreeInner(BinaryReader reader, BitReader bitReader, ref int index, ref int readIndex)
    {
        if (readIndex == TO_THE_END)
        {
            return;
        }
        if (readIndex == index)
        {
            ReceiveMessageFromClient(reader, bitReader);
            readIndex = reader.Read7BitEncodedInt();
            if (readIndex == TO_THE_END)
            {
                return;
            }
        }
        index += 1;
        foreach (var child in GetNetUpdateChildren())
        {
            child.ReceiveMessageFromClientTreeInner(reader, bitReader, ref index, ref readIndex);
        }
    }

    const int TO_THE_END = 0;
    const int START_INDEX = 1;
}
