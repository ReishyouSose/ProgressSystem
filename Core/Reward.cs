namespace ProgressSystem.Core;

public abstract class Reward
{
    /// <summary>
    /// 获取奖励
    /// </summary>
    /// <returns>是否全部获取</returns>
    public abstract bool Receive();
    public virtual void SaveData(TagCompound tag) { }
    public virtual void LoadData(TagCompound tag) { }
}

public class ItemReward(Item item) : Reward
{
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
        var item = Item.Clone();
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