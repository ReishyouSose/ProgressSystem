using ProgressSystem.GameEvents;

namespace ProgressSystem.Core.Requirements;

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
    protected PickItemRequirement() { }

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
    public override void SaveStaticData(TagCompound tag)
    {
        base.SaveStaticData(tag);
        if (ShouldSaveStaticData)
        {
            tag.SetWithDefault("ItemType", ItemType);
            tag.SetWithDefault("Count", Count);
        }
    }
    public override void LoadStaticData(TagCompound tag)
    {
        base.LoadStaticData(tag);
        if (ShouldSaveStaticData)
        {
            tag.GetWithDefault("ItemType", out ItemType);
            tag.GetWithDefault("Count", out Count);
        }
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
        if ((ItemType > 0 && item.type != ItemType) || Condition?.Invoke(item) == false)
        {
            return;
        }
        DoIf((CountNow += item.stack) >= Count, CompleteSafe);
    }
}
