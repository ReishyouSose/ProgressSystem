namespace ProgressSystem.GameEvents.Events;

public class AnyCompleted : GameEvent
{
    GameEvent[] pre;
    public AnyCompleted(params GameEvent[] events)
    {
        pre = events;
        foreach (GameEvent e in pre)
        {
            e.OnCompleted += CheckAnyCompleted;
        }
    }
    private void CheckAnyCompleted(GameEvent obj)
    {
        if (pre.Any(e => e.IsCompleted))
        {
            Complete();
        }
    }
}
