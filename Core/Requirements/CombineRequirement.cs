﻿using ProgressSystem.Core.NetUpdate;
using ProgressSystem.Core.StaticData;

namespace ProgressSystem.Core.Requirements;

public class CombineRequirement : Requirement
{
    public RequirementList Requirements;

    public int needCount;
    public CombineRequirement()
    {
        Requirements = [];
    }

    public CombineRequirement(int count, params Requirement[] requirements) : base()
    {
        needCount = count;
        Requirements = [.. requirements ?? []];
        for (int i = 0; i < Requirements.Count; i++)
        {
            Requirements[i].OnComplete += () => ElementComplete(i);
        }
    }
    public override void Reset()
    {
        base.Reset();
        Requirements.ForeachDo(r => r.Reset());
    }
    public override void Initialize(Achievement achievement)
    {
        base.Initialize(achievement);
        Requirements.Initialize(achievement);
    }

    #region 数据存取
    public override void SaveDataInPlayer(TagCompound tag)
    {
        base.SaveDataInPlayer(tag);
        if (ShouldSaveStaticData)
        {
            tag.SetWithDefault("Count", needCount);
        }
        tag.SaveListData("Requirements", Requirements, (r, t) => r.SaveDataInPlayer(t));
    }
    public override void LoadDataInPlayer(TagCompound tag)
    {
        base.LoadDataInPlayer(tag);
        if (ShouldSaveStaticData)
        {
            tag.GetWithDefault("Count", out needCount);
        }
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
        this.SaveStaticDataListTemplate(Requirements, "Requirements", tag);
    }
    public override void LoadStaticData(TagCompound tag)
    {
        base.LoadStaticData(tag);
        this.LoadStaticDataListTemplate(Requirements.GetS, Requirements!.SetFS, "Requirements", tag, (r, t) => Requirements.Clear());
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

    protected void ElementComplete(int elementIndex)
    {
        if (Requirements.Sum(r => r.Completed.ToInt()) >= needCount)
        {
            CompleteSafe();
        }
    }
    #endregion
}
