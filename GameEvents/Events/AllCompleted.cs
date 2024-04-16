namespace ProgressSystem.GameEvents.Events;

public class AllCompleted : GameEvent
{
    private GameEvent[] _events;
    public AllCompleted(params GameEvent[] events)
    {
        _events = events;
        foreach (GameEvent e in _events)
        {
            e.OnCompleted += CheckAllCompleted;
        }
    }
    private void CheckAllCompleted(GameEvent obj)
    {
        if (_events.All(e => e.IsCompleted))
        {
            Complete();
        }
    }
    public override IEnumerable<ConstructInfoTable<GameEvent>> GetConstructInfoTables()
    {
        ConstructInfoTable<GameEvent> table = new ConstructInfoTable<GameEvent>(t =>
        {
            IEnumerator<ConstructInfoTable<GameEvent>.Entry> e = t.GetEnumerator();
            e.MoveNext();
            GameEvent[] events = e.Current.GetValue<GameEvent[]>();
            return new AllCompleted(events);
        }, nameof(AllCompleted));
        table.AddEntry(new(typeof(GameEvent[]), "events"));
        table.Close();
        yield return table;
        yield break;
    }
}
