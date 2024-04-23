using ProgressSystem.Core.Listeners;
using Terraria.Localization;

namespace ProgressSystem.Core.Requirements.ItemRequirements;

public class ConsumeItemRequirement : ItemRequirement
{
    public ConsumeItemRequirement(int itemType, int count = 1) : base(itemType, count) { }
    [SpecializeAutoConstruct(Disabled = true)]
    public ConsumeItemRequirement(Func<Item, bool> condition, LocalizedText conditionDescription, int count = 1) : base(condition, conditionDescription, count) { }
    protected ConsumeItemRequirement() : base() { }
    protected override void BeginListen()
    {
        base.BeginListen();
        if (ItemType > 0)
        {
            PlayerListener.OnLocalPlayerConsumeItemOfTypeAdd(ItemType, ListenConsumeItem);
        }
        else
        {
            PlayerListener.OnLocalPlayerConsumeItem += ListenConsumeItem;
        }
    }
    protected override void EndListen()
    {
        base.EndListen();
        if (ItemType > 0)
        {
            PlayerListener.OnLocalPlayerConsumeItemOfTypeRemove(ItemType, ListenConsumeItem);
        }
        else
        {
            PlayerListener.OnLocalPlayerConsumeItem -= ListenConsumeItem;
        }
    }
    private void ListenConsumeItem(Item item)
    {
        if (Condition?.Invoke(item) == false)
        {
            return;
        }
        CountNow += 1;
    }
}
