using ProgressSystem.Common.Configs;
using ProgressSystem.Core.Interfaces;
using ProgressSystem.Core.NetUpdate;
using ProgressSystem.Core.Requirements;
using ProgressSystem.Core.Requirements.ItemRequirements;
using ProgressSystem.Core.Requirements.MiscRequirements;
using ProgressSystem.Core.Requirements.NPCRequirements;
using ProgressSystem.Core.Rewards;
using ProgressSystem.Core.StaticData;
using System.IO;
using System.Reflection;

namespace ProgressSystem.Core;

/// <summary>
/// 储存并管理所有的成就
/// </summary>
public class AchievementManager : ModSystem, IWithStaticData, INetUpdate, IProgressable, IAchievementNode
{
    public static AchievementManager Instance { get; set; } = null!;
    public static int GeneralTimer { get; private set; }
    #region Test
    public override void OnModLoad()
    {
        var page = AchievementPage.Create(ModInstance, "TestPage");
        page.Add(new Achievement(page, ModInstance, "First", rewards: [new ItemReward(ItemID.SilverCoin, 20)]) { NeedSubmit = true });
        page.Add(new Achievement(page, ModInstance, "Workbench",
            requirements: [new CraftItemRequirement(ItemID.WorkBench)],
            rewards: [new ItemReward(ItemID.Wood, 100)])
        { UseRollingRequirementTexture = true }.SetPredecessorNames(["Wood"]));
        page.Add(new Achievement(page, ModInstance, "WoodTools",
            requirements: [
                new CraftItemRequirement(ItemID.WoodenSword),
                new CraftItemRequirement(ItemID.WoodHelmet),
                new CraftItemRequirement(ItemID.WoodBreastplate)
            ],
            rewards: [new ItemReward(ItemID.IronskinPotion, 5)])
        { UseRollingRequirementTexture = true }.SetPredecessorNames(["Workbench"]));
        page.Add(new Achievement(page, ModInstance, "House",
            requirements: [new HouseRequirement()],
            rewards: [new ItemReward(ItemID.Wood, 100)]).SetPredecessorNames(["Workbench", "Torch"]));
        page.Add(new Achievement(page, ModInstance, "Slime",
            requirements: [new KillNPCRequirement(NPCID.BlueSlime)],
            rewards: [new ItemReward(ItemID.Gel, 30)])
        { UseRollingRequirementTexture = true });
        page.Add(new Achievement(page, ModInstance, "Torch",
            requirements: [new CraftItemRequirement(ItemID.Torch)],
            rewards: [new ItemReward(ItemID.Torch, 100)])
        { UseRollingRequirementTexture = true }.SetPredecessorNames(["Wood", "Slime"]));
        page.Add(new Achievement(page, ModInstance, "Wood",
            requirements: [new PickItemRequirement(ItemID.Wood)],
            rewards: [new ItemReward(ItemID.Apple)])
        { UseRollingRequirementTexture = true }.SetPredecessorNames(["First"]));
        page.Add(new Achievement(page, ModInstance, "Wood In World",
            requirements: [new CraftItemInWorldRequirement(ItemID.Wood, 9999)])
        { UseRollingRequirementTexture = true });
        page.Add(new Achievement(page, ModInstance, "Combine Test",
            requirements: [new PickItemRequirement(ItemID.Zenith), new CombineRequirement(
                    new PickItemRequirement(ItemID.DirtBlock, 10),
                    new CombineRequirement(2,
                        new PickItemRequirement(ItemID.GoldCoin),
                        new PickItemRequirement(ItemID.SilverCoin),
                        new EmptyRequirement()
                    )
                )],
            rewards: [new EmptyReward(), new CombineReward(2,
                    new ItemReward(ItemID.Apple),
                    new ItemReward(ItemID.Banana),
                    new CombineReward(3,
                        new ItemReward(ItemID.Cactus),
                        new ItemReward(ItemID.DesertTorch),
                        new ItemReward(ItemID.Eggnog),
                        new ItemReward(ItemID.Gel),
                        new ItemReward(ItemID.Hay)
                    )
                )])
        {
            Repeatable = true
        });
    }
    #endregion

    #region Pages
    /// <summary>
    /// 存有所有的成就页, 键为<see cref="AchievementPage.FullName"/>
    /// </summary>
    public static IReadOnlyDictionary<string, AchievementPage> Pages => pages;
    private static readonly Dictionary<string, AchievementPage> pages = [];
    public static IReadOnlyDictionary<Mod, Dictionary<string, AchievementPage>> PagesByMod => pagesByMod;
    private static readonly Dictionary<Mod, Dictionary<string, AchievementPage>> pagesByMod = [];
    /// <summary>
    /// 尝试添加一个页面
    /// </summary>
    /// <param name="forceAdd">如果有同名页则替换它</param>
    /// <returns>是否成功添加</returns>
    public static bool TryAddPage(AchievementPage page, bool forceAdd = false)
    {
        if (pages.TryGetValue(page.FullName, out var existingPage))
        {
            if (existingPage == page)
            {
                return true;
            }
            if (forceAdd)
            {
                pages[page.FullName] = page;
                pagesByMod[page.Mod][page.Name] = page;
                return true;
            }
            return false;
        }
        pages.Add(page.FullName, page);
        if (!pagesByMod.TryGetValue(page.Mod, out var modPages))
        {
            modPages = [];
            pagesByMod.Add(page.Mod, modPages);
        }
        modPages.Add(page.Name, page);
        return true;
    }
    public static bool RemovePage(AchievementPage page)
    {
        if (!pages.Remove(page.FullName))
        {
            return false;
        }
        pagesByMod[page.Mod].Remove(page.Name);
        return true;
    }
    /// <summary>
    /// 在保证没有同名页时才能调用此方法以添加页面, 否则会报错
    /// </summary>
    internal static void AddPage(AchievementPage page)
    {
        pages.Add(page.FullName, page);
        if (!pagesByMod.TryGetValue(page.Mod, out var modPages))
        {
            modPages = [];
            pagesByMod.Add(page.Mod, modPages);
        }
        modPages.Add(page.Name, page);
    }
    /// <summary>
    /// 获得页面的索引, 若页面不在其中, 返回 -1
    /// </summary>
    public static int GetIndexOfPage(AchievementPage page) => pages.GetIndexByKey(page.FullName);
    /// <summary>
    /// 获得全名对应的页面的索引
    /// </summary>
    public static int GetIndexByFullName(string pageFullName) => pages.GetIndexByKey(pageFullName);
    /// <summary>
    /// 根据索引获得页面, 若索引超界则返回空
    /// </summary>
    public static AchievementPage? GetPageByIndex(int index) => pages.GetValueByIndexS(index);
    /// <summary>
    /// 根据索引获得页面, 若索引超界则报错
    /// </summary>
    public static AchievementPage GetPageByIndexF(int index) => pages.GetValueByIndex(index);
    #endregion

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
    public static event Action? OnStaticDataLoaded;
    public void SaveStaticData(TagCompound tag)
    {
        this.SaveStaticDataTemplate(Pages.Values, p => p.FullName, "Pages", tag);
    }
    public void LoadStaticData(TagCompound tag)
    {
        this.LoadStaticDataTemplate(fullName => Pages.TryGetValue(fullName, out var p) ? p : null,
            (p, m, n) => { p.Mod = m; p.Name = n; }, (f, p) => AddPage(p), "Pages", tag);
    }
    const string StaticDataFileIdentifier = "AchievementStaticDataForProgressSystem";
    private static void LoadStaticDataFromAllLoadedMod()
    {
        foreach (var mod in ModLoader.Mods)
        {
            var dataPaths = mod.GetFileNames()?.Where(s => s.EndsWith(".dat"));
            if (dataPaths == null)
            {
                continue;
            }
            foreach (var dataPath in dataPaths)
            {
                using var stream = mod.GetFileStream(dataPath, true);
                var tag = TagIO.FromStream(stream);
                if (!tag.TryGet("type", out string identifier) || identifier != StaticDataFileIdentifier)
                {
                    continue;
                }
                if (!tag.TryGet("data", out object data) || data is not TagCompound dataTag)
                {
                    continue;
                }
                LoadStaticDataStatic(dataTag);
            }
        }
        OnStaticDataLoaded?.Invoke();
    }
    public static void SaveStaticDataToStream(Stream stream)
    {
        TagCompound dataTag = [], tag = [];
        SaveStaticDataStatic(dataTag);
        tag["data"] = dataTag;
        tag["type"] = StaticDataFileIdentifier;
        TagIO.ToStream(tag, stream);
    }
    #endregion

    #region 网络同步
    public static bool NeedNetUpdate { get; private set; }
    public static void SetNeedNetUpdate() => NeedNetUpdate = true;
    public bool NetUpdate { get; set; }
    public IEnumerable<INetUpdate> GetNetUpdateChildren() => Pages.Values;
    /// <summary>
    /// 由大到小缩减
    /// </summary>
    private static int netUpdateTimer;
    private static void Update_TryNetUpdate()
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            return;
        }
        if (netUpdateTimer > 0)
        {
            netUpdateTimer -= 1;
            return;
        }
        if (!NeedNetUpdate)
        {
            return;
        }
        NeedNetUpdate = false;
        netUpdateTimer = ServerConfig.Instance.NetUpdateFrequency;
        NetHandler.ManagerNetUpdate();
    }
    #endregion

    #region 进度
    float IProgressable.Progress => Progress;
    IEnumerable<IProgressable> IProgressable.ProgressChildren => Pages.Values;
    public static float Progress { get; private set; }
    public static void UpdateProgress()
    {
        Progress = ((IProgressable)Instance).GetProgressOfChildren();
    }
    #endregion

    #region 流程控制
    public static bool AfterPostSetup { get; private set; }
    public static void DoAfterPostSetup(Action action)
    {
        if (AfterPostSetup)
        {
            action();
        }
        OnPostInitialize += action;
    }

    public static event Action? OnPostInitialize;

    public void PostInitialize()
    {
        LoadStaticDataFromAllLoadedMod();

    }
    /// <summary>
    /// 在 PostSetup之后调用
    /// </summary>
    protected static void PostInitializeTree()
    {
        ((IAchievementNode)Instance).PostInitializeTree();
        OnPostInitialize?.Invoke();
        AfterPostSetup = true;
    }
    /// <summary>
    /// 在 <see cref="ModPlayer.OnEnterWorld"/> 中调用
    /// 此时玩家的和世界的数据都已加载完毕
    /// </summary>
    public static void StartTree() => ((IAchievementNode)Instance).StartTree();
    /// <summary>
    /// 重置所有的成就数据,
    /// 一般在世界卸载时使用
    /// </summary>
    public static void ResetTree() => ((IAchievementNode)Instance).ResetTree();
    public IEnumerable<IAchievementNode> NodeChildren => Pages.Values;
    /// <summary>
    /// 在游戏中调用, 重置所有成就的进度
    /// </summary>
    public static void Restart()
    {
        ResetTree();
        StartTree();
    }
    #endregion

    #region 重写 ModSystem 的方法
    public override void PostUpdateEverything()
    {
        Update_TryNetUpdate();
        GeneralTimer += 1;
    }
    public override void OnWorldLoad()
    {
        // 在 LoadWorldData 前执行
    }
    public override void OnWorldUnload()
    {
        // 在 SaveWorldData 后执行
        ResetTree();
    }
    #endregion

    #region 钩子
    public override void Load()
    {
        Instance = this;
        HookInPostInitialize();
    }

    private static void HookInPostInitialize()
    {
        MonoModHooks.Add(typeof(ItemLoader).GetMethod("FinishSetup", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static), OnItemLoaderFinishSetup);
    }

    private static void OnItemLoaderFinishSetup(Action orig)
    {
        orig();
        PostInitializeTree();
    }
    public override void Unload()
    {
        AfterPostSetup = false;
    }
    #endregion

    #region 杂项
    public static void TryReceiveAllRewards()
    {
        foreach (var page in Pages.Values)
        {
            page.TryReceiveAllRewards();
        }
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
        AchievementManager.StartTree();
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

