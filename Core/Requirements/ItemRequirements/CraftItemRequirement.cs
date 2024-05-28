using ProgressSystem.Core.Listeners;
using Terraria.Localization;

namespace ProgressSystem.Core.Requirements.ItemRequirements;

/// <summary>
/// 需要玩家制作某个物品
/// </summary>
public class CraftItemRequirement : ItemRequirement
{
    public CraftItemRequirement(int itemType, int count = 1) : base(itemType, count) { }
    [SpecializeAutoConstruct(Disabled = true)]
    public CraftItemRequirement(Func<Item, bool> condition, LocalizedText conditionDescription, int count = 1) : base(condition, conditionDescription, count) { }
    protected CraftItemRequirement() : base() { }
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
    private void ListenCraftItem(Item item, RecipeItemCreationContext context)
    {
        if (Condition?.Invoke(item) == false)
        {
            return;
        }
        CountNow += item.stack;
    }
}
