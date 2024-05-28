using Terraria.GameContent.UI.States;

namespace ProgressSystem.Core.Requirements.MiscRequirements;

public class OpenBestiaryRequirement : Requirement
{
    #region 钩子
    // 原版打开图鉴的方式应该是在Main.DrawBestiaryIcon中
    static OpenBestiaryRequirement()
    {
        On_UIBestiaryTest.OnOpenPage += Hook;
    }

    private static void Hook(On_UIBestiaryTest.orig_OnOpenPage orig, UIBestiaryTest self)
    {
        orig(self);
        OnOpenBestiary?.Invoke();
    }
    public static Action? OnOpenBestiary;
    #endregion
    public OpenBestiaryRequirement() : base(ListenTypeEnum.OnStart) { }
    protected override void BeginListen()
    {
        OnOpenBestiary += CompleteSafe;
    }
    protected override void EndListen()
    {
        OnOpenBestiary -= CompleteSafe;
    }
}
