using Terraria.Localization;

namespace ProgressSystem.Core.Requirements.ItemRequirements;

/// <summary>
/// 需要玩家制作某个物品
/// </summary>
public class CraftItemRequirement : ItemRequirement
{
    public CraftItemRequirement(int itemType, int count = 1) : base(itemType, count) { }
    public CraftItemRequirement(Func<Item, bool> condition, LocalizedText conditionDescription, int count = 1) : base(condition, conditionDescription, count) { }
    protected CraftItemRequirement() : base() { }
    protected override void BeginListen()
    {
        base.BeginListen();
        CommonListener.OnLocalPlayerCraftItem += ListenCraftItem;
    }
    protected override void EndListen()
    {
        base.EndListen();
        CommonListener.OnLocalPlayerCraftItem -= ListenCraftItem;
    }
    private void ListenCraftItem(Item item, RecipeItemCreationContext context)
    {
        if (ItemType > 0 && item.type != ItemType || Condition?.Invoke(item) == false)
        {
            return;
        }
        CountNow += item.stack;
    }
}
