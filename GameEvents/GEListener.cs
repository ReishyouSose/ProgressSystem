using Terraria.ObjectData;

namespace ProgressSystem.GameEvents;

/// <summary>
/// GameEvent 监听器
/// </summary>
public static class GEListener
{
    public delegate void OnNPCKilledDelegate  (Player? player, NPC npc);
    public delegate void OnCreateItemDelegate (Player player, Item item, RecipeItemCreationContext context);
    public delegate void OnTileBreakDelegate  (Player player, int x, int y, Tile tile);
    public delegate void OnBuyItemDelegate    (Player player, NPC vendor, Item[] shopInventory, Item item);
    public delegate void OnConsumeItemDelegate(Player player, Item item);
    public delegate void OnPickItemDelegate   (Player player, Item item);
    public static event OnNPCKilledDelegate  ? OnNPCKilled;
    public static event OnCreateItemDelegate ? OnCreateItem;
    public static event OnTileBreakDelegate  ? OnTileBreak;
    public static event OnBuyItemDelegate    ? OnBuyItem;
    public static event OnConsumeItemDelegate? OnConsumeItem;
    public static event OnPickItemDelegate   ? OnPickItem;
    
    public delegate void OnLocalPlayerKillNPCDelegate    (NPC npc);
    public delegate void OnLocalPlayerCreateItemDelegate (Item item, ItemCreationContext context);
    public delegate void OnLocalPlayerCraftItemDelegate  (Item item, RecipeItemCreationContext context);
    public delegate void OnLocalPlayerBreakTileDelegate  (int x, int y, Tile tile);
    public delegate void OnLocalPlayerBuyItemDelegate    (NPC vendor, Item[] shopInventory, Item item);
    public delegate void OnLocalPlayerConsumeItemDelegate(Item item);
    public delegate void OnLocalPlayerPickItemDelegate   (Item item);
    public static event OnLocalPlayerKillNPCDelegate    ? OnLocalPlayerKillNPC;
    public static event OnLocalPlayerCreateItemDelegate ? OnLocalPlayerCreateItem;
    public static event OnLocalPlayerCraftItemDelegate  ? OnLocalPlayerCraftItem;
    public static event OnLocalPlayerBreakTileDelegate  ? OnLocalPlayerBreakTile;
    public static event OnLocalPlayerBuyItemDelegate    ? OnLocalPlayerBuyItem;
    public static event OnLocalPlayerConsumeItemDelegate? OnLocalPlayerConsumeItem;
    public static event OnLocalPlayerPickItemDelegate   ? OnLocalPlayerPickItem;

    /// <summary>
    /// Set this hook in <see cref="GlobalNPC.HitEffect(NPC, NPC.HitInfo)"/> when <see cref="NPC.life"/> less than 1 if in server 
    /// <br>or <see cref="GlobalNPC.OnKill(NPC)"/> if in single.</br>
    /// </summary>
    /// <param name="npc"></param>
    internal static void ListenNPCKilled(NPC npc)
    {
        Player? player = Main.player.IndexInRange(npc.lastInteraction) ? Main.player[npc.lastInteraction] : null;
        OnNPCKilled?.Invoke(player, npc);
        if (npc.lastInteraction == Main.myPlayer) {
            OnLocalPlayerKillNPC?.Invoke(npc);
        }
    }
    /// <summary>
    /// See <see cref="GlobalItem.OnCreated(Item, ItemCreationContext)"/>
    /// </summary>
    /// <param name="item"></param>
    internal static void ListenCreateItem(Item item, ItemCreationContext context)
    {
        OnLocalPlayerCreateItem?.Invoke(item, context);
        if (context is RecipeItemCreationContext craftContext) {
            OnCreateItem?.Invoke(Main.LocalPlayer, item, craftContext);
            OnLocalPlayerCraftItem?.Invoke(item, craftContext);
        }
        
    }
    internal static void ListenTileBreak(Player player, int x, int y, Tile tile)
    {
        var data = TileObjectData.GetTileData(tile);
        if (data is null)
        {
            OnTileBreak?.Invoke(player, x, y, tile);
            if (player.whoAmI == Main.myPlayer) {
                OnLocalPlayerBreakTile?.Invoke(x, y, tile);
            }
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
            if (player.whoAmI == Main.myPlayer) {
                OnLocalPlayerBreakTile?.Invoke(x, y, tile);
            }
        }
    }
    internal static void ListenBuyItem(Player player, NPC vendor, Item[] shopInventory, Item item)
    {
        OnBuyItem?.Invoke(player, vendor, shopInventory, item);
        if (player.whoAmI == Main.myPlayer) {
            OnLocalPlayerBuyItem?.Invoke(vendor, shopInventory, item);
        }
    }
    internal static void ListenConsumeItem(Player player, Item item)
    {
        OnConsumeItem?.Invoke(player, item);
        if (player.whoAmI == Main.myPlayer) {
            OnLocalPlayerConsumeItem?.Invoke(item);
        }
    }
    // !!!!! 当玩家碰到物品但捡不起来时会反复调用
    internal static void ListenPickItem(Player player, Item item)
    {
        OnPickItem?.Invoke(player, item);
        if (player.whoAmI == Main.myPlayer) {
            OnLocalPlayerPickItem?.Invoke(item);
        }
    }
}
