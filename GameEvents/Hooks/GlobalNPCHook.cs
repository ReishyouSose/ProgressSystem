namespace ProgressSystem.GameEvents.Hooks;

internal class GlobalNPCHook : GlobalNPC
{
    public override void HitEffect(NPC npc, NPC.HitInfo hit)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient && npc.life <= 0)
        {
            Console.WriteLine($"HitEffect NPC Life: {npc.life}");

            GEListener.ListenNPCKilled(npc);
        }
    }

    public override void OnKill(NPC npc)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            Console.WriteLine($"OnKill NPC Life: {npc.life}");

            GEListener.ListenNPCKilled(npc);
        }
    }
}
