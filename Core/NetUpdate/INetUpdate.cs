using System.IO;

namespace ProgressSystem.Core.NetUpdate;

public interface INetUpdate
{
    /// <summary>
    /// 现在是否需要同步
    /// </summary>
    bool NetUpdate { get; set; }
    /// <summary>
    /// 是否可能会同步
    /// </summary>
    bool WouldNetUpdate => false;
    IEnumerable<INetUpdate>? GetNetUpdateChildren() => null;
    
    /// <summary>
    /// 服务器向客户端发送数据
    /// </summary>
    void WriteMessageFromServer(BinaryWriter writer, BitWriter bitWriter) { }
    /// <summary>
    /// 客户端从服务器接收数据
    /// </summary>
    void ReceiveMessageFromServer(BinaryReader reader, BitReader bitReader) { }
    
    /// <summary>
    /// 客户端向服务器发送数据
    /// </summary>
    void WriteMessageFromClient(BinaryWriter writer, BitWriter bitWriter) { }
    /// <summary>
    /// 服务器从客户端接收数据
    /// </summary>
    void ReceiveMessageFromClient(BinaryReader reader, BitReader bitReader) { }

    /// <summary>
    /// 在进入世界时服务器向客户端发送数据
    /// </summary>
    void WriteMessageToEnteringPlayer(BinaryWriter writer, BitWriter bitWriter) => WriteMessageFromServer(writer, bitWriter);
    /// <summary>
    /// 在进入世界时客户端从服务器接收数据
    /// </summary>
    void ReceiveMessageToEnteringPlayer(BinaryReader reader, BitReader bitReader) => ReceiveMessageFromServer(reader, bitReader);

    /// <summary>
    /// 在进入世界时客户端向服务器发送数据
    /// </summary>
    void WriteMessageFromEnteringPlayer(BinaryWriter writer, BitWriter bitWriter) => WriteMessageFromClient(writer, bitWriter);
    /// <summary>
    /// 在进入世界时服务器从客户端接收数据
    /// </summary>
    void ReceiveMessageFromEnteringPlayer(BinaryReader reader, BitReader bitReader) => ReceiveMessageFromClient(reader, bitReader);

    public static void InitializeNodeList(INetUpdate root)
    {
        if (nodeList != null)
        {
            return;
        }
        nodeList = [];
        Stack<IEnumerator<INetUpdate>> stack = [];
        if (root.WouldNetUpdate)
        {
            nodeList.Add(root);
        }
        var rootChildren = root.GetNetUpdateChildren();
        if (rootChildren != null && rootChildren.Any())
        {
            stack.Push(rootChildren.GetEnumerator());
        }
        while(stack.Count > 0)
        {
            var peek = stack.Peek();
            if (!peek.MoveNext())
            {
                stack.Pop();
                continue;
            }
            var current = peek.Current;
            if (current.WouldNetUpdate)
            {
                nodeList.Add(current);
            }
            var children = current.GetNetUpdateChildren();
            if (children != null && children.Any())
            {
                stack.Push(children.GetEnumerator());
            }
        }
    }
    public static IReadOnlyList<INetUpdate>? NodeList => nodeList;
    public static IReadOnlyList<INetUpdate> NodeListS
    {
        get
        {
            if (nodeList == null)
            {
                InitializeNodeList(AchievementManager.Instance);
            }
            return nodeList!;
        }
    }
    private static List<INetUpdate>? nodeList;

    /// <summary>
    /// 需要先判断 <see cref="Main.netMode"/> 不为 <see cref="NetmodeID.SinglePlayer"/> 才能使用
    /// </summary>
    public static void WriteMessageList(BinaryWriter writer, BitWriter bitWriter)
    {
        if (Main.netMode == NetmodeID.Server)
        {
            WriteMessageFromServerList(writer, bitWriter);
        }
        else
        {
            WriteMessageFromClientList(writer, bitWriter);
        }
    }
    public static void WriteMessageFromServerList(BinaryWriter writer, BitWriter bitWriter)
    {
        var list = NodeListS;
        for (int i = 0; i < list.Count; ++i)
        {
            var item = list[i];
            if (item.NetUpdate)
            {
                item.NetUpdate = false;
                writer.Write7BitEncodedInt(i + 1);
                item.WriteMessageFromServer(writer, bitWriter);
            }
        }
        writer.Write7BitEncodedInt(0);
    }
    public static void WriteMessageFromClientList(BinaryWriter writer, BitWriter bitWriter)
    {
        var list = NodeListS;
        for (int i = 0; i < list.Count; ++i)
        {
            var item = list[i];
            if (item.NetUpdate)
            {
                item.NetUpdate = false;
                writer.Write7BitEncodedInt(i + 1);
                item.WriteMessageFromClient(writer, bitWriter);
            }
        }
        writer.Write7BitEncodedInt(0);
    }
    public static void WriteMessageToEnteringPlayerList(BinaryWriter writer, BitWriter bitWriter)
    {
        NodeListS.ForeachDo(i => i.WriteMessageToEnteringPlayer(writer, bitWriter));
    }
    public static void WriteMessageFromEnteringPlayerList(BinaryWriter writer, BitWriter bitWriter)
    {
        NodeListS.ForeachDo(i => i.WriteMessageFromEnteringPlayer(writer, bitWriter));
    }

    /// <summary>
    /// 需要先判断 <see cref="Main.netMode"/> 不为 <see cref="NetmodeID.SinglePlayer"/> 才能使用
    /// </summary>
    public static void ReceiveMessageList(BinaryReader reader, BitReader bitReader)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            ReceiveMessageFromServerList(reader, bitReader);
        }
        else
        {
            ReceiveMessageFromClientList(reader, bitReader);
        }
    }
    public static void ReceiveMessageFromServerList(BinaryReader reader, BitReader bitReader)
    {
        var list = NodeListS;
        int index;
        while((index = reader.Read7BitEncodedInt()) != 0)
        {
            list[index].ReceiveMessageFromServer(reader, bitReader);
        }
    }
    public static void ReceiveMessageFromClientList(BinaryReader reader, BitReader bitReader)
    {
        var list = NodeListS;
        int index;
        while((index = reader.Read7BitEncodedInt()) != 0)
        {
            list[index].ReceiveMessageFromClient(reader, bitReader);
        }
    }
    public static void ReceiveDataToEnteringPlayerList(BinaryReader reader, BitReader bitReader)
    {
        NodeListS.ForeachDo(i => i.ReceiveMessageToEnteringPlayer(reader, bitReader));
    }
    public static void ReceiveDataFromEnteringPlayerList(BinaryReader reader, BitReader bitReader)
    {
        NodeListS.ForeachDo(i => i.ReceiveMessageFromEnteringPlayer(reader, bitReader));
    }

#if NetUpdateTreeVersion
    /// <summary>
    /// 需要先判断 <see cref="Main.netMode"/> 不为 <see cref="NetmodeID.SinglePlayer"/> 才能使用
    /// </summary>
    public void WriteMessageTree(BinaryWriter writer, BitWriter bitWriter)
    {
        if (Main.netMode == NetmodeID.Server)
        {
            WriteMessageFromServerTree(writer, bitWriter);
        }
        else
        {
            WriteMessageFromClientTree(writer, bitWriter);
        }
    }
    public void WriteMessageFromServerTree(BinaryWriter writer, BitWriter bitWriter)
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
    public void WriteMessageFromClientTree(BinaryWriter writer, BitWriter bitWriter)
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
    public void SendDataToEnteringPlayerTree(BinaryWriter writer, BitWriter bitWriter)
    {
        int index = START_INDEX;
        SendDataToEnteringPlayerTreeInner(writer, bitWriter, ref index);
    }
    private void SendDataToEnteringPlayerTreeInner(BinaryWriter writer, BitWriter bitWriter, ref int index)
    {
        if (ShouldSendDataToEnteringPlayer)
        {
            writer.Write7BitEncodedInt(index);
            SendDataToEnteringPlayer(writer, bitWriter);
        }
        index += 1;
        foreach (var child in GetNetUpdateChildren())
        {
            child.SendDataToEnteringPlayerTreeInner(writer, bitWriter, ref index);
        }
        writer.Write7BitEncodedInt(TO_THE_END);
    }

    /// <summary>
    /// 需要先判断 <see cref="Main.netMode"/> 不为 <see cref="NetmodeID.SinglePlayer"/> 才能使用
    /// </summary>
    public void ReceiveMessageTree(BinaryReader reader, BitReader bitReader)
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
    public void ReceiveMessageFromServerTree(BinaryReader reader, BitReader bitReader)
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
    public void ReceiveMessageFromClientTree(BinaryReader reader, BitReader bitReader)
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
    public void ReceiveDataToEnteringPlayerTree(BinaryReader reader, BitReader bitReader)
    {
        int index = START_INDEX;
        int readIndex = reader.Read7BitEncodedInt();
        ReceiveDataToEnteringPlayerTreeInner(reader, bitReader, ref index, ref readIndex);
    }
    private void ReceiveDataToEnteringPlayerTreeInner(BinaryReader reader, BitReader bitReader, ref int index, ref int readIndex)
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
#endif
}
