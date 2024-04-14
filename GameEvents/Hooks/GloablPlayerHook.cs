using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace ProgressSystem.GameEvents.Hooks;

internal class GloablPlayerHook : ModPlayer
{
    protected override bool CloneNewInstances => true;
    public override void Load()
    {
        IL_Player.GrabItems += IL_Player_GrabItems;
    }
    private void IL_Player_GrabItems(ILContext il)
    {
        ILCursor c = new(il);
        if(c.TryGotoNext(
            i=>i.MatchLdsfld(typeof(Main),nameof(Main.item)),
            i=>i.MatchLdloc0(),
            i=>i.MatchNewobj<Item>(),
            i=>i.MatchStelemRef()))
        {
            return;
        }
        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldloc_1);
        c.EmitDelegate(GEListener.ListenPickItem);
        if(c.TryGotoNext(
            i=>i.MatchLdarg0(),
            i=>i.MatchLdarg1(),
            i=>i.MatchLdloc0(),
            i=>i.MatchLdloc1(),
            i=>i.MatchCall(typeof(Player), "PickupItem"),
            i=>i.MatchStloc1()))
        {
            return;
        }
        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldloc_1);
        c.EmitDelegate(GEListener.ListenPickItem);
    }
    public override void PostBuyItem(NPC vendor, Item[] shopInventory, Item item)
    {
        GEListener.ListenBuyItem(Player, vendor, shopInventory, item);
    }
    public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
    {
        GEListener.ListenDamage(Player, damageDone);
    }
    public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
    {
        GEListener.ListenDamage(Player, damageDone);
    }
}
