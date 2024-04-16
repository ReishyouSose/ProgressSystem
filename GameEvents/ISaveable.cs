namespace ProgressSystem.GameEvents;

public interface ISaveable
{

    public abstract void SaveData(TagCompound tag);

    public abstract void LoadData(TagCompound tag);
}
