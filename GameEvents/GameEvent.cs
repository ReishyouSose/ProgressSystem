namespace ProgressSystem.GameEvents;

public abstract class GameEvent
{
    public virtual bool IsCompleted { get; protected set; }

    public event Action<GameEvent> OnCompleted;
    protected ref Action<GameEvent> _onCompleted => ref OnCompleted;
    public virtual void Complete(params object[] args)
    {
        if (IsCompleted)
        {
            return;
        }
        IsCompleted = true;
        OnCompleted?.Invoke(this);
    }
}