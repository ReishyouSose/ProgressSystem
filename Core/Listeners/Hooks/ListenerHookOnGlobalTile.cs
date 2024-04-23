using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria.GameContent.Achievements;

namespace ProgressSystem.Core.Listeners.Hooks;

internal class ListenerHookOnGlobalTile : GlobalTile
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
                i => i.MatchCall(typeof(AchievementsHelper).GetMethod("NotifyTileDestroyed")!)))
        {
            return;
        }
        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldarg_1);
        c.Emit(OpCodes.Ldloc, 0);
        c.EmitDelegate((int x, int y, Tile tile) =>
        {
            PlayerListener.ListenBreakTile(Main.LocalPlayer, x, y, tile);
        });
    }
}
