using Terraria.Localization;

namespace ProgressSystem.Core.Requirements.TileRequirements;

public class InteractWithChestRequirement : Requirement
{
    #region 钩子
    static InteractWithChestRequirement()
    {
        MonoModHooks.Add(typeof(Player).GetMethod(nameof(Player.OpenChest)), Hook);
    }
    delegate void OpenChestDelegate(Player self, int x, int y, int newChest);
    static void Hook(OpenChestDelegate orig, Player self, int x, int y, int newChest)
    {
        orig(self, x, y, newChest);
        if (!Main.chest.IndexInRange(newChest))
        {
            return;
        }
        OnOpenChest?.Invoke(Main.chest[newChest]);
    }
    public static Action<Chest>? OnOpenChest;
    #endregion

    public Func<Chest, bool>? Condition;
    public LocalizedText? ConditionDescription;
    public InteractWithChestRequirement(Func<Chest, bool> condition, LocalizedText conditionDescription) : this()
    {
        Condition = condition;
        ConditionDescription = conditionDescription;
    }
    protected InteractWithChestRequirement() : base() { }
    protected override object?[] DisplayNameArgs => [ConditionDescription?.Value ?? "?"];
    protected override void BeginListen()
    {
        OnOpenChest += ListenOpenChest;
    }
    protected override void EndListen()
    {
        OnOpenChest -= ListenOpenChest;
    }
    void ListenOpenChest(Chest chest)
    {
        if (Condition?.Invoke(chest) != false)
        {
            CompleteSafe();
        }
    }
}

public class InteractWithAnyChestRequirement : InteractWithChestRequirement
{
    public InteractWithAnyChestRequirement(LocalizedText? conditionDescription = null) :
        base(chest => true, conditionDescription ?? ModInstance.GetLocalization("Requirements.InteractWithAnyChestRequirement.ConditionDescription"))
    { }
    protected InteractWithAnyChestRequirement() : base() { }
}
