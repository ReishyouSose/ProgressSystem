namespace ProgressSystem.Core.StaticData;

public interface IWithStaticData
{
    public bool ShouldSaveStaticData { get; set; }
    public void SaveStaticData(TagCompound tag);
    public void LoadStaticData(TagCompound tag);
}
