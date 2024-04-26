using ProgressSystem.Common.Systems;
using ProgressSystem.UI.DeveloperMode;
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
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (KeyBinds.Check.JustPressed)
            {
                GEEditor.Ins.OnInitialization();
            }
        }
    }
}
