using ProgressSystem.Core.Listeners;
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
        if (ItemType > 0)
        {
            PlayerListener.OnLocalPlayerPickItemOfTypeAdd(ItemType, ListenPickItem);
        }
        else
        {
            PlayerListener.OnLocalPlayerPickItem += ListenPickItem;
        }
    }
    protected override void EndListen()
    {
        base.EndListen();
        PlayerListener.OnLocalPlayerPickItem -= ListenPickItem;
        if (ItemType > 0)
        {
            PlayerListener.OnLocalPlayerPickItemOfTypeRemove(ItemType, ListenPickItem);
        }
        else
        {
            PlayerListener.OnLocalPlayerPickItem -= ListenPickItem;
        }
    }
    private void ListenPickItem(Item item)
    {
        if (Condition?.Invoke(item) == false)
        {
            return;
        }
        CountNow += item.stack;
    }
}
