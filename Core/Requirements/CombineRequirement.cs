using ProgressSystem.Core.Interfaces;
using ProgressSystem.Core.NetUpdate;
using ProgressSystem.Core.StaticData;

namespace ProgressSystem.Core.Requirements;

public class CombineRequirement : Requirement
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
    public CombineRequirement() : base()
    {
        Requirements = new(null, r => r.OnComplete += ElementComplete, r => r.OnComplete -= ElementComplete);
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
    public override void Initialize(Achievement achievement)
    {
        base.Initialize(achievement);
        Requirements.AddOnAddAndDo(r => r.Initialize(achievement));
    }

    public IEnumerable<IAchievementNode> NodeChildren => Requirements;

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
        this.LoadStaticDataListTemplate(Requirements.GetS, Requirements!.SetFS, "Requirements", tag);
    }
    #endregion

    #region 多人同步
    public IEnumerable<INetUpdate> GetNetUpdateChildren() => Requirements;
    #endregion

    #region 进度
    public override IEnumerable<IProgressable> ProgressChildren() => Requirements;
    public override float GetProgress() => ((IProgressable)this).GetProgressOfChildren();
    #endregion

    #region 监听
    protected override void BeginListen()
    {
        base.BeginListen();
        foreach (Requirement requirement in Requirements)
        {
            requirement.BeginListenSafe();
        }
    }
    protected override void EndListen()
    {
        base.EndListen();
        foreach (Requirement requirement in Requirements)
        {
            requirement.EndListenSafe();
        }
    }
    #endregion

    #region 完成状况

    protected void ElementComplete()
    {
        if (Count == 0)
        {
            if (Requirements.All(r => r.Completed))
            {
                CompleteSafe();
            }
            return;
        }
        if (Requirements.Sum(r => r.Completed.ToInt()) >= Count)
        {
            CompleteSafe();
        }
    }
    #endregion
}
