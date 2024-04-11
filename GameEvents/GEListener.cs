using Terraria.ObjectData;

namespace ProgressSystem.GameEvents;

/// <summary>
/// GameEvent 监听器
/// </summary>
public static class GEListener
{
    public static event Action<Player, NPC> OnNPCKilled;
    public static event Action<Player, Item, RecipeItemCreationContext> OnCreateItem;
    public static event Action<Player, int, int, Tile> OnTileBreak;
    public static event Action<Player, NPC, Item[], Item> OnBuyItem;
    public static event Action<Player, Item> OnConsumeItem;
    public static event Action<Player, Item> OnPickItem;

    /// <summary>
    /// Set this hook in <see cref="GlobalNPC.HitEffect(NPC, NPC.HitInfo)"/> when <see cref="NPC.life"/> less than 1 if in server 
    /// <br>or <see cref="GlobalNPC.OnKill(NPC)"/> if in single.</br>
    /// </summary>
    /// <param name="npc"></param>
    internal static void ListenNPCKilled(NPC npc)
    {
        Player player = Main.player.IndexInRange(npc.lastInteraction) ? Main.player[npc.lastInteraction] : null;
        OnNPCKilled?.Invoke(player, npc);
    }
    /// <summary>
    /// See <see cref="GlobalItem.OnCreated(Item, ItemCreationContext)"/>
    /// </summary>
    /// <param name="item"></param>
    internal static void ListenCreateItem(Item item, ItemCreationContext context)
    {
        OnCreateItem?.Invoke(Main.LocalPlayer, item, (RecipeItemCreationContext)context);
    }
    internal static void ListenTileBreak(Player player, int x, int y, Tile tile)
    {
        var data = TileObjectData.GetTileData(tile);
        if (data is null)
        {
            OnTileBreak?.Invoke(player, x, y, tile);
        }
        else
        {
            int frameX = tile.TileFrameX;
            int frameY = tile.TileFrameY;
            int partFrameX = frameX % data.CoordinateFullWidth;
            if (partFrameX != 0)
            {
                return;
            }
            int partFrameY = frameY % data.CoordinateFullHeight;
            if (partFrameY != 0)
            {
                return;
            }
            OnTileBreak?.Invoke(player, x, y, tile);
        }
    }
    internal static void ListenBuyItem(Player player, NPC vendor, Item[] shopInventory, Item item)
    {
        OnBuyItem?.Invoke(player, vendor, shopInventory, item);
    }
    internal static void ListenConsumeItem(Player player, Item item)
    {
        OnConsumeItem?.Invoke(player, item);
    }
    internal static void ListenPickItem(Player player, Item item)
    {
        OnPickItem?.Invoke(player, item);
    }
}