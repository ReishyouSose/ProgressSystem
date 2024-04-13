using Humanizer;
using ProgressSystem.GameEvents;
using System.IO;

namespace ProgressSystem.Core;

/// <summary>
/// 达成成就所需的条件
/// </summary>
public abstract class Requirement
{
    public Achievement Achievement = null!;
    public TextGetter DisplayName;
    public TextGetter TooltipName;
    public Texture2DGetter Texture;
    protected virtual object?[] DisplayNameArgs => [];
    protected virtual object?[] TooltipNameArgs => [];
    #region 构造函数与初始化
    static Requirement()
    {
        // 在成就页解锁时尝试开始监听
        AchievementPage.OnUnlockStatic += p =>
            p.Achievements.Values.ForeachDo(a =>
                a.Requirements.ForeachDo(r =>
                    r.TryBeginListen()));
        // 在成就解锁时尝试开始监听
        Achievement.OnUnlockStatic += a =>
            a.Requirements.ForeachDo(r =>
                r.TryBeginListen());
        // 在开始时尝试开始监听写在了 Start() 中

        // 在成就完成时结束监听
        Achievement.OnCompleteStatic += a =>
        {
            foreach (var requirement in a.Requirements)
            {
                requirement.EndListenSafe();
            }
        };
    }
    public Requirement(ListenTypeEnum listenType = ListenTypeEnum.None, MultiplayerTypeEnum multiplayerType = MultiplayerTypeEnum.LocalPlayer)
    {
        ListenType = listenType;
        MultiplayerType = multiplayerType;
        Reset();
    }
    /// <summary>
    /// 初始化, 在被加入 <see cref="RequirementList"/> 时被调用
    /// </summary>
    public virtual void Initialize(Achievement achievement) {
        Achievement = achievement;
        // TODO
        if (DisplayName.IsNone)
        {
            TooltipName = achievement.Mod.GetLocalization($"Requirements.{GetType().Name}.DisplayName".FormatWith(DisplayNameArgs));
        }
        if (DisplayName.IsNone)
        {
            TooltipName = achievement.Mod.GetLocalization($"Requirements.{GetType().Name}.Tooltip".FormatWith(TooltipNameArgs));
        }
        if (Texture.IsNone)
        {
            Texture = $"{achievement.Mod.Name}/Assets/Textures/Requirements/{GetType().Name}";
        }
    }
    #endregion
    #region 重置与开始
    /// <summary>
    /// 重置
    /// 初始化时也会被调用
    /// </summary>
    public virtual void Reset()
    {
        EndListenSafe();
        Completed = false;
    }

    public virtual void Start()
    {
        TryBeginListen();
    }
    #endregion
    #region 多人类型
    public enum MultiplayerTypeEnum
    {
        /// <summary>
        /// 只处理本地玩家, 每个玩家的条件分别处理
        /// 数据会储存在玩家处
        /// </summary>
        LocalPlayer,
        /// <summary>
        /// 每个玩家分别推进, 只要有任意玩家完成了条件即算全部玩家完成
        /// 数据会储存在玩家处, 而完成情况则储存在世界处
        /// </summary>
        AnyPlayer,
        /// <summary>
        /// 世界的条件, 条件在服务器处理
        /// 数据会储存在世界中
        /// </summary>
        World
    }
    /// <summary>
    /// 多人模式类型
    /// </summary>
    public MultiplayerTypeEnum MultiplayerType;
    #endregion
    #region 数据存取
    public virtual void SaveDataInPlayer(TagCompound tag)
    {
        if (MultiplayerType == MultiplayerTypeEnum.LocalPlayer)
        {
            tag.SetWithDefault("Completed", Completed);
        }
    }
    public virtual void LoadDataInPlayer(TagCompound tag)
    {
        if (MultiplayerType == MultiplayerTypeEnum.LocalPlayer)
        {
            Completed = tag.GetWithDefault<bool>("Completed");
        }
    }
    public virtual void SaveDataInWorld(TagCompound tag)
    {
        if (MultiplayerType is MultiplayerTypeEnum.AnyPlayer or MultiplayerTypeEnum.World)
        {
            tag.SetWithDefault("Completed", Completed);
        }
    }
    public virtual void LoadDataInWorld(TagCompound tag)
    {
        if (MultiplayerType is MultiplayerTypeEnum.AnyPlayer or MultiplayerTypeEnum.World)
        {
            Completed = tag.GetWithDefault<bool>("Completed");
        }
    }
    #endregion
    #region 多人同步
    public virtual void NetSend(BinaryWriter writer)
    {
        if (MultiplayerType is MultiplayerTypeEnum.AnyPlayer or MultiplayerTypeEnum.World)
        {
            writer.Write(Completed);
        }
    }
    public virtual void NetReceive(BinaryReader reader)
    {
        if (MultiplayerType is MultiplayerTypeEnum.AnyPlayer or MultiplayerTypeEnum.World)
        {
            if (reader.ReadBoolean())
            {
                CompleteSafe();
            }
        }
    }
    #endregion
    #region 监听
    public enum ListenTypeEnum
    {
        /// <summary>
        /// 不监听
        /// </summary>
        None,
        /// <summary>
        /// 在成就解锁时开始监听
        /// </summary>
        OnAchievementUnlocked,
        /// <summary>
        /// 在成就页解锁时开始监听
        /// </summary>
        OnPageUnlocked,
        /// <summary>
        /// 在进入世界时就开始监听
        /// </summary>
        OnStart,
    }
    /// <summary>
    /// 什么时候开始监听
    /// </summary>
    public ListenTypeEnum ListenType;
    public bool Listening { get; protected set; }
    public void TryBeginListen()
    {
        if (Listening || Completed)
        {
            return;
        }
        if (ListenType == ListenTypeEnum.None)
        {
            return;
        }
        if (ListenType == ListenTypeEnum.OnAchievementUnlocked && !Achievement.State.IsUnlocked())
        {
            return;
        }
        if (ListenType == ListenTypeEnum.OnPageUnlocked && Achievement.Page.State == AchievementPage.StateEnum.Locked)
        {
            return;
        }
        BeginListenSafe();
    }
    public void BeginListenSafe()
    {
        if (Listening)
        {
            return;
        }
        BeginListen();
    }
    public void EndListenSafe()
    {
        if (!Listening)
        {
            return;
        }
        EndListen();
    }
    protected virtual void BeginListen()
    {
        Listening = true;
    }
    protected virtual void EndListen()
    {
        Listening = false;
    }
    #endregion
    #region 完成状况
    public bool Completed { get; protected set; }
    public event Action? OnComplete;
    public static event Action<Requirement>? OnCompleteStatic;
    protected void DoOnComplete()
    {
        OnComplete?.Invoke();
        OnCompleteStatic?.Invoke(this);
    }
    public void CompleteSafe()
    {
        if (Completed)
        {
            return;
        }
        Complete();
    }
    protected virtual void Complete()
    {
        Completed = true;
        EndListenSafe();
        DoOnComplete();
    }
    #endregion

    public override string ToString()
    {
        return $"{GetType().Name}: {nameof(Completed)}: {Completed}, {nameof(Listening)}: {Listening}";
    }
}

public class EmptyRequirement : Requirement
{
    public override void Reset()
    {
        base.Reset();
        Completed = true;
    }
}

public abstract class RequirementCombination : Requirement
{
    public List<Requirement> Requirements;

    public RequirementCombination(IEnumerable<Requirement> requirements)
    {
        Requirements = [.. requirements];
        foreach (int i in Requirements.Count)
        {
            Requirements[i].OnComplete += () => ElementComplete(i);
        }
    }
    public override void Reset()
    {
        base.Reset();
        Requirements.ForEach(r => r.Reset());
    }
    #region 数据存取
    public override void SaveDataInPlayer(TagCompound tag)
    {
        base.SaveDataInPlayer(tag);
        tag.SaveListData("Requirements", Requirements, (r, t) => r.SaveDataInPlayer(t));
    }
    public override void LoadDataInPlayer(TagCompound tag)
    {
        base.LoadDataInPlayer(tag);
        tag.LoadListData("Requirements", Requirements, (r, t) => r.LoadDataInPlayer(t));
    }
    public override void SaveDataInWorld(TagCompound tag)
    {
        base.SaveDataInWorld(tag);
        tag.SaveListData("Requirements", Requirements, (r, t) => r.SaveDataInWorld(t));
    }
    public override void LoadDataInWorld(TagCompound tag)
    {
        base.LoadDataInWorld(tag);
        tag.LoadListData("Requirements", Requirements, (r, t) => r.LoadDataInWorld(t));
    }
    #endregion
    #region 多人同步
    public override void NetSend(BinaryWriter writer)
    {
        base.NetSend(writer);
        foreach (var requirement in Requirements)
        {
            requirement.NetSend(writer);
        }
    }
    public override void NetReceive(BinaryReader reader)
    {
        base.NetReceive(reader);
        foreach (var requirement in Requirements)
        {
            requirement.NetReceive(reader);
        }
    }
    #endregion
    #region 监听
    protected override void BeginListen()
    {
        base.BeginListen();
        foreach (var requirement in Requirements)
        {
            requirement.BeginListenSafe();
        }
    }
    protected override void EndListen()
    {
        base.EndListen();
        foreach (var requirement in Requirements)
        {
            requirement.EndListenSafe();
        }
    }
    #endregion
    #region 完成状况
    protected abstract void ElementComplete(int elementIndex);
    #endregion
}
public class AllOfRequirements(IEnumerable<Requirement> requirements) : RequirementCombination(requirements)
{
    protected override void ElementComplete(int elementIndex)
    {
        if (Requirements.All(r => r.Completed))
        {
            CompleteSafe();
        }
    }
}
public class AnyOfRequirements(IEnumerable<Requirement> requirements) : RequirementCombination(requirements)
{
    protected override void ElementComplete(int elementIndex)
    {
        CompleteSafe();
    }
}
public class SomeOfRequirements(IEnumerable<Requirement> requirements, int count) : RequirementCombination(requirements)
{
    public int Count = count;
    protected override void ElementComplete(int elementIndex)
    {
        if (Requirements.Sum(r => r.Completed.ToInt()) >= Count)
        {
            CompleteSafe();
        }
    }
}

// TODO
/// <summary>
/// 需要玩家在成就页面自行提交
/// </summary>
public class SubmitRequirement : Requirement {
    public SubmitRequirement()
    {
        Completed = true;
    }
    public override void Reset()
    {
        base.Reset();
        Completed = true;
    }
}

/// <summary>
/// 需要玩家捡到某个物品
/// </summary>
public class PickItemRequirement : Requirement
{
    public int ItemType;
    public int Count;
    public int CountNow;
    public Func<Item, bool>? Condition;
    public PickItemRequirement(int itemType, int count = 1) : this(itemType, null, count) { }
    public PickItemRequirement(Func<Item, bool> condition, int count = 1) : this(0, condition, count) { }
    protected PickItemRequirement(int itemType, Func<Item, bool>? condition, int count) : base(ListenTypeEnum.OnStart)
    {
        ItemType = itemType;
        Condition = condition;
        Count = count;
    }

    public override void Reset()
    {
        base.Reset();
        CountNow = 0;
    }

    public override void SaveDataInPlayer(TagCompound tag)
    {
        base.SaveDataInPlayer(tag);
        if (Completed)
        {
            return;
        }
        tag.SetWithDefault("CountNow", CountNow);
    }
    public override void LoadDataInPlayer(TagCompound tag)
    {
        base.LoadDataInPlayer(tag);
        if (Completed)
        {
            CountNow = Count;
            return;
        }
        tag.GetWithDefault("CountNow", out CountNow);
    }

    protected override void BeginListen()
    {
        base.BeginListen();
        GEListener.OnLocalPlayerPickItem += ListenPickItem;
        DoIf(CountNow >= Count, CompleteSafe);
    }
    protected override void EndListen()
    {
        base.EndListen();
        GEListener.OnLocalPlayerPickItem -= ListenPickItem;
    }
    private void ListenPickItem(Item item)
    {
        if (ItemType > 0 && item.type != ItemType || Condition?.Invoke(item) == false)
        {
            return;
        }
        DoIf((CountNow += item.stack) >= Count, CompleteSafe);
    }
}

/// <summary>
/// 需要玩家制作某个物品
/// </summary>
public class CraftItemRequirement : Requirement
{
    public int ItemType;
    public int Count;
    public int CountNow;
    public Func<Item, bool>? Condition;
    public CraftItemRequirement(int itemType, int count = 1) : this(itemType, null, count) { }
    public CraftItemRequirement(Func<Item, bool> condition, int count = 1) : this(0, condition, count) { }
    protected CraftItemRequirement(int itemType, Func<Item, bool>? condition, int count) : base(ListenTypeEnum.OnStart)
    {
        ItemType = itemType;
        Condition = condition;
        Count = count;
    }
    public override void Reset()
    {
        base.Reset();
        CountNow = 0;
    }
    public override void SaveDataInPlayer(TagCompound tag)
    {
        base.SaveDataInPlayer(tag);
        if (Completed)
        {
            return;
        }
        tag.SetWithDefault("CountNow", CountNow);
    }
    public override void LoadDataInPlayer(TagCompound tag)
    {
        base.LoadDataInPlayer(tag);
        if (Completed)
        {
            CountNow = Count;
            return;
        }
        tag.GetWithDefault("CountNow", out CountNow);
    }

    protected override void BeginListen()
    {
        base.BeginListen();
        GEListener.OnLocalPlayerCraftItem += ListenCraftItem;
        DoIf(CountNow >= Count, CompleteSafe);
    }
    protected override void EndListen()
    {
        base.EndListen();
        GEListener.OnLocalPlayerCraftItem -= ListenCraftItem;
    }
    private void ListenCraftItem(Item item, RecipeItemCreationContext context)
    {
        if (ItemType > 0 && item.type != ItemType || Condition?.Invoke(item) == false)
        {
            return;
        }
        DoIf((CountNow += item.stack) >= Count, CompleteSafe);
    }
}

// TODO
/// <summary>
/// 有一个房子
/// </summary>
public class HouseRequirement : Requirement { }

// TODO: NPC NetID
public class KillNPCRequirement : Requirement
{
    public int NPCType;
    public int Count;
    public int CountNow;
    public Func<NPC, bool>? Condition;
    public KillNPCRequirement(int npcType, int count = 1) : this(npcType, null, count) { }
    public KillNPCRequirement(Func<NPC, bool> condition, int count = 1) : this(0, condition, count) { }
    protected KillNPCRequirement(int npcType, Func<NPC, bool>? condition, int count) : base(ListenTypeEnum.OnStart)
    {
        NPCType = npcType;
        Condition = condition;
        Count = count;
    }

    public override void Reset()
    {
        base.Reset();
        CountNow = 0;
    }

    public override void SaveDataInPlayer(TagCompound tag)
    {
        base.SaveDataInPlayer(tag);
        tag.SetWithDefault("countNow", CountNow);
    }
    public override void LoadDataInPlayer(TagCompound tag)
    {
        base.LoadDataInPlayer(tag);
        tag.GetWithDefault("countNow", out CountNow);
    }

    protected override void BeginListen()
    {
        base.BeginListen();
        GEListener.OnLocalPlayerKillNPC += ListenKillNPC;
        DoIf(CountNow >= Count, CompleteSafe);
    }
    protected override void EndListen()
    {
        base.EndListen();
        GEListener.OnLocalPlayerKillNPC -= ListenKillNPC;
    }
    private void ListenKillNPC(NPC npc)
    {
        if (NPCType > 0 && npc.type != NPCType || Condition?.Invoke(npc) == false)
        {
            return;
        }
        DoIf((CountNow += 1) >= Count, CompleteSafe);
    }
}
