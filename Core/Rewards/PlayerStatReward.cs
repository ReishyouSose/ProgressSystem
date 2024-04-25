using ProgressSystem.Common.Players;

namespace ProgressSystem.Core.Rewards;

public class PlayerStatReward : Reward
{
    public PlayerStatReward(int maxLife = 0, int maxMana = 0, int defense = 0, float moveSpeed = 0,
        float moveAccel = 0, float damage = 0, float endurance = 0, int crit = 0)
    {
        this.maxLife = maxLife;
        this.maxMana = maxMana;
        this.defense = defense;
        this.moveSpeed = moveSpeed;
        this.moveAccel = moveAccel;
        this.damage = damage;
        this.endurance = endurance;
        this.crit = crit;
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
