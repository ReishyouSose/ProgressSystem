using ProgressSystem.Core.Listeners.Hooks;
using Terraria.ObjectData;

namespace ProgressSystem.Core.Listeners;

public static class PlayerListener
{
    #region KillNPC
    public delegate void OnKillNPCDelegate(Player? player, NPC npc);
    public static ListenerWithType<OnKillNPCDelegate> OnKillNPC = new();
    public delegate void OnLocalPlayerKillNPCDelegate(NPC npc);
    public static ListenerWithType<OnLocalPlayerKillNPCDelegate> OnLocalPlayerKillNPC = new();

    /// <summary>
    /// Set this hook in <see cref="GlobalNPC.HitEffect(NPC, NPC.HitInfo)"/> when <see cref="NPC.life"/> less than 1 if in server 
    /// <br>or <see cref="GlobalNPC.OnKill(NPC)"/> if in single.</br>
    /// </summary>
    internal static void ListenKillNPC(NPC npc)
    {
        Player? player = Main.player.IndexInRange(npc.lastInteraction) ? Main.player[npc.lastInteraction] : null;
        OnKillNPC.Invoke(npc.type, d => d(player, npc));
        if (npc.lastInteraction == Main.myPlayer)
        {
            OnLocalPlayerKillNPC?.Invoke(npc.type, d => d(npc));
        }
    }
    #endregion
    #region BreakTile
    public delegate void OnBreakTileDelegate(Player player, int x, int y, Tile tile);
    public static event OnBreakTileDelegate? OnBreakTile;
    public delegate void OnLocalPlayerBreakTileDelegate(int x, int y, Tile tile);
    public static event OnLocalPlayerBreakTileDelegate? OnLocalPlayerBreakTile;
    internal static void ListenBreakTile(Player player, int x, int y, Tile tile)
    {
        TileObjectData? data = TileObjectData.GetTileData(tile);
        if (data is null)
        {
            OnBreakTile?.Invoke(player, x, y, tile);
            if (player.whoAmI == Main.myPlayer)
            {
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
            OnBreakTile?.Invoke(player, x, y, tile);
            if (player.whoAmI == Main.myPlayer)
            {
                OnLocalPlayerBreakTile?.Invoke(x, y, tile);
            }
        }
    }
    #endregion
    #region BuyItem
    public delegate void OnBuyItemDelegate(Player player, NPC vendor, Item[] shopInventory, Item item);
    public delegate void OnLocalPlayerBuyItemDelegate(NPC vendor, Item[] shopInventory, Item item);
    public static ListenerWithType<OnBuyItemDelegate> OnBuyItem = new();
    public static ListenerWithType<OnLocalPlayerBuyItemDelegate> OnLocalPlayerBuyItem = new();

    internal static void ListenBuyItem(Player player, NPC vendor, Item[] shopInventory, Item item)
    {
        OnBuyItem?.Invoke(item.type, d => d(player, vendor, shopInventory, item));
        if (player.whoAmI == Main.myPlayer)
        {
            OnLocalPlayerBuyItem?.Invoke(item.type, d => d(vendor, shopInventory, item));
        }
    }
    #endregion
    #region ConsumeItem
    public delegate void OnConsumeItemDelegate(Player player, Item item);
    public delegate void OnLocalPlayerConsumeItemDelegate(Item item);
    public static ListenerWithType<OnConsumeItemDelegate> OnConsumeItem = new();
    public static ListenerWithType<OnLocalPlayerConsumeItemDelegate> OnLocalPlayerConsumeItem = new();

    // TODO: 消耗 1 个? 是否消耗?
    internal static void ListenConsumeItem(Player player, Item item)
    {
        OnConsumeItem.Invoke(item.type, d => d(player, item));
        if (player.whoAmI == Main.myPlayer)
        {
            OnLocalPlayerConsumeItem?.Invoke(item.type, d => d(item));
        }
    }
    #endregion
    #region PickItem
    public delegate void OnPickItemDelegate(Player player, Item item);
    public delegate void OnLocalPlayerPickItemDelegate(Item item);
    public static ListenerWithType<OnPickItemDelegate> OnPickItem = new();
    public static ListenerWithType<OnLocalPlayerPickItemDelegate> OnLocalPlayerPickItem = new();

    internal static void ListenPickItem(Player player, Item item)
    {
        OnPickItem.Invoke(item.type, d => d(player, item));
        if (player.whoAmI == Main.myPlayer)
        {
            OnLocalPlayerPickItem.Invoke(item.type, d => d(item));
        }
    }
    #endregion
    #region Create / Craft Item
    public delegate void OnLocalPlayerCreateItemDelegate(Item item, ItemCreationContext context);
    public delegate void OnLocalPlayerCraftItemDelegate(Item item, RecipeItemCreationContext context);
    public static ListenerWithType<OnLocalPlayerCreateItemDelegate> OnLocalPlayerCreateItem = new();
    public static ListenerWithType<OnLocalPlayerCraftItemDelegate> OnLocalPlayerCraftItem = new();

    /// <summary>
    /// See <see cref="GlobalItem.OnCreated(Item, ItemCreationContext)"/>
    /// </summary>
    internal static void ListenCreateItem(Item item, ItemCreationContext context)
    {
        OnLocalPlayerCreateItem?.Invoke(item.type, d => d(item, context));
        if (context is RecipeItemCreationContext craftContext)
        {
            OnLocalPlayerCraftItem?.Invoke(item.type, d => d(item, craftContext));
        }

    }
    #endregion
    #region HitNPC / DealDamage
    public delegate void OnHitNPCDelegate(Player player, NPC target, NPC.HitInfo hit, int damage);
    public delegate void OnLocalPlayerHitNPCDelegate(NPC target, NPC.HitInfo hit, int damage);
    public delegate void OnDealDamageDelegate(Player player, int damage);
    public delegate void OnLocalPlayerDealDamageDelegate(int damage);
    public static ListenerWithType<OnHitNPCDelegate> OnHitNPC = new();
    public static ListenerWithType<OnLocalPlayerHitNPCDelegate> OnLocalPlayerHitNPC = new();
    public static event OnDealDamageDelegate? OnDealDamage;
    public static event OnLocalPlayerDealDamageDelegate? OnLocalPlayerDealDamage;

    internal static void ListenDamage(Player player, NPC target, NPC.HitInfo hit, int damage)
    {
        OnHitNPC?.Invoke(target.type, d => d(player, target, hit, damage));
        OnDealDamage?.Invoke(player, damage);
        if (player.whoAmI == Main.myPlayer)
        {
            OnLocalPlayerHitNPC?.Invoke(target.type, d=>d(target, hit, damage));
            OnLocalPlayerDealDamage?.Invoke(damage);
        }
    }
    #endregion
    #region Move
    public delegate void OnMoveDelegate(Player player, float distance);
    public static event OnMoveDelegate? OnMove;
    public delegate void OnLocalPlayerMoveDelegate(float distance);
    public static event OnLocalPlayerMoveDelegate? OnLocalPlayerMove;
    internal static void ListenMove(Player player, float moveDistance)
    {
        OnMove?.Invoke(player, moveDistance);
        if (player.whoAmI == Main.myPlayer)
        {
            OnLocalPlayerMove?.Invoke(moveDistance);
        }
    }
    #endregion
    #region Hurt
    public delegate void OnHurtDelegate(Player player, Player.HurtInfo hurtInfo);
    /// <summary>
    /// 在本地, 服务器, 其它客户端都会执行
    /// </summary>
    public static event OnHurtDelegate? OnHurt;
    public delegate void OnLocalPlayerHurtDelegate(Player.HurtInfo hurtInfo);
    public static event OnLocalPlayerHurtDelegate? OnLocalPlayerHurt;
    internal static void ListenHurt(Player player, Player.HurtInfo hurtInfo)
    {
        OnHurt?.Invoke(player, hurtInfo);
        if (player.whoAmI == Main.myPlayer)
        {
            OnLocalPlayerHurt?.Invoke(hurtInfo);
        }
    }
    #endregion
    #region ConsumeMana
    public delegate void OnConsumeManaDelegate(Player player, int cost);
    public static event OnConsumeManaDelegate? OnConsumeMana;
    public delegate void OnLocalPlayerConsumeManaDelegate(int cost);
    public static event OnLocalPlayerConsumeManaDelegate? OnLocalPlayerConsumeMana;
    internal static void ListenCostMana(Player player, int manaConsumed)
    {
        OnConsumeMana?.Invoke(player, manaConsumed);
        if (player.whoAmI == Main.myPlayer)
        {
            OnLocalPlayerConsumeMana?.Invoke(manaConsumed);
        }
    }
    #endregion

    #region Statistics
    public static long LocalPlayerTotalHealthLose => PlayerStatistics.Ins?.TotalHealthLose ?? 0;
    public static long LocalPlayerTotalManaConsumed => PlayerStatistics.Ins?.TotalManaConsumed ?? 0;

    public delegate void OnLocalPlayerTotalHealthLoseChangedDelegate(long totalHealthLose);
    public delegate void OnLocalPlayerTotalManaConsumedChangedDelegate(long totalManaConsumed);

    public static event OnLocalPlayerTotalHealthLoseChangedDelegate? OnLocalPlayerTotalHealthLoseChanged;
    public static event OnLocalPlayerTotalManaConsumedChangedDelegate? OnLocalPlayerTotalManaConsumedChanged;

    internal static void ListenLocalPlayerTotalHealthLoseChanged(long totalHealthLose)
    {
        OnLocalPlayerTotalHealthLoseChanged?.Invoke(totalHealthLose);
    }
    internal static void ListenLocalPlayerTotalManaConsumedChanged(long totalManaConsumed)
    {
        OnLocalPlayerTotalManaConsumedChanged?.Invoke(totalManaConsumed);
    }
    #endregion
}
