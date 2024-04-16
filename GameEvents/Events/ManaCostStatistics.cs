namespace ProgressSystem.GameEvents.Events
{
    public class ManaCostStatistics : CountInt
    {
        public static new ManaCostStatistics Create(int target = 1)
        {
            target = Math.Max(target, 1);
            ManaCostStatistics @event = new()
            {
                _target = target
            };
            return @event;
        }
        public static void SetUp(ManaCostStatistics @event)
        {
            GEListener.OnManaCostStatistics += @event.TryComplete;
            @event.OnCompleted += e => GEListener.OnManaCostStatistics -= @event.TryComplete;
        }
        public static ManaCostStatistics CreateAndSetUp(int target = 1)
        {
            target = Math.Max(target, 1);
            ManaCostStatistics @event = Create(target);
            SetUp(@event);
            return @event;
        }
        public void TryComplete(Player player, int ManaCost)
        {
            Increase(ManaCost);
        }
        public override IEnumerable<ConstructInfoTable<GameEvent>> GetConstructInfoTables()
        {
            ConstructInfoTable<GameEvent> table = new ConstructInfoTable<GameEvent>(t =>
            {
                IEnumerator<ConstructInfoTable<GameEvent>.Entry> e = t.GetEnumerator();
                e.MoveNext();
                int target = e.Current.GetValue<int>();
                ManaCostStatistics ge = Create(target);
                return ge;
            }, nameof(ManaCostStatistics));
            table.AddEntry(new(typeof(int), "target"));
            table.Close();
            yield return table;
            yield break;
        }
    }
}
