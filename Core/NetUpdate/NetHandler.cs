using Humanizer;
using ProgressSystem.Configs;
using System.IO;

namespace ProgressSystem.Core.NetUpdate;

public static class NetHandler
{
    public static void WriteMessageID(ModPacket packet, int id)
    {
        packet.Write((byte)id);
    }
    public static int ReadMessageID(BinaryReader reader)
    {
        return reader.ReadByte();
    }

    #region 0 无 None
    private static void HandleNone(BinaryReader reader, int whoAmI) { }
    #endregion

    #region 1 成就系统的网络同步 ManagerNetUpdate
    public static void ManagerNetUpdate()
    {
        var packet = ModInstance.GetPacket();
        WriteMessageID(packet, AchievementMessageID.ManagerNetUpdate);
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);
        BitWriter bitWriter = new();
        INetUpdate.WriteMessageList(writer, bitWriter);
        bitWriter.Flush(packet);
        packet.Write(stream.ToArray());
        packet.Send();
    }
    private static void HandleManagerNetUpdate(BinaryReader reader, int whoAmI)
        => INetUpdate.ReceiveMessageList(reader, new(reader));
    #endregion

    #region 2 玩家完成成就 PlayerCompleteAchievement
    public static void TryShowPlayerCompleteMessage(Achievement achievement, bool handle = false, int whoAmI = -1)
    {
        int pageIndex = AchievementManager.GetIndexOfPage(achievement.Page);
        if (pageIndex < 0)
        {
            return;
        }
        int achievementIndex = achievement.Page.GetAchievementIndex(achievement.FullName);
        if (achievementIndex < 0)
        {
            return;
        }
        void TrySendPacket()
        {
            if (ServerConfig.Instance.DontReceiveOtherPlayerCompleteAchievementMessage)
            {
                return;
            }
            if (pageIndex < 0 || achievementIndex < 0)
            {
                return;
            }
            var packet = ModInstance.GetPacket();
            WriteMessageID(packet, AchievementMessageID.PlayerCompleteAchievement);
            packet.Write((byte)whoAmI); // whoAmI == -1 时自动转化为 255
            packet.Write7BitEncodedInt(pageIndex);
            packet.Write7BitEncodedInt(achievementIndex);
            packet.Send(-1, whoAmI);
        }
        if (Main.netMode == NetmodeID.Server)
        {
            if (handle)
            {
                TrySendPacket();
            }
            return;
        }
        if (handle)
        {
            if (!ClientConfig.Instance.DontShowAnyAchievementMessage)
            {
                var player = Main.player.GetS(whoAmI);
                if (player == null || !player.active)
                {
                    return;
                }
                ModInstance.GetLocalizedValue("Messages.OtherPlayerCompleteAchievement").FormatWith(player.name, achievement.DisplayName.Value);
            }
            return;
        }
        if (!ClientConfig.Instance.DontShowAnyAchievementMessage)
        {
            ModInstance.GetLocalizedValue("Messages.LocalPlayerCompleteAchievement").FormatWith(achievement.DisplayName.Value);
        }
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            return;
        }
        TrySendPacket();
    }
    private static void HandlePlayerCompleteAchievement(BinaryReader reader, int whoAmI)
    {
        whoAmI = reader.ReadByte();
        int pageIndex = reader.Read7BitEncodedInt();
        int achievementIndex = reader.Read7BitEncodedInt();
        var page = AchievementManager.GetPageByIndex(pageIndex);
        var achievement = page?.GetAchievementByIndexS(achievementIndex);
        if (achievement == null)
        {
            return;
        }
        TryShowPlayerCompleteMessage(achievement, true, whoAmI);
    }
    #endregion

    #region 3 进入世界时同步数据 SyncAchievementDataOnEnterWorld
    public static void SyncAchievementDataOnEnterWorld(int sendTo = -1)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            return;
        }
        var packet = ModInstance.GetPacket();
        WriteMessageID(packet, AchievementMessageID.SyncAchievementDataOnEnterWorld);
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);
        BitWriter bitWriter = new();
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            INetUpdate.WriteMessageFromEnteringPlayerList(writer, bitWriter);
        }
        else
        {
            INetUpdate.WriteMessageToEnteringPlayerList(writer, bitWriter);
        }
        bitWriter.Flush(packet);
        packet.Write(stream.ToArray());
        packet.Send(sendTo);
    }
    private static void HandleSyncAchievementDataOnEnterWorld(BinaryReader reader, int whoAmI)
    {
        // 客户端接收数据
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            INetUpdate.ReceiveDataToEnteringPlayerList(reader, new(reader));
            return;
        }

        // 服务器接收数据并再向客户端发送数据
        INetUpdate.ReceiveDataFromEnteringPlayerList(reader, new(reader));
        SyncAchievementDataOnEnterWorld(whoAmI);
    }
    #endregion

    public delegate void HandlerDelegate(BinaryReader reader, int whoAmI);

    #region Handlers
    public static HandlerDelegate[] Handlers = [
        /* 0  */HandleNone,
        /* 1  */HandleManagerNetUpdate,
        /* 2  */HandlePlayerCompleteAchievement,
        /* 3  */HandleSyncAchievementDataOnEnterWorld,
    ];
    #endregion
}
