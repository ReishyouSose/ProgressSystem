using Humanizer;
using ProgressSystem.Core.Interfaces;
using ProgressSystem.Core.NetUpdate;
using ProgressSystem.Core.StaticData;

namespace ProgressSystem.Core.Requirements;

public class CombineRequirement : Requirement, IAchievementNode
{
    public RequirementList Requirements;
    private int count;

    /// <summary>
    /// 需要的条件数, 默认 0 代表需要所有条件满足
    /// </summary>
    public int Count
    {
        get => count;
        set => count = Math.Max(value, 0);
    }
    protected bool isAll;
    public CombineRequirement() : base()
    {
        Requirements = new(null, r => r.OnComplete += TryComplete, r => r.OnComplete -= TryComplete);
        var displayNameAllOf = GetModLocalization($"Requirements.{GetType().Name}.DisplayNameAllOf");
        var displayNameAnyOf = GetModLocalization($"Requirements.{GetType().Name}.DisplayNameAnyOf");
        var displayName = GetModLocalization($"Requirements.{GetType().Name}.DisplayName");
        DisplayName = new(() => isAll ? displayNameAllOf.Value : Count == 1 ? displayNameAnyOf.Value : displayName.Value.FormatWith(Count));
    }

    public CombineRequirement(int count) : this()
    {
        Count = count;
    }

    [SpecializeAutoConstruct(Disabled = true)]
    public CombineRequirement(int count, params Requirement[]? requirements) : this(count)
    {
        if (requirements != null)
        {
            Requirements.AddRange(requirements);
        }
    }
    [SpecializeAutoConstruct(Disabled = true)]
    public CombineRequirement(params Requirement[]? requirements) : this(0, requirements) { }
    public override void Initialize(Achievement achievement)
    {
        base.Initialize(achievement);
        Requirements.AddOnAddAndDo(r => r.Initialize(achievement));
    }

    public override void PostInitialize()
    {
        base.PostInitialize();
        UpdateIsAll();
        OnStart += TryComplete;
        OnComplete += () => Requirements.ForeachDo(r => r.CloseSafe());
    }
    public void UpdateIsAll()
    {
        isAll = Count == 0 || Count >= Requirements.Count(r => r.State != StateEnum.Disabled);
    }

    IEnumerable<IAchievementNode> IAchievementNode.NodeChildren => Requirements;

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
    public override void SaveStaticData(TagCompound tag)
    {
        base.SaveStaticData(tag);
        if (ShouldSaveStaticData)
        {
            tag.SetWithDefault("Count", Count);
        }
        this.SaveStaticDataListTemplate(Requirements, "Requirements", tag);
    }
    public override void LoadStaticData(TagCompound tag)
    {
        base.LoadStaticData(tag);
        if (ShouldSaveStaticData)
        {
            Count = tag.GetWithDefault<int>("Count");
        }
        this.LoadStaticDataListTemplate(Requirements.GetS, Requirements!.SetFSF, "Requirements", tag);
    }
    #endregion

    #region 多人同步
    public IEnumerable<INetUpdate> GetNetUpdateChildren() => Requirements;
    #endregion

    #region 进度
    public override IEnumerable<IProgressable> ProgressChildren() => Requirements;
    public override float GetProgress() => ((IProgressable)this).GetProgressOfChildren();
    #endregion

    #region 完成状况
    protected void TryComplete()
    {
        if (isAll)
        {
            if (Requirements.All(r => r.State is StateEnum.Completed or StateEnum.Disabled))
            {
                CompleteSafe();
            }
            return;
        }
        if (Requirements.Sum(r => (r.State == StateEnum.Completed).ToInt()) >= Count)
        {
            CompleteSafe();
        }
    }
    #endregion
}
