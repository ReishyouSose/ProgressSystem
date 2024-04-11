namespace ProgressSystem.GameEvents.Hooks;

internal class GlobalItemHook : GlobalItem
{
    public override void OnCreated(Item item, ItemCreationContext context)
    {
        GEListener.ListenCreateItem(item, context);
    }
    public override void OnConsumeItem(Item item, Player player)
    {
        GEListener.ListenConsumeItem(player, item);
    }
}