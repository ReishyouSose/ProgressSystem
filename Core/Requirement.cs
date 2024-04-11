﻿using System.IO;

namespace ProgressSystem.Core;

/// <summary>
/// 达成成就所需的条件
/// </summary>
public abstract class Requirement
{
    public TextGetter DisplayName;
    public TextGetter TooltipName;
    public Texture2DGetter Texture;
    #region 构造函数与初始化
    public Requirement() { }
    public Requirement(ListenTypeEnum listenType, MultiplayerTypeEnum multiplayerType = MultiplayerTypeEnum.LocalPlayer, bool repeatable = false)
    {
        ListenType = listenType;
        MultiplayerType = multiplayerType;
        Repeatable = repeatable;
    }
    public virtual void Initialize() { }
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
        /// 在前置完成时开始监听
        /// </summary>
        OnUnlocked,
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
    public void BeginListenSafe()
    {
        if (Listening || ListenType == ListenTypeEnum.None)
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
    public Action? OnComplete;
    public void CompleteSafe()
    {
        if (Completed == true)
        {
            return;
        }
        Complete();
    }
    protected virtual void Complete()
    {
        Completed = true;
        OnComplete?.Invoke();
        EndListenSafe();
    }
    #endregion
    #region 是否可重复完成
    /// <summary>
    /// 是否可重复完成
    /// </summary>
    public bool Repeatable;
    /// <summary>
    /// 可重复完成且已完成时, 重置进度
    /// </summary>
    public void ResetSafe()
    {
        if (!Repeatable || !Completed)
        {
            return;
        }
        Reset();
    }
    protected virtual void Reset()
    {
        Completed = false;
        BeginListenSafe();
    }
    #endregion
}

public abstract class RequirementList : Requirement
{
    public Requirement[] Requirements;

    public RequirementList(IEnumerable<Requirement> requirements)
    {
        Requirements = [.. requirements];
        foreach (int i in Requirements.Length)
        {
            Requirements[i].OnComplete += () => ElementComplete(i);
        }
    }
    #region 数据存取
    public override void SaveDataInPlayer(TagCompound tag)
    {
        base.SaveDataInPlayer(tag);
        var requirementsData = Requirements.Select(r => new TagCompound().WithAction(SaveDataInPlayer)).ToArray();
        if (requirementsData.Any(t => t.Count > 0))
        {
            tag["Requirements"] = requirementsData;
        }
    }
    public override void LoadDataInPlayer(TagCompound tag)
    {
        base.LoadDataInPlayer(tag);
        if (tag.TryGet("Requirements", out TagCompound[] requirementsData))
        {
            foreach (int i in Requirements.Length)
            {
                Requirements[i].LoadDataInPlayer(requirementsData.GetSN(i, []));
            }
        }
    }
    public override void SaveDataInWorld(TagCompound tag)
    {
        base.SaveDataInWorld(tag);
        var requirementsData = Requirements.Select(r => new TagCompound().WithAction(SaveDataInWorld)).ToArray();
        if (requirementsData.Any(t => t.Count > 0))
        {
            tag["Requirements"] = requirementsData;
        }
    }
    public override void LoadDataInWorld(TagCompound tag)
    {
        base.LoadDataInWorld(tag);
        if (tag.TryGet("Requirements", out TagCompound[] requirementsData))
        {
            foreach (int i in Requirements.Length)
            {
                Requirements[i].LoadDataInWorld(requirementsData.GetSN(i, []));
            }
        }
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
    protected virtual void ElementComplete(int elementIndex)
    {

    }
    #endregion
}

/// <summary>
/// 需要玩家在成就页面自行提交
/// </summary>
public class SubmitRequirement : Requirement { }

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
        // GEListener.OnLocalPlayerPickItem += ListenPickItem;
        DoIf(CountNow >= Count, CompleteSafe);
    }
    protected override void EndListen()
    {
        base.EndListen();
        // GEListener.OnLocalPlayerPickItem -= ListenPickItem;
    }
    private void ListenPickItem(Item item)
    {
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
        // GEListener.OnLocalPlayerCreateItem += ListenCraftItem;
        DoIf(CountNow >= Count, CompleteSafe);
    }
    protected override void EndListen()
    {
        base.EndListen();
        // GEListener.OnLocalPlayerCreateItem -= ListenCraftItem;
    }
    private void ListenCraftItem(Item item, ItemCreationContext context)
    {
        // TODO: 检测创建类型是否为制造
        DoIf((CountNow += item.stack) >= Count, CompleteSafe);
    }
}

// TODO
/// <summary>
/// 有一个房子
/// </summary>
public class HouseRequirement : Requirement { }

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
        // GEListener.OnLocalPlayerKillNPC += ListenKillNPC;
        DoIf(CountNow >= Count, CompleteSafe);
    }
    protected override void EndListen()
    {
        base.EndListen();
        // GEListener.OnLocalPlayerKillNPC -= ListenKillNPC;
    }

    private void ListenKillNPC(NPC npc)
    {
        DoIf((CountNow += 1) >= Count, CompleteSafe);
    }
}