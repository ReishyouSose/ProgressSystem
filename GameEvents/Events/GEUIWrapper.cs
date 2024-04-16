namespace ProgressSystem.GameEvents.Events
{
    public class GEUIWrapper : GameEvent
    {
        internal GEUIWrapper(params GameEvent[] innerEvents)
        {
            _innerEvents = [.. innerEvents];
        }
        internal HashSet<GameEvent> _innerEvents;
        public ReadOnlySpan<GameEvent> InnerEvents => new([.. _innerEvents]);
        public bool AddConetent(GameEvent e)
        {
            if (_innerEvents.Add(e))
            {
                e.OnCompleted += OnInnerEventCompleted;
                return true;
            }
            return false;
        }
        public void OnInnerEventCompleted(GameEvent e)
        {

        }
        public override IEnumerable<ConstructInfoTable<GameEvent>> GetConstructInfoTables()
        {
            ConstructInfoTable<GameEvent> table = new ConstructInfoTable<GameEvent>(t =>
            {
                IEnumerator<ConstructInfoTable<GameEvent>.Entry> e = t.GetEnumerator();
                e.MoveNext();
                GameEvent[] innerEvents = e.Current.GetValue<GameEvent[]>();
                return new GEUIWrapper(innerEvents);
            }, nameof(GEUIWrapper));
            table.AddEntry(new(typeof(GameEvent[]), "innerEvents"));
            table.Close();
            yield return table;
            yield break;
        }
    }
}
