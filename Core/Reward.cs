namespace ProgressSystem.Core;

// TODO
public abstract class Reward
{
    public Achievement Achievement = null!;
    public virtual bool Received { get; protected set; }
    /// <summary>
    /// 获取奖励
    /// </summary>
    /// <returns>是否全部获取</returns>
    public abstract bool Receive();
    public virtual void SaveData(TagCompound tag)
    {
        tag.SetWithDefault("Received", Received);
    }
    public virtual void LoadData(TagCompound tag)
    {
        if (tag.GetWithDefault("Received", out bool received))
        {
            Received = received;
        }
    }
    public virtual void Initialize(Achievement achievement)
    {
        Achievement = achievement;
    }
}

public class EmptyReward : Reward
{
    public override bool Receive() => Received = true;
}

public class ItemReward(Item item) : Reward
{
    public override bool Received
    {
        get => leftStack <= 0;
        protected set
        {
            if (value)
            {
                leftStack = 0;
            }
        }
    }
    public Item Item = item;
    /// <summary>
    /// 剩余的个数 (有可能一次没领完)
    /// </summary>
    public int leftStack = item.stack;

    public ItemReward(int itemType, int stack = 1) : this(new(itemType, stack)) { }

    public override bool Receive()
    {
        if (leftStack <= 0)
        {
            return true;
        }
        Item item = Item.Clone();
        item.stack = leftStack;
        // TODO: Entity Source
        // TODO: 直接给玩家背包塞东西
        // Main.LocalPlayer.TryStackToInventory(item, null, false);
        // leftStack = item.stack;
        Main.LocalPlayer.QuickSpawnItem(null, item, item.stack);
        leftStack = 0;
        return leftStack <= 0;
    }

    public override void SaveData(TagCompound tag)
    {
        tag.SetWithDefault("leftStack", leftStack, Item.stack);
    }
    public override void LoadData(TagCompound tag)
    {
        tag.GetWithDefault("leftStack", out leftStack, Item.stack);
    }
}
