using Terraria.ModLoader.Config;

namespace ProgressSystem.Configs;

public class ClientConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;
    public static ClientConfig Instance = null!;
    public override void OnLoaded()
    {
        Instance = this;
    }

    public bool DontShowAnyAchievementMessage;
    public bool DontShowOtherPlayerCompleteAchievementMessage;
    public bool DeveloperMode;
}
