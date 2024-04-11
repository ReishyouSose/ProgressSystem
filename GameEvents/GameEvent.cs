using System.IO;
using System.Linq;
using System.Reflection;

namespace ProgressSystem.GameEvents;

public abstract class GameEvent : ILoadable
{
    public virtual bool IsCompleted { get; protected set; }

    public event Action<GameEvent> OnCompleted;
    protected ref Action<GameEvent> _onCompleted => ref OnCompleted;
    public virtual void Complete()
    {
        if (IsCompleted)
        {
            return;
        }
        IsCompleted = true;
        OnCompleted?.Invoke(this);
    }
    public void Load(Mod mod)
    {
        var cs = GetType().GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        string FullName = $"{mod.Name}.{GetType().FullName}";
        var list = GEM._constructInfoTables[FullName] = [];
        foreach (var c in cs)
        {
            var table = ConstructInfoTable<GameEvent>.Create(c);
            list.Add(table);
        }
    }
    public void Unload()
    {
    }
}