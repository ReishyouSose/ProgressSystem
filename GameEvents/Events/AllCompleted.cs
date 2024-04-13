namespace ProgressSystem.GameEvents.Events;

public class AllCompleted : GameEvent
{
    GameEvent[] _events;
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
        var table = new ConstructInfoTable<GameEvent>(t =>
        {
            var e = t.GetEnumerator();
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
