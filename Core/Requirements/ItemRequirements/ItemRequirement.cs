using Terraria.GameContent;
using Terraria.GameContent.UI.Chat;
using Terraria.Localization;

namespace ProgressSystem.Core.Requirements.ItemRequirements;

public abstract class ItemRequirement : Requirement
{
    #region ItemType
    protected int itemType;
    public virtual int ItemType
    {
        get => itemType;
        set
        {
            if (itemType == value)
            {
                return;
            }
            itemType = value;
            if (itemType > 0)
            {
                itemTag = ItemTagHandler.GenerateTag(new Item(itemType, Count.WithMin(1)));
            }
            else
            {
                itemTag = null;
            }
        }
    }
    #endregion
    #region Count
    protected int count = 1;
    public virtual int Count
    {
        get => count;
        set
        {
            if (count == value)
            {
                return;
            }
            count = value;
            if (itemType > 0)
            {
                itemTag = ItemTagHandler.GenerateTag(new Item(itemType, Count.WithMin(1)));
            }
        }
    }
    #endregion
    public Func<Item, bool>? Condition;
    public LocalizedText? ConditionDescription;

    protected int countNow;
    public virtual int CountNow {
        get => countNow;
        set
        {
            countNow = value;
            if (value >= Count)
            {
                CompleteSafe();
            }
        }
    }

    protected virtual ListenTypeEnum ListenTypeOverride => ListenTypeEnum.OnStart;
    protected virtual MultiplayerTypeEnum MultiplayerTypeOverride => MultiplayerTypeEnum.LocalPlayer;

    public ItemRequirement(int itemType, int count = 1) : this(itemType, null, null, count) { }
    public ItemRequirement(Func<Item, bool> condition, LocalizedText conditionDescription, int count = 1) : this(0, condition, conditionDescription, count) { }
    protected ItemRequirement(int itemType, Func<Item, bool>? condition, LocalizedText? conditionDescription, int count) : this()
    {
        ItemType = itemType;
        Condition = condition;
        ConditionDescription = conditionDescription;
        Count = count;
    }
    protected ItemRequirement() : base()
    {
        ListenType = ListenTypeOverride;
        MultiplayerType = MultiplayerTypeOverride;
        Texture = new(() =>
        {
            if (ItemType <= 0)
            {
                return null;
            }
            Main.instance.LoadItem(ItemType);
            return TextureAssets.Item[ItemType].Value;
        });
    }

    protected string? itemTag;
    protected override object?[] DisplayNameArgs => [itemTag ?? Count + " " + ConditionDescription?.Value ?? "?"];

    public override void Reset()
    {
        base.Reset();
        CountNow = 0;
    }
    public override void SaveDataInPlayer(TagCompound tag)
    {
        base.SaveDataInPlayer(tag);
        if (State != StateEnum.Completed)
        {
            tag.SetWithDefault("CountNow", CountNow);
        }
    }
    public override void LoadDataInPlayer(TagCompound tag)
    {
        base.LoadDataInPlayer(tag);
        if (State == StateEnum.Completed)
        {
            countNow = Count;
            return;
        }
        countNow = tag.GetWithDefault<int>("CountNow");
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
            ItemType = tag.GetWithDefault<int>("ItemType");
            Count = tag.GetWithDefault<int>("Count");
        }
    }
}
