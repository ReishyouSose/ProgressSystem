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
        item.newAndShiny = true;
        item.Center = Main.LocalPlayer.MountedCenter;
        item = Main.LocalPlayer.GetItem(Main.myPlayer, item, new GetItemSettings(CanGoIntoVoidVault: true));
        leftStack = item.stack;
        if (leftStack <= 0)
        {
            leftStack = 0;
            State = StateEnum.Received;
        }
        else if (leftStack < Item.stack)
        {
            State = StateEnum.Receiving;
        }
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
    public override void SaveStaticData(TagCompound tag)
    {
        base.SaveStaticData(tag);
        if (!ShouldSaveStaticData)
        {
            return;
        }
        tag.SetWithDefault("Item", Item, TigerExtensions.ItemCheckDefault);
    }
    public override void LoadStaticData(TagCompound tag)
    {
        if (!ShouldSaveStaticData)
        {
            return;
        }
        if (tag.TryGet<Item>("Item", out var item))
        {
            Item = item;
        }
    }
}
