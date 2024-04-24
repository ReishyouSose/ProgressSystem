namespace ProgressSystem.Core.Listeners.Hooks;

internal class ListenerHookOnGlobalNPC : GlobalNPC
{
    public override void HitEffect(NPC npc, NPC.HitInfo hit)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient && npc.life <= 0)
        {
            PlayerListener.ListenKillNPC(npc);
        }
    }

    public override void OnKill(NPC npc)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            PlayerListener.ListenKillNPC(npc);
        }
    }
}
