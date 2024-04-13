namespace ProgressSystem.GameEvents.Events;

public class AnyCompleted : GameEvent
{
    GameEvent[] _events;
    public AnyCompleted(params GameEvent[] events)
    {
        _events = events;
        foreach (GameEvent e in _events)
        {
            e.OnCompleted += CheckAnyCompleted;
        }
    }
    private void CheckAnyCompleted(GameEvent obj)
    {
        if (_events.Any(e => e.IsCompleted))
        {
            Complete();
        }
    }
    public static AnyCompleted Create(params GameEvent[] innerEvents)
    {
        return new AnyCompleted(innerEvents);
    }
    public override IEnumerable<ConstructInfoTable<GameEvent>> GetConstructInfoTables()
    {
        var table = new ConstructInfoTable<GameEvent>(t =>
        {
            var e = t.GetEnumerator();
            e.MoveNext();
            GameEvent[] events = e.Current.GetValue<GameEvent[]>();
            return new AnyCompleted(events);
        }, nameof(AnyCompleted));
        table.AddEntry(new(typeof(GameEvent[]), "events"));
        table.Close();
        yield return table;
        yield break;
    }
}
