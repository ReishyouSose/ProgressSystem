using Microsoft.Xna.Framework.Input;
using ProgressSystem.UIEditor;
using Terraria.GameInput;

namespace ProgressSystem.System
{
    public class PSPlayer : ModPlayer
    {
        public static PSPlayer Instance { get; private set; }
        public PSPlayer() => Instance = this;
        public static ModKeybind check;
        public int maxLife;
        public int maxMana;
        public int defense;
        public float moveSpeed;
        public float moveAccel;
        public float damage;
        public float endurance;
        public int crit;
        public override void Load()
        {
            check = KeybindLoader.RegisterKeybind(Mod, "check", Keys.K);
        }
        public override void ResetEffects()
        {
            /*maxLife = 0;
            maxMana = 0;
            defense = 0;
            moveSpeed = 0;
            moveAccel = 0;
            damage = 0;
            endurance = 0;
            crit = 0;*/
        }
        public override void UpdateEquips()
        {
            Player.statLifeMax2 += maxLife;
            Player.statManaMax2 += maxMana;
            Player.statDefense += defense;
            Player.moveSpeed += moveSpeed;
            Player.runAcceleration += moveAccel;
            Player.GetDamage(DamageClass.Generic) += damage;
            Player.endurance += endurance;
            Player.GetCritChance(DamageClass.Generic) += crit;
        }
        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (check.JustPressed)
            {
                GEEditor.Ins.OnInitialization();
            }
        }
    }
}
