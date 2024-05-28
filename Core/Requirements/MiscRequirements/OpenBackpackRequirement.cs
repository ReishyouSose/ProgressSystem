namespace ProgressSystem.Core.Requirements.MiscRequirements;

public class OpenBackpackRequirement : Requirement
{
    #region 钩子
    static OpenBackpackRequirement()
    {
        MonoModHooks.Add(typeof(Player).GetMethod("OpenInventory", TMLReflection.bfns), Hook);
    }
    static void Hook(Action orig)
    {
        bool old = Main.playerInventory;
        orig();
        if (old || !Main.playerInventory)
        {
            return;
        }
        OnOpenBackpack?.Invoke();
    }
    public static Action? OnOpenBackpack;
    #endregion

    public OpenBackpackRequirement() : base(ListenTypeEnum.OnStart) { }
    protected override void BeginListen()
    {
        OnOpenBackpack += CompleteSafe;
        if (Main.playerInventory)
        {
            CompleteSafe();
        }
    }
    protected override void EndListen()
    {
        OnOpenBackpack -= CompleteSafe;
    }
}
