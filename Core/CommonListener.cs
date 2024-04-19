using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Collections.ObjectModel;
using Terraria.GameContent.Achievements;
using Terraria.ObjectData;

namespace ProgressSystem;

public static class CommonListener
{
    public delegate void OnNPCKilledDelegate(Player? player, NPC npc);
    public delegate void OnCreateItemDelegate(Player player, Item item, RecipeItemCreationContext context);
    public delegate void OnTileBreakDelegate(Player player, int x, int y, Tile tile);
    public delegate void OnBuyItemDelegate(Player player, NPC vendor, Item[] shopInventory, Item item);
    public delegate void OnConsumeItemDelegate(Player player, Item item);
    public delegate void OnPickItemDelegate(Player player, Item item);
    public delegate void OnLocalPlayerKillNPCDelegate(NPC npc);
    public delegate void OnDamageStatisticsDelegate(Player player, int damage);
    public delegate void OnDistanceStatisticsDelegate(Player player, float distance);
    public delegate void OnPlayerResetDelegate(Player player);
    public delegate void OnStatisticInventoryDelegate(Player player, ReadOnlyDictionary<int, int> invItems);
    public delegate void OnPlayerHurtDelegate(Player player, Player.HurtInfo hurtInfo);
    public delegate void OnManaCostStatisticsDelegate(Player player, int cost);

    public static event OnNPCKilledDelegate? OnNPCKilled;
    public static event OnCreateItemDelegate? OnCreateItem;
    public static event OnTileBreakDelegate? OnTileBreak;
    public static event OnBuyItemDelegate? OnBuyItem;
    public static event OnConsumeItemDelegate? OnConsumeItem;
    public static event OnPickItemDelegate? OnPickItem;
    public static event OnDamageStatisticsDelegate? OnDamageStatistics;
    public static event OnDistanceStatisticsDelegate? OnDistanceStatistics;
    public static event OnPlayerResetDelegate? OnPlayerReset;
    public static event OnStatisticInventoryDelegate? OnStatisticInventory;
    public static event OnPlayerHurtDelegate? OnPlayerHurt;
    public static event OnManaCostStatisticsDelegate? OnManaCostStatistics;

    public delegate void OnLocalPlayerCreateItemDelegate(Item item, ItemCreationContext context);
    public delegate void OnLocalPlayerCraftItemDelegate(Item item, RecipeItemCreationContext context);
    public delegate void OnLocalPlayerBreakTileDelegate(int x, int y, Tile tile);
    public delegate void OnLocalPlayerBuyItemDelegate(NPC vendor, Item[] shopInventory, Item item);
    public delegate void OnLocalPlayerConsumeItemDelegate(Item item);
    public delegate void OnLocalPlayerPickItemDelegate(Item item);

    public static event OnLocalPlayerKillNPCDelegate? OnLocalPlayerKillNPC;
    public static event OnLocalPlayerCreateItemDelegate? OnLocalPlayerCreateItem;
    public static event OnLocalPlayerCraftItemDelegate? OnLocalPlayerCraftItem;
    public static event OnLocalPlayerBreakTileDelegate? OnLocalPlayerBreakTile;
    public static event OnLocalPlayerBuyItemDelegate? OnLocalPlayerBuyItem;
    public static event OnLocalPlayerConsumeItemDelegate? OnLocalPlayerConsumeItem;
    public static event OnLocalPlayerPickItemDelegate? OnLocalPlayerPickItem;

    static CommonListener()
    {
        OnPlayerReset += ListenStatisticInventory;
    }

    /// <summary>
    /// Set this hook in <see cref="GlobalNPC.HitEffect(NPC, NPC.HitInfo)"/> when <see cref="NPC.life"/> less than 1 if in server 
    /// <br>or <see cref="GlobalNPC.OnKill(NPC)"/> if in single.</br>
    /// </summary>
    internal static void ListenNPCKilled(NPC npc)
    {
        Player? player = Main.player.IndexInRange(npc.lastInteraction) ? Main.player[npc.lastInteraction] : null;
        OnNPCKilled?.Invoke(player, npc);
        if (npc.lastInteraction == Main.myPlayer)
        {
            OnLocalPlayerKillNPC?.Invoke(npc);
        }
    }
    /// <summary>
    /// See <see cref="GlobalItem.OnCreated(Item, ItemCreationContext)"/>
    /// </summary>
    internal static void ListenCreateItem(Item item, ItemCreationContext context)
    {
        OnLocalPlayerCreateItem?.Invoke(item, context);
        if (context is RecipeItemCreationContext craftContext)
        {
            OnCreateItem?.Invoke(Main.LocalPlayer, item, craftContext);
            OnLocalPlayerCraftItem?.Invoke(item, craftContext);
        }

    }
    internal static void ListenTileBreak(Player player, int x, int y, Tile tile)
    {
        TileObjectData? data = TileObjectData.GetTileData(tile);
        if (data is null)
        {
            OnTileBreak?.Invoke(player, x, y, tile);
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
            OnTileBreak?.Invoke(player, x, y, tile);
            if (player.whoAmI == Main.myPlayer)
            {
                OnLocalPlayerBreakTile?.Invoke(x, y, tile);
            }
        }
    }
    internal static void ListenBuyItem(Player player, NPC vendor, Item[] shopInventory, Item item)
    {
        OnBuyItem?.Invoke(player, vendor, shopInventory, item);
        if (player.whoAmI == Main.myPlayer)
        {
            OnLocalPlayerBuyItem?.Invoke(vendor, shopInventory, item);
        }
    }
    internal static void ListenConsumeItem(Player player, Item item)
    {
        OnConsumeItem?.Invoke(player, item);
        if (player.whoAmI == Main.myPlayer)
        {
            OnLocalPlayerConsumeItem?.Invoke(item);
        }
    }

    internal static void ListenPickItem(Player player, Item item)
    {
        OnPickItem?.Invoke(player, item);
        if (player.whoAmI == Main.myPlayer)
        {
            OnLocalPlayerPickItem?.Invoke(item);
        }
    }
    internal static void ListenDamage(Player player, int damage)
    {
        OnDamageStatistics?.Invoke(player, damage);
    }
    internal static void ListenPlayerMove(Player player, float moveDistance)
    {
        OnDistanceStatistics?.Invoke(player, moveDistance);
    }
    internal static void ListenStatisticInventory(Player player)
    {
        Dictionary<int, int> invItems = [];
        foreach (Item? item in player.inventory)
        {
            if (invItems.ContainsKey(item.type))
            {
                invItems[item.type] += item.stack;
            }
            else
            {
                invItems[item.type] = item.stack;
            }
        }
        ReadOnlyDictionary<int, int> dic = new(invItems);
        OnStatisticInventory?.Invoke(player, dic);
    }
    internal static void ListenManaCost(Player player, int manaConsumed)
    {
        OnManaCostStatistics?.Invoke(player, manaConsumed);
    }
    internal static void ListenPlayerHurt(Player player, Player.HurtInfo hurtInfo)
    {
        OnPlayerHurt?.Invoke(player, hurtInfo);
    }
}

internal class GloablPlayerHook : ModPlayer
{
    protected override bool CloneNewInstances => true;
    public override void Load()
    {
        IL_Player.GrabItems += IL_Player_GrabItems;
        IL_Player.Update += IL_Player_Update;
    }
    private void IL_Player_Update(ILContext il)
    {
        ILCursor c = new(il);
        if (!c.TryGotoNext(i => i.MatchLdloc(16)))
        {
            return;
        }
        c.Emit(OpCodes.Ldloc, 17);
        c.Emit(OpCodes.Ldloc, 18);
        c.EmitDelegate(ListenForPlayerMove);
    }
    private void IL_Player_GrabItems(ILContext il)
    {
        ILCursor c = new(il);
        if (!c.TryGotoNext(MoveType.AfterLabel,
            i => i.MatchLdarg0(),
            i => i.MatchLdarg1(),
            i => i.MatchLdloc0(),
            i => i.MatchLdloc1(),
            i => i.MatchCall(typeof(Player), "PickupItem"),
            i => i.MatchStloc1()))
        {
            return;
        }
        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldloc_1);
        c.EmitDelegate(CommonListener.ListenPickItem);
    }
    private static void ListenForPlayerMove(bool localPlayerNotOnMount, Vector2 movement)
    {
        if (localPlayerNotOnMount)
        {
            CommonListener.ListenPlayerMove(Main.LocalPlayer, movement.Length());
        }
    }
    public override void PostBuyItem(NPC vendor, Item[] shopInventory, Item item)
    {
        CommonListener.ListenBuyItem(Player, vendor, shopInventory, item);
    }
    public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
    {
        CommonListener.ListenDamage(Player, damageDone);
    }
    public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
    {
        CommonListener.ListenDamage(Player, damageDone);
    }
    public override void OnHurt(Player.HurtInfo info)
    {
        CommonListener.ListenPlayerHurt(Player, info);
    }
    public override void OnConsumeMana(Item item, int manaConsumed)
    {
        CommonListener.ListenManaCost(Player, manaConsumed);
    }
}

internal class GlobalItemHook : GlobalItem
{
    public override void OnCreated(Item item, ItemCreationContext context)
    {
        CommonListener.ListenCreateItem(item, context);
    }
    public override void OnConsumeItem(Item item, Player player)
    {
        CommonListener.ListenConsumeItem(player, item);
    }
}

internal class GlobalNPCHook : GlobalNPC
{
    public override void HitEffect(NPC npc, NPC.HitInfo hit)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient && npc.life <= 0)
        {
            Console.WriteLine($"HitEffect NPC Life: {npc.life}");

            CommonListener.ListenNPCKilled(npc);
        }
    }

    public override void OnKill(NPC npc)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            Console.WriteLine($"OnKill NPC Life: {npc.life}");

            CommonListener.ListenNPCKilled(npc);
        }
    }
}

internal class GlobalTileHook : GlobalTile
{
    public override void Load()
    {
        // 原版成就物块处理后面插入
        IL_WorldGen.KillTile += IL_WorldGen_KillTile;
    }
    private void IL_WorldGen_KillTile(ILContext il)
    {
        ILCursor c = new(il);
        if (!c.TryGotoNext(
                MoveType.Before,
                i => i.MatchCall(typeof(AchievementsHelper).GetMethod("NotifyTileDestroyed"))))
        {
            return;
        }
        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldarg_1);
        c.Emit(OpCodes.Ldloc, 0);
        c.EmitDelegate((int x, int y, Tile tile) =>
        {
            CommonListener.ListenTileBreak(Main.LocalPlayer, x, y, tile);
        });
    }
}
