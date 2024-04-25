using ProgressSystem.Common.Systems;
using ProgressSystem.UIEditor;
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
        public override void SaveData(TagCompound tag)
        {
            tag.SetWithDefault("MaxLife", maxLife);
            tag.SetWithDefault("MaxMana", maxMana);
            tag.SetWithDefault("Defense", defense);
            tag.SetWithDefault("MoveSpeed", moveSpeed);
            tag.SetWithDefault("MoveAccel", moveAccel);
            tag.SetWithDefault("Damage", damage);
            tag.SetWithDefault("Endurance", endurance);
            tag.SetWithDefault("Crit", crit);
        }
        public override void LoadData(TagCompound tag)
        {
            tag.GetWithDefault("MaxLife", out maxLife);
            tag.GetWithDefault("MaxMana", out maxMana);
            tag.GetWithDefault("Defense", out defense);
            tag.GetWithDefault("MoveSpeed", out moveSpeed);
            tag.GetWithDefault("MoveAccel", out moveAccel);
            tag.GetWithDefault("Damage", out damage);
            tag.GetWithDefault("Endurance", out endurance);
            tag.GetWithDefault("Crit", out crit);
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
