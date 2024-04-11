namespace ProgressSystem.GameEvents.Hooks;

internal class GloablPlayerHook : ModPlayer
{
    public override bool CloneNewInstances => true;

    public override void PostBuyItem(NPC vendor, Item[] shopInventory, Item item)
    {
        GEListener.ListenBuyItem(Player, vendor, shopInventory, item);
    }

    public override bool OnPickup(Item item)
    {
        GEListener.ListenPickItem(Player, item);
        return true;
    }
}
