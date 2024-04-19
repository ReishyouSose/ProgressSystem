using Terraria.Localization;

namespace ProgressSystem.Core.Requirements.ItemRequirements;

/// <summary>
/// 需要玩家捡到某个物品
/// </summary>
public class PickItemRequirement : ItemRequirement
{
    public PickItemRequirement(int itemType, int count = 1) : base(itemType, count) { }
    public PickItemRequirement(Func<Item, bool> condition, LocalizedText conditionDescription, int count = 1) : base(condition, conditionDescription, count) { }
    protected PickItemRequirement() : base() { }
    protected override void BeginListen()
    {
        base.BeginListen();
        CommonListener.OnLocalPlayerPickItem += ListenPickItem;
        DoIf(CountNow >= Count, CompleteSafe);
    }
    protected override void EndListen()
    {
        base.EndListen();
        CommonListener.OnLocalPlayerPickItem -= ListenPickItem;
    }
    private void ListenPickItem(Item item)
    {
        if (ItemType > 0 && item.type != ItemType || Condition?.Invoke(item) == false)
        {
            return;
        }
        DoIf((CountNow += item.stack) >= Count, CompleteSafe);
    }
}
