using ProgressSystem.Core.Requirements;
using ProgressSystem.Core.StaticData;
using System.Reflection;

namespace ProgressSystem.Core;

// DOING...

/// <summary>
/// 储存并管理所有的成就
/// </summary>
public class AchievementManager : ModSystem, IWithStaticData
{
    public static AchievementManager Instance { get; set; } = null!;
    #region Test
    public override void OnModLoad()
    {
        var page = AchievementPage.Create(ModInstance, "Achievements");
        Achievement.Create(page, ModInstance, "First", requirements: [new SubmitRequirement()], rewards: [new ItemReward(ItemID.SilverCoin, 20)]);
        Achievement.Create(page, ModInstance, "Workbench", predecessorNames: ["Wood"],
            requirements: [new CraftItemRequirement(ItemID.WorkBench)],
            rewards: [new ItemReward(ItemID.Wood, 100)]);
        Achievement.Create(page, ModInstance, "WoodTools", predecessorNames: ["Workbench"],
            requirements: [
                new CraftItemRequirement(ItemID.WoodenSword),
                new CraftItemRequirement(ItemID.WoodHelmet),
                new CraftItemRequirement(ItemID.WoodBreastplate)],
            rewards: [new ItemReward(ItemID.IronskinPotion, 5)]);
        Achievement.Create(page, ModInstance, "House", predecessorNames: ["Workbench", "Torch"],
            requirements: [new HouseRequirement()],
            rewards: [new ItemReward(ItemID.Wood, 100)]);
        Achievement.Create(page, ModInstance, "Slime",
            requirements: [new KillNPCRequirement(NPCID.BlueSlime)],
            rewards: [new ItemReward(ItemID.Gel, 30)]);
        Achievement.Create(page, ModInstance, "Torch", predecessorNames: ["Slime", "Wood"],
            requirements: [new CraftItemRequirement(ItemID.Torch)],
            rewards: [new ItemReward(ItemID.Torch, 100)]);
        Achievement.Create(page, ModInstance, "Wood", predecessorNames: ["First"],
            requirements: [new PickItemRequirement(ItemID.Wood)],
            rewards: [new ItemReward(ItemID.Apple)]);
    }
    #endregion

    /// <summary>
    /// 存有所有的成就页, 键为<see cref="AchievementPage.FullName"/>
    /// </summary>
    public static IReadOnlyDictionary<string, AchievementPage> Pages => pages;
    private static readonly Dictionary<string, AchievementPage> pages = [];
    public static IReadOnlyDictionary<Mod, Dictionary<string, AchievementPage>> PagesByMod => pagesByMod;
    private static readonly Dictionary<Mod, Dictionary<string, AchievementPage>> pagesByMod = [];
    internal static void AddPage(AchievementPage page)
    {
        pages.Add(page.FullName, page);
        if (!pagesByMod.ContainsKey(page.Mod))
        {
            pagesByMod.Add(page.Mod, []);
        }
        pagesByMod[page.Mod].Add(page.Name, page);
    }

    public static void PostInitialize()
    {
        Pages.Values.ForeachDo(p => p.Achievements.Values.ForeachDo(a => a.PostInitialize()));
    }
    #region 存取数据
    public override void SaveWorldData(TagCompound tag)
    {
        tag.SaveDictionaryData("Pages", pages, (p, t) => p.SaveDataInWorld(t));
    }
    public override void LoadWorldData(TagCompound tag)
    {
        tag.LoadDictionaryData("Pages", pages, (p, t) => p.LoadDataInWorld(t));
    }
    /// <summary>
    /// 永远是false
    /// </summary>
    public bool ShouldSaveStaticData { get => false; set { } }
    public static void SaveStaticDataStatic(TagCompound tag) => Instance.SaveStaticData(tag);
    public static void LoadStaticDataStatic(TagCompound tag) => Instance.LoadStaticData(tag);
    public void SaveStaticData(TagCompound tag)
    {
        this.SaveStaticDataTemplate(Pages.Values, p => p.FullName, "Pages", tag);
    }
    public void LoadStaticData(TagCompound tag)
    {
        this.LoadStaticDataTemplate(fullName => Pages.TryGetValue(fullName, out var p) ? p : null,
            (p, m, n) => { p.Mod = m; p.Name = n; }, (f, p) => AddPage(p), "Pages", tag);
    }
    #endregion
    public override void OnWorldLoad()
    {
        // 在 LoadWorldData 前执行
    }
    public override void OnWorldUnload()
    {
        // 在 SaveWorldData 后执行
        Reset();
    }
    /// <summary>
    /// 重置所有的成就数据
    /// </summary>
    public static void Reset()
    {
        Pages.Values.ForeachDo(p => p.Reset());
    }
    /// <summary>
    /// 在 <see cref="ModPlayer.OnEnterWorld"/> 中调用
    /// 此时玩家的和世界的数据都已加载完毕
    /// </summary>
    public static void Start()
    {
        Pages.Values.ForeachDo(p => p.Start());
    }
    /// <summary>
    /// 在游戏中调用, 重置所有成就的进度
    /// </summary>
    public static void Restart()
    {
        Reset();
        Start();
    }

    #region 钩子
    public override void Load()
    {
        Instance = this;
        HookInPostInitialize();
    }
    static void HookInPostInitialize()
    {
        MonoModHooks.Add(typeof(ItemLoader).GetMethod("FinishSetup", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static), OnItemLoaderFinishSetup);
    }
    static void OnItemLoaderFinishSetup(Action orig)
    {
        orig();
        PostInitialize();
    }
    #endregion
}

public class AchievementPlayerManager : ModPlayer
{
    public override void OnEnterWorld()
    {
        if (loadedData != null)
        {
            LoadDataOnEnterWorld(loadedData);
        }
        AchievementManager.Start();
    }
    /// <summary>
    /// 同<see cref="AchievementManager.Pages"/>
    /// </summary>
    private static IReadOnlyDictionary<string, AchievementPage> Pages => AchievementManager.Pages;

    public override void SaveData(TagCompound tag)
    {
        tag.SaveReadOnlyDictionaryData("Pages", Pages, (p, t) => p.SaveDataInPlayer(t));
    }
    public static void LoadDataOnEnterWorld(TagCompound tag)
    {
        tag.LoadReadOnlyDictionaryData("Pages", Pages, (p, t) => p.LoadDataInPlayer(t));
    }
    public override void LoadData(TagCompound tag)
    {
        loadedData = tag;
    }
    private TagCompound? loadedData;
}

