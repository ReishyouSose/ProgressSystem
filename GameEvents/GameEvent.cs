using Microsoft.Xna.Framework.Graphics;
using ProgressSystem.Core;

namespace ProgressSystem.GameEvents;

[Obsolete($"使用{nameof(Requirement)}代替, 部分功能则在{nameof(Achievement)}中, 使用{nameof(Achievement)}.{nameof(Achievement.Requirements)}获得成就的所有条件")]
public abstract class GameEvent : ILoadable
{
    [Obsolete($"使用{nameof(Requirement)}.{nameof(Requirement.Completed)}")]
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
    [Obsolete($"使用{nameof(Achievement)}.{nameof(Achievement.Texture)}获取, 并且在绘制时使用{nameof(Texture2DGetter)}.{nameof(Texture2DGetter.Value)}获得它的值")]
    public virtual (Texture2D?, Rectangle?) DrawData() => (null, null);

    public virtual void Load(Mod mod) { }
    public virtual void Unload() { }
}
