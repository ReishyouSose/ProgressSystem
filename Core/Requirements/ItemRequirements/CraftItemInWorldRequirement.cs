using ProgressSystem.Core.Listeners;
using Terraria.Localization;

namespace ProgressSystem.Core.Requirements.ItemRequirements;

public class CraftItemInWorldRequirement : ItemWorldRequirement
{
    public CraftItemInWorldRequirement(int itemType, int count = 1) : base(itemType, count) { }
    public CraftItemInWorldRequirement(Func<Item, bool> condition, LocalizedText conditionDescription, int count = 1) : base(condition, conditionDescription, count) { }
    protected CraftItemInWorldRequirement() : base() { }
    public override void TryBeginListen()
    {
        // 不在服务端监听
        if (Main.netMode == NetmodeID.Server)
        {
            return;
        }
        base.TryBeginListen();
    }
    protected override void BeginListen()
    {
        base.BeginListen();
        if (ItemType > 0)
        {
            PlayerListener.OnLocalPlayerCraftItem.Add(ItemType, ListenCraftItem);
        }
        else
        {
            PlayerListener.OnLocalPlayerCraftItem.Any += ListenCraftItem;
        }
    }
    protected override void EndListen()
    {
        base.EndListen();
        if (ItemType > 0)
        {
            PlayerListener.OnLocalPlayerCraftItem.Remove(ItemType, ListenCraftItem);
        }
        else
        {
            PlayerListener.OnLocalPlayerCraftItem.Any -= ListenCraftItem;
        }
    }
    /// <summary>
    /// 不在服务端监听
    /// </summary>
    private void ListenCraftItem(Item item, RecipeItemCreationContext context)
    {
        if (Condition?.Invoke(item) == false)
        {
            return;
        }
        // 单人模式下直接对世界的 CountNow 操作
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            CountNow += item.stack;
            return;
        }
        // 多人模式下先暂时记录到 countToAdd, 同步时世界的 CountNow 再加上
        countToAdd += item.stack;
        NetUpdate = true;
    }
}
