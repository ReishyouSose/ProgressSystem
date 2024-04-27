using Terraria.GameContent.UI.Chat;

namespace ProgressSystem.Core.Rewards;

public class ItemReward(Item item) : Reward
{
    protected Item _item = item;
    public Item Item
    {
        get => _item;
        set
        {
            _item = value;
            itemTag = ItemTagHandler.GenerateTag(value);
        }
    }
    /// <summary>
    /// 剩余的个数 (有可能一次没领完)
    /// </summary>
    public int leftStack = item.stack;

    protected override object?[] DisplayNameArgs => [itemTag];
    protected string itemTag = ItemTagHandler.GenerateTag(item);

    protected ItemReward() : this(new(0, 1)) { }
    public ItemReward(int itemType, int stack = 1) : this(new(itemType, stack)) { }
    
    protected override bool AutoAssignReceived => false;
    protected override void Receive()
    {
        if (leftStack <= 0)
        {
            State = StateEnum.Received;
            return;
        }
        Item item = Item.Clone();
        item.stack = leftStack;
        // TODO: Entity Source
        // TODO: 直接给玩家背包塞东西
        // Main.LocalPlayer.TryStackToInventory(item, null, false);
        // leftStack = item.stack;
        Main.LocalPlayer.QuickSpawnItem(null, item, item.stack);
        leftStack = 0;
        State = StateEnum.Received;
    }

    public override void SaveDataInPlayer(TagCompound tag)
    {
        base.SaveDataInPlayer(tag);
        tag.SetWithDefault("LeftStack", leftStack, Item.stack);
    }
    public override void LoadDataInPlayer(TagCompound tag)
    {
        base.LoadDataInPlayer(tag);
        tag.GetWithDefault("LeftStack", out leftStack, Item.stack);
    }
}
