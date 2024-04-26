namespace ProgressSystem.Core.NetUpdate;

public static class AchievementMessageID
{
    public const int None = 0;
    public const int ManagerNetUpdate = 1;
    /// <summary>
    /// 对应参数: playerWai(byte), pageIndex(int7bit), achievementIndex(int7bit)
    /// </summary>
    public const int PlayerCompleteAchievement = 2;
    /// <summary>
    /// 玩家进入世界时同步数据
    /// </summary>
    public const int SyncAchievementDataOnEnterWorld = 3;

    /// <summary>
    /// <br/>如果这个值超过byte的范围了(2 ^ 8 = 256)需要修改
    /// <br/><see cref="NetHandler.WriteMessageID"/>和
    /// <br/><see cref="NetHandler.ReadMessageID"/>
    /// </summary>
    public const int Count = 3;
}
