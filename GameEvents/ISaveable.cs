namespace ProgressSystem.GameEvents;

public interface ISaveable
{

    public abstract void Save(TagCompound tag);

    public abstract void Load(TagCompound tag);
}
