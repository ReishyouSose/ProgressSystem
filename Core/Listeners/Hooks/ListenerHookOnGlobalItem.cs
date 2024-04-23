namespace ProgressSystem.Core.Listeners.Hooks;

internal class ListenerHookOnGlobalItem : GlobalItem
{
    public override void OnCreated(Item item, ItemCreationContext context)
    {
        PlayerListener.ListenCreateItem(item, context);
    }
    public override void OnConsumeItem(Item item, Player player)
    {
        PlayerListener.ListenConsumeItem(player, item);
    }
}
