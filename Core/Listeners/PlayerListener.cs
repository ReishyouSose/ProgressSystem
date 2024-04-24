using ProgressSystem.Core.Listeners.Hooks;
using Terraria.ObjectData;

namespace ProgressSystem.Core.Listeners;

public static class PlayerListener
{
    #region KillNPC
    public delegate void OnKillNPCDelegate(Player? player, NPC npc);
    public static event OnKillNPCDelegate? OnKillNPC;
    public delegate void OnLocalPlayerKillNPCDelegate(NPC npc);
    public static event OnLocalPlayerKillNPCDelegate? OnLocalPlayerKillNPC;
    /// <summary>
    /// Set this hook in <see cref="GlobalNPC.HitEffect(NPC, NPC.HitInfo)"/> when <see cref="NPC.life"/> less than 1 if in server 
    /// <br>or <see cref="GlobalNPC.OnKill(NPC)"/> if in single.</br>
    /// </summary>
    internal static void ListenKillNPC(NPC npc)
    {
        Player? player = Main.player.IndexInRange(npc.lastInteraction) ? Main.player[npc.lastInteraction] : null;
        OnKillNPC?.Invoke(player, npc);
        if (npc.lastInteraction == Main.myPlayer)
        {
            OnLocalPlayerKillNPC?.Invoke(npc);
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
    public static event OnBuyItemDelegate? OnBuyItem;
    public static event OnLocalPlayerBuyItemDelegate? OnLocalPlayerBuyItem;
    
    private static readonly Dictionary<int, OnBuyItemDelegate> OnBuyItemOfTypes = [];
    public static void OnBuyItemOfTypeAdd(int type, OnBuyItemDelegate onBuyItemOfType)
    {
        AddDelegateToDict(OnBuyItemOfTypes, type, onBuyItemOfType);
    }
    public static void OnBuyItemOfTypeRemove(int type, OnBuyItemDelegate onBuyItemOfType)
    {
        RemoveDelegateFromDict(OnBuyItemOfTypes, type, onBuyItemOfType);
    }
    private static readonly Dictionary<int, OnLocalPlayerBuyItemDelegate> OnLocalPlayerBuyItemOfTypes = [];
    public static void OnLocalPlayerBuyItemOfTypeAdd(int type, OnLocalPlayerBuyItemDelegate onLocalPlayerBuyItemOfType)
    {
        AddDelegateToDict(OnLocalPlayerBuyItemOfTypes, type, onLocalPlayerBuyItemOfType);
    }
    public static void OnLocalPlayerBuyItemOfTypeRemove(int type, OnLocalPlayerBuyItemDelegate onLocalPlayerBuyItemOfType)
    {
        RemoveDelegateFromDict(OnLocalPlayerBuyItemOfTypes, type, onLocalPlayerBuyItemOfType);
    }
    
    internal static void ListenBuyItem(Player player, NPC vendor, Item[] shopInventory, Item item)
    {
        OnBuyItem?.Invoke(player, vendor, shopInventory, item);
        if (OnBuyItemOfTypes.TryGetValue(item.type, out var onBuyItemOfType))
        {
            onBuyItemOfType(player, vendor, shopInventory, item);
        }
        if (player.whoAmI == Main.myPlayer)
        {
            OnLocalPlayerBuyItem?.Invoke(vendor, shopInventory, item);
            if (OnLocalPlayerBuyItemOfTypes.TryGetValue(item.type, out var onLocalPlayerBuyItemOfType))
            {
                onLocalPlayerBuyItemOfType(vendor, shopInventory, item);
            }
        }
    }
    #endregion
    #region ConsumeItem
    public delegate void OnConsumeItemDelegate(Player player, Item item);
    public static event OnConsumeItemDelegate? OnConsumeItem;
    public delegate void OnLocalPlayerConsumeItemDelegate(Item item);
    public static event OnLocalPlayerConsumeItemDelegate? OnLocalPlayerConsumeItem;
    
    private static readonly Dictionary<int, OnConsumeItemDelegate> OnConsumeItemOfTypes = [];
    public static void OnConsumeItemOfTypeAdd(int type, OnConsumeItemDelegate onConsumeItemOfType)
    {
        AddDelegateToDict(OnConsumeItemOfTypes, type, onConsumeItemOfType);
    }
    public static void OnConsumeItemOfTypeRemove(int type, OnConsumeItemDelegate onConsumeItemOfType)
    {
        RemoveDelegateFromDict(OnConsumeItemOfTypes, type, onConsumeItemOfType);
    }
    private static readonly Dictionary<int, OnLocalPlayerConsumeItemDelegate> OnLocalPlayerConsumeItemOfTypes = [];
    public static void OnLocalPlayerConsumeItemOfTypeAdd(int type, OnLocalPlayerConsumeItemDelegate onLocalPlayerConsumeItemOfType)
    {
        AddDelegateToDict(OnLocalPlayerConsumeItemOfTypes, type, onLocalPlayerConsumeItemOfType);
    }
    public static void OnLocalPlayerConsumeItemOfTypeRemove(int type, OnLocalPlayerConsumeItemDelegate onLocalPlayerConsumeItemOfType)
    {
        RemoveDelegateFromDict(OnLocalPlayerConsumeItemOfTypes, type, onLocalPlayerConsumeItemOfType);
    }
    
    // TODO: 消耗 1 个? 是否消耗?
    internal static void ListenConsumeItem(Player player, Item item)
    {
        OnConsumeItem?.Invoke(player, item);
        if (OnConsumeItemOfTypes.TryGetValue(item.type, out var onConsumeItemOfType))
        {
            onConsumeItemOfType(player, item);
        }
        if (player.whoAmI == Main.myPlayer)
        {
            OnLocalPlayerConsumeItem?.Invoke(item);
            if (OnLocalPlayerConsumeItemOfTypes.TryGetValue(item.type, out var onLocalPlayerConsumeItemOfType))
            {
                onLocalPlayerConsumeItemOfType(item);
            }
        }
    }
    #endregion
    #region PickItem
    public delegate void OnPickItemDelegate(Player player, Item item);
    public static event OnPickItemDelegate? OnPickItem;
    public delegate void OnLocalPlayerPickItemDelegate(Item item);
    public static event OnLocalPlayerPickItemDelegate? OnLocalPlayerPickItem;
    
    private static readonly Dictionary<int, OnPickItemDelegate> OnPickItemOfTypes = [];
    public static void OnPickItemOfTypeAdd(int type, OnPickItemDelegate onPickItemOfType)
    {
        AddDelegateToDict(OnPickItemOfTypes, type, onPickItemOfType);
    }
    public static void OnPickItemOfTypeRemove(int type, OnPickItemDelegate onPickItemOfType)
    {
        RemoveDelegateFromDict(OnPickItemOfTypes, type, onPickItemOfType);
    }
    private static readonly Dictionary<int, OnLocalPlayerPickItemDelegate> OnLocalPlayerPickItemOfTypes = [];
    public static void OnLocalPlayerPickItemOfTypeAdd(int type, OnLocalPlayerPickItemDelegate onLocalPlayerPickItemOfType)
    {
        AddDelegateToDict(OnLocalPlayerPickItemOfTypes, type, onLocalPlayerPickItemOfType);
    }
    public static void OnLocalPlayerPickItemOfTypeRemove(int type, OnLocalPlayerPickItemDelegate onLocalPlayerPickItemOfType)
    {
        RemoveDelegateFromDict(OnLocalPlayerPickItemOfTypes, type, onLocalPlayerPickItemOfType);
    }
    
    internal static void ListenPickItem(Player player, Item item)
    {
        OnPickItem?.Invoke(player, item);
        if (OnPickItemOfTypes.TryGetValue(item.type, out var onPickItemOfType))
        {
            onPickItemOfType(player, item);
        }
        if (player.whoAmI == Main.myPlayer)
        {
            OnLocalPlayerPickItem?.Invoke(item);
            if (OnLocalPlayerPickItemOfTypes.TryGetValue(item.type, out var onLocalPlayerPickItemOfType))
            {
                onLocalPlayerPickItemOfType(item);
            }
        }
    }
    #endregion
    #region Create / Craft Item
    public delegate void OnLocalPlayerCreateItemDelegate(Item item, ItemCreationContext context);
    public delegate void OnLocalPlayerCraftItemDelegate(Item item, RecipeItemCreationContext context);

    public static event OnLocalPlayerCreateItemDelegate? OnLocalPlayerCreateItem;
    public static event OnLocalPlayerCraftItemDelegate? OnLocalPlayerCraftItem;
    
    private static readonly Dictionary<int, OnLocalPlayerCreateItemDelegate> OnLocalPlayerCreateItemOfTypes = [];
    public static void OnLocalPlayerCreateItemOfTypeAdd(int type, OnLocalPlayerCreateItemDelegate onLocalPlayerCreateItemOfType)
    {
        AddDelegateToDict(OnLocalPlayerCreateItemOfTypes, type, onLocalPlayerCreateItemOfType);
    }
    public static void OnLocalPlayerCreateItemOfTypeRemove(int type, OnLocalPlayerCreateItemDelegate onLocalPlayerCreateItemOfType)
    {
        RemoveDelegateFromDict(OnLocalPlayerCreateItemOfTypes, type, onLocalPlayerCreateItemOfType);
    }
    private static readonly Dictionary<int, OnLocalPlayerCraftItemDelegate> OnLocalPlayerCraftItemOfTypes = [];
    public static void OnLocalPlayerCraftItemOfTypeAdd(int type, OnLocalPlayerCraftItemDelegate onLocalPlayerCraftItemOfType)
    {
        AddDelegateToDict(OnLocalPlayerCraftItemOfTypes, type, onLocalPlayerCraftItemOfType);
    }
    public static void OnLocalPlayerCraftItemOfTypeRemove(int type, OnLocalPlayerCraftItemDelegate onLocalPlayerCraftItemOfType)
    {
        RemoveDelegateFromDict(OnLocalPlayerCraftItemOfTypes, type, onLocalPlayerCraftItemOfType);
    }
    
    /// <summary>
    /// See <see cref="GlobalItem.OnCreated(Item, ItemCreationContext)"/>
    /// </summary>
    internal static void ListenCreateItem(Item item, ItemCreationContext context)
    {
        OnLocalPlayerCreateItem?.Invoke(item, context);
        if (OnLocalPlayerCreateItemOfTypes.TryGetValue(item.type, out var onLocalPlayerCreateItemOfType))
        {
            onLocalPlayerCreateItemOfType(item, context);
        }
        if (context is RecipeItemCreationContext craftContext)
        {
            OnLocalPlayerCraftItem?.Invoke(item, craftContext);
            if (OnLocalPlayerCraftItemOfTypes.TryGetValue(item.type, out var onLocalPlayerCraftItemOfType))
            {
                onLocalPlayerCraftItemOfType(item, craftContext);
            }
        }

    }
    #endregion
    #region HitNPC / DealDamage
    public delegate void OnHitNPCDelegate(Player player, NPC target, NPC.HitInfo hit, int damage);
    public static event OnHitNPCDelegate? OnHitNPC;
    public delegate void OnLocalPlayerHitNPCDelegate(NPC target, NPC.HitInfo hit, int damage);
    public static event OnLocalPlayerHitNPCDelegate? OnLocalPlayerHitNPC;
    public delegate void OnDealDamageDelegate(Player player, int damage);
    public static event OnDealDamageDelegate? OnDealDamage;
    public delegate void OnLocalPlayerDealDamageDelegate(int damage);
    public static event OnLocalPlayerDealDamageDelegate? OnLocalPlayerDealDamage;

    internal static void ListenDamage(Player player, NPC target, NPC.HitInfo hit, int damage)
    {
        OnHitNPC?.Invoke(player, target, hit, damage);
        OnDealDamage?.Invoke(player, damage);
        if (player.whoAmI == Main.myPlayer)
        {
            OnLocalPlayerHitNPC?.Invoke(target, hit, damage);
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

    #region Helper
    static void AddDelegateToDict<TKey, TValue>(Dictionary<TKey, TValue>  delegateDict, TKey key, TValue value) where TKey : notnull where TValue : Delegate
    {
        if (delegateDict.TryGetValue(key, out var dictValue))
        {
            delegateDict[key] = (TValue)Delegate.Combine(dictValue, value);
        }
        else
        {
            delegateDict[key] = value;
        }
    }
    static void RemoveDelegateFromDict<TKey, TValue>(Dictionary<TKey, TValue> delegateDict, TKey key, TValue value) where TKey : notnull where TValue : Delegate
    {
        if (!delegateDict.TryGetValue(key, out var dictValue))
        {
            return;
        }
        var removed = (TValue?)Delegate.Remove(dictValue, value);
        if (removed == null)
        {
            delegateDict.Remove(key);
        }
        else
        {
            delegateDict[key] = removed;
        }
    }
    #endregion
}
