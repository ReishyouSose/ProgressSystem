namespace ProgressSystem.GameEvents.Events
{
    public class DamageStatistics : CountInt
    {
        public bool ResetEveryTime { get; private set; }
        public static DamageStatistics Create(int target = 1, bool resetEveryTime = false)
        {
            target = Math.Max(target, 1);
            DamageStatistics @event = new()
            {
                _target = target,
                ResetEveryTime = resetEveryTime
            };
            return @event;
        }
        public static void SetUp(DamageStatistics @event)
        {
            GEListener.OnDamageStatistics += @event.TryComplete;
            @event.OnCompleted += e => GEListener.OnDamageStatistics -= @event.TryComplete;
        }
        public static DamageStatistics CreateAndSetUp(int target = 1, bool resetEveryTime = false)
        {
            target = Math.Max(target, 1);
            DamageStatistics @event = Create(target);
            SetUp(@event);
            return @event;
        }
        public void TryComplete(Player player, int damage)
        {
            if (ResetEveryTime)
            {
                _count = 0;
            }
            Increase(damage);
        }
        public override IEnumerable<ConstructInfoTable<GameEvent>> GetConstructInfoTables()
        {
            ConstructInfoTable<GameEvent> table = new ConstructInfoTable<GameEvent>(t =>
            {
                IEnumerator<ConstructInfoTable<GameEvent>.Entry> e = t.GetEnumerator();
                e.MoveNext();
                int target = e.Current.GetValue<int>();
                e.MoveNext();
                bool resetEveryTime = e.Current.GetValue<bool>();
                DamageStatistics ge = Create(target, resetEveryTime);
                return ge;
            }, nameof(DamageStatistics));
            table.AddEntry(new(typeof(int), "target"));
            table.AddEntry(new(typeof(bool), "resetEveryTime", false));
            table.Close();
            yield return table;
            yield break;
        }
    }
}