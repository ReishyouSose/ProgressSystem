using ProgressSystem.Common.Systems;
using ProgressSystem.Core.NetUpdate;
using ProgressSystem.Configs;
using ProgressSystem.UI.DeveloperMode;
using ProgressSystem.UI.PlayerMode;
using Terraria.GameInput;

namespace ProgressSystem.Common.Players
{
    public class PSPlayer : ModPlayer
    {
        public static PSPlayer? Instance { get; private set; }
        public int maxLife;
        public int maxMana;
        public int defense;
        public float moveSpeed;
        public float moveAccel;
        public float damage;
        public float endurance;
        public int crit;
        public override void OnEnterWorld()
        {
            Instance = this;
            NetHandler.SyncAchievementDataOnEnterWorld();
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (KeyBinds.Check.JustPressed)
            {
                if (ClientConfig.Instance.DeveloperMode)
                {
                    GEEditor.Ins.OnInitialization();
                    ProgressPanel.Ins.Info.IsVisible = false;
                }
                else
                {
                    ProgressPanel.Ins.OnInitialization();
                    GEEditor.Ins.Info.IsVisible = false;
                }
            }
        }
    }
}
