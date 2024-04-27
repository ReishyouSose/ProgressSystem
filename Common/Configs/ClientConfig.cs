using Terraria.ModLoader.Config;

namespace ProgressSystem.Common.Configs;

public class ClientConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;
    public static ClientConfig Instance = null!;
    public override void OnLoaded()
    {
        Instance = this;
    }

    public bool DontShowAnyAchievementMessage { get; set; }
    public bool DontShowOtherPlayerCompleteAchievementMessage { get; set; }
    public bool DeveloperMode { get; set; }
    public bool AutoReceive { get; set; }

    public enum AutoSelectRewardEnum
    {
        NotSelect,
        First,
        Random
    }
    [DrawTicks]
    public AutoSelectRewardEnum AutoSelectReward;
}
