using System.Linq;
namespace ProgressSystem.GameEvents.Events;

public class AllCompleted : GameEvent
{
    GameEvent[] pre;
    public AllCompleted(params GameEvent[] events)
    {
        pre = events;
        foreach (GameEvent e in pre)
        {
            e.OnCompleted += CheckAllCompleted;
        }
    }
    private void CheckAllCompleted(GameEvent obj)
    {
        if (pre.All(e => e.IsCompleted))
        {
            Complete();
        }
    }
}
