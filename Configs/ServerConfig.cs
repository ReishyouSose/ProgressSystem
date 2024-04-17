using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace ProgressSystem.Configs;

public class ServerConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ServerSide;
    public static ServerConfig Instance = null!;
    public override void OnLoaded()
    {
        Instance = this;
    }

    [Range(0, 600), DefaultValue(2)]
    public int NetUpdateFrequency { get => netUpdateFrequency.WithMin(0); set => netUpdateFrequency = value; }
    private int netUpdateFrequency;

    public bool DontReceiveOtherPlayerCompleteAchievementMessage { get; set; }
}
