using ProgressSystem.Common.Players;

namespace ProgressSystem.Core.Rewards;

public class PlayerStatReward : Reward
{
    public PlayerStatReward()
    {
        Repeatable = false;
    }
    public int maxLife;
    public int maxMana;
    public int defense;
    public float moveSpeed;
    public float moveAccel;
    public float damage;
    public float endurance;
    public int crit;
    public override void Start()
    {
        if (Received)
        {
            Receive();
        }
    }
    protected override bool Receive()
    {
        var psPlayer = PSPlayer.Instance;
        if (psPlayer == null || !psPlayer.Player.active)
        {
            return false;
        }
        psPlayer.maxLife += maxLife;
        psPlayer.maxMana += maxMana;
        psPlayer.defense += defense;
        psPlayer.moveSpeed += moveSpeed;
        psPlayer.moveAccel += moveAccel;
        psPlayer.damage += damage;
        psPlayer.endurance += endurance;
        psPlayer.crit += crit;
        return true;
    }
}
