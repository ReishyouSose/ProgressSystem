namespace ProgressSystem.Core;

// DOING...

/// <summary>
/// 储存并管理所有的成就
/// </summary>
public class AchievementManager : ModSystem
{
    #region Test
    public override void OnModLoad()
    {
        var page = AchievementPage.Create(ModInstance, "Achievements");
        page.Add(new(ModInstance, page, "First", requirements: [new SubmitRequirement()], rewards: [new ItemReward(ItemID.SilverCoin, 20)]));
        page.Add(new(ModInstance, page, "Workbench", predecessorNames: ["Wood"],
            requirements: [new CraftItemRequirement(ItemID.WorkBench)],
            rewards: [new ItemReward(ItemID.Wood, 100)]));
        page.Add(new(ModInstance, page, "WoodTools", predecessorNames: ["Workbench"],
            requirements: [
                new CraftItemRequirement(ItemID.WoodenSword),
                new CraftItemRequirement(ItemID.WoodHelmet),
                new CraftItemRequirement(ItemID.WoodBreastplate)],
            rewards: [new ItemReward(ItemID.IronskinPotion, 5)]));
        page.Add(new(ModInstance, page, "House", predecessorNames: ["Workbench", "Torch"],
            requirements: [new HouseRequirement()],
            rewards: [new ItemReward(ItemID.Wood, 100)]));
        page.Add(new(ModInstance, page, "Slime",
            requirements: [new KillNPCRequirement(NPCID.GreenSlime)],
            rewards: [new ItemReward(ItemID.Gel, 30)]));
        page.Add(new(ModInstance, page, "Torch", predecessorNames: ["Slime", "Wood"],
            requirements: [new CraftItemRequirement(ItemID.Torch)],
            rewards: [new ItemReward(ItemID.Torch, 100)]));
        page.Add(new(ModInstance, page, "Wood", predecessorNames: ["First"],
            requirements: [new PickItemRequirement(ItemID.Wood)],
            rewards: [new ItemReward(ItemID.Apple)]));
    }
    #endregion

    public static Dictionary<string, AchievementPage> Pages { get; set; } = [];

    public override void PostUpdatePlayers()
    {
        base.PostUpdatePlayers();

    }
    public override void SaveWorldData(TagCompound tag)
    {
        base.SaveWorldData(tag);
    }
    public override void LoadWorldData(TagCompound tag)
    {
        base.LoadWorldData(tag);
    }
}

public class AchievementPlayerManager : ModPlayer
{
    public override void OnEnterWorld()
    {
        base.OnEnterWorld();
    }
    public override void SaveData(TagCompound tag)
    {
        base.SaveData(tag);
    }
    public override void LoadData(TagCompound tag)
    {
        base.LoadData(tag);
    }
}

