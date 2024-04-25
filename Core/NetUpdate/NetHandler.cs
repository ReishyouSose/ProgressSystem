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

    public static void HandleNone(BinaryReader reader, int whoAmI) { }
    public static void ManagerNetUpdate()
    {
        var packet = ModInstance.GetPacket();
        WriteMessageID(packet, AchievementMessageID.ManagerNetUpdate);
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);
        BitWriter bitWriter = new();
        ((INetUpdate)AchievementManager.Instance).WriteMessageTree(writer, bitWriter);
        bitWriter.Flush(packet);
        packet.Write(stream.ToArray());
        packet.Send();
    }
    public static void HandleManagerNetUpdate(BinaryReader reader, int whoAmI)
    {
        BitReader bitReader = new(reader);
        ((INetUpdate)AchievementManager.Instance).ReceiveMessageTree(reader, bitReader);
    }
    public static void TryShowPlayerCompleteMessage(Achievement achievement, bool handle = false, int whoAmI = -1)
    {
        int pageIndex = AchievementManager.GetIndexOfPage(achievement.Page);
        if (pageIndex < 0)
        {
            return;
        }
        int achievementIndex = achievement.Page.Achievements.GetIndexByKey(achievement.FullName);
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
            if (whoAmI == -1)
            {
                whoAmI = 255;
            }
            WriteMessageID(packet, AchievementMessageID.PlayerCompleteAchievement);
            packet.Write((byte)whoAmI);
            packet.Write7BitEncodedInt(pageIndex);
            packet.Write7BitEncodedInt(achievementIndex);
            packet.Send();
        }
        if (Main.netMode == NetmodeID.Server)
        {
            if (handle)
            {
                TrySendPacket();
            }
            return;
        }
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
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
                // TODO; 本地化
                Main.NewText($"{player.name}已完成成就{achievement.DisplayName.Value}");
            }
            return;
        }
        if (!ClientConfig.Instance.DontShowAnyAchievementMessage)
        {
            // TODO; 本地化
            Main.NewText($"成就{achievement.DisplayName.Value}已完成");
        }
        TrySendPacket();
    }
    public static void HandlePlayerCompleteAchievement(BinaryReader reader, int whoAmI)
    {
        whoAmI = reader.ReadByte();
        int pageIndex = reader.Read7BitEncodedInt();
        int achievementIndex = reader.Read7BitEncodedInt();
        var page = AchievementManager.GetPageByIndex(pageIndex);
        var achievement = page?.Achievements.GetValueByIndexS(achievementIndex);
        if (achievement == null)
        {
            return;
        }
        TryShowPlayerCompleteMessage(achievement, true, whoAmI);
    }

    public delegate void HandlerDelegate(BinaryReader reader, int whoAmI);
#pragma warning disable IDE0300 // 简化集合初始化
    public static HandlerDelegate[] Handlers = {
        /* 0  */HandleNone,
        /* 1  */HandleManagerNetUpdate,
        /* 2  */HandlePlayerCompleteAchievement,
    };
#pragma warning restore IDE0300 // 简化集合初始化
}
