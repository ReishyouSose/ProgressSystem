using ProgressSystem.Common.Players;
using System.Text;

namespace ProgressSystem.Core.Rewards;

public partial class PlayerStatReward : Reward
{
    private readonly StringBuilder builder = new();
    public PlayerStatReward() : this(0) { }
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
    public int   maxLife  ;
    public int   maxMana  ;
    public int   defense  ;
    public float moveSpeed;
    public float moveAccel;
    public float damage   ;
    public float endurance;
    public int   crit     ;
    public override void Start()
    {
        if (State == StateEnum.Received)
        {
            Receive();
        }
    }
    protected override bool AutoAssignReceived => false;
    protected override void Receive()
    {
        var psPlayer = PSPlayer.Instance;
        if (psPlayer == null || !psPlayer.Player.active)
        {
            return;
        }
        psPlayer.maxLife += maxLife;
        psPlayer.maxMana += maxMana;
        psPlayer.defense += defense;
        psPlayer.moveSpeed += moveSpeed;
        psPlayer.moveAccel += moveAccel;
        psPlayer.damage += damage;
        psPlayer.endurance += endurance;
        psPlayer.crit += crit;
        State = StateEnum.Received;
        return;
    }
    public override bool ReportDetails(out string details)
    {
        builder.Clear();
        if (maxLife > 0)
            AppendStatText("最大生命值 " + maxLife);
        if (maxMana > 0)
            AppendStatText("最大法力值 " + maxMana);
        if (defense > 0)
            AppendStatText("防御力 " + defense);
        if (moveSpeed > 0)
            AppendStatText("移动速度% " + moveSpeed);
        if (moveAccel > 0)
            AppendStatText("加速度% " + moveAccel);
        if (damage > 0)
            AppendStatText("伤害加成% " + damage);
        if (endurance > 0)
            AppendStatText("伤害减免% " + endurance);
        if (crit > 0)
            AppendStatText("暴击率% " + crit);
        details = builder.ToString();
        if (details.EndsWith("\n"))
            details = details[..^1];
        return true;
    }
    public void AppendStatText(string text)
    {
        builder.Append(text);
        builder.AppendLine();
    }
    public override void Reset()
    {
        if (!Achievement.InRepeat)
        {
            base.Reset();
        }
    }
    public override void SaveStaticData(TagCompound tag)
    {
        base.SaveStaticData(tag);
        if (!ShouldSaveStaticData)
        {
            return;
        }
        tag.SetWithDefault("MaxLife"  , maxLife  );
        tag.SetWithDefault("MaxMana"  , maxMana  );
        tag.SetWithDefault("Defense"  , defense  );
        tag.SetWithDefault("MoveSpeed", moveSpeed);
        tag.SetWithDefault("MoveAccel", moveAccel);
        tag.SetWithDefault("Damage"   , damage   );
        tag.SetWithDefault("Endurance", endurance);
        tag.SetWithDefault("Crit"     , crit     );
    }
    public override void LoadStaticData(TagCompound tag)
    {
        base.LoadStaticData(tag);
        if (!ShouldSaveStaticData)
        {
            return;
        }
        if (tag.TryGet<int>("MaxLife"  , out var maxLife  )) { this.maxLife   = maxLife  ; }
        if (tag.TryGet<int>("MaxMana"  , out var maxMana  )) { this.maxMana   = maxMana  ; }
        if (tag.TryGet<int>("Defense"  , out var defense  )) { this.defense   = defense  ; }
        if (tag.TryGet<int>("MoveSpeed", out var moveSpeed)) { this.moveSpeed = moveSpeed; }
        if (tag.TryGet<int>("MoveAccel", out var moveAccel)) { this.moveAccel = moveAccel; }
        if (tag.TryGet<int>("Damage"   , out var damage   )) { this.damage    = damage   ; }
        if (tag.TryGet<int>("Endurance", out var endurance)) { this.endurance = endurance; }
        if (tag.TryGet<int>("Crit"     , out var crit     )) { this.crit      = crit     ; }
    }
}
