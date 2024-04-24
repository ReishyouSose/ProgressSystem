using ProgressSystem.System;

namespace ProgressSystem.Core.Rewards
{
    public class PlayerStatReward : Reward
    {
        public static PSPlayer PSPlayer => PSPlayer.Instance;
        public override bool Repeatable => true;
        public int maxLife;
        public int maxMana;
        public int defense;
        public float moveSpeed;
        public float moveAccel;
        public float damage;
        public float endurance;
        public int crit;

        public override bool Receive()
        {
            PSPlayer.maxLife += maxLife;
            PSPlayer.maxMana += maxMana;
            PSPlayer.defense += defense;
            PSPlayer.moveSpeed += moveSpeed;
            PSPlayer.moveAccel += moveAccel;
            PSPlayer.damage += damage;
            PSPlayer.endurance += endurance;
            PSPlayer.crit += crit;
            return true;
        }
    }
}
