
namespace ProgressSystem.Core.Listeners.Hooks;

/// <summary>
/// 本地玩家的统计数据
/// </summary>
internal class PlayerStatistics : ModPlayer
{
    public long TotalHealthLose { get; private set; }
    public long TotalManaConsumed { get; private set; }

    /// <summary>
    /// LocalPlayer 上的 PlayerStatistics
    /// </summary>
    public static PlayerStatistics? Ins;
    public override void OnEnterWorld()
    {
        base.OnEnterWorld();
        Ins = this;
    }
    static PlayerStatistics()
    {
        PlayerListener.OnLocalPlayerHurt += hurt =>
        {
            if (Ins == null)
            {
                return;
            }
            var damage = hurt.Damage.WithMax(Main.LocalPlayer.statLife);
            if (damage <= 0)
            {
                return;
            }
            Ins.TotalHealthLose += damage;
            PlayerListener.ListenLocalPlayerTotalHealthLoseChanged(Ins.TotalHealthLose);
        };
        PlayerListener.OnLocalPlayerConsumeMana += cost =>
        {
            if (Ins == null || cost <= 0)
            {
                return;
            }
            Ins.TotalManaConsumed += cost;
            PlayerListener.ListenLocalPlayerTotalManaConsumedChanged(Ins.TotalManaConsumed);
        };
    }
    public override void SaveData(TagCompound tag)
    {
        tag.SetWithDefault("TotalHealthLose", TotalHealthLose);
        tag.SetWithDefault("TotalManaConsumed", TotalManaConsumed);
    }
    public override void LoadData(TagCompound tag)
    {
        TotalHealthLose = tag.GetWithDefault<long>("TotalHealthLose");
        TotalManaConsumed = tag.GetWithDefault<long>("TotalManaConsumed");
    }
}
