using Microsoft.Xna.Framework.Graphics;

namespace ProgressSystem.GameEvents;

public abstract class GameEvent : ILoadable
{
    public virtual bool IsCompleted { get; protected set; }

    public event Action<GameEvent>? OnCompleted;
    protected ref Action<GameEvent>? _onCompleted => ref OnCompleted;
    public virtual void Complete()
    {
        if (IsCompleted)
        {
            return;
        }
        IsCompleted = true;
        OnCompleted?.Invoke(this);
    }
    public virtual IEnumerable<ConstructInfoTable<GameEvent>> GetConstructInfoTables()
    {
        yield break;
    }
    public virtual (Texture2D?, Rectangle?) DrawData() => (null, null);

    public virtual void Load(Mod mod) { }
    public virtual void Unload() { }
}
