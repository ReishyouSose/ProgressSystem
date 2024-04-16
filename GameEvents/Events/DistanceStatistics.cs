namespace ProgressSystem.GameEvents.Events
{
    public class DistanceStatistics : CountFloat
    {
        public bool NeedContactGround { get; private set; }
        public static DistanceStatistics Create(float target = 1, bool needContactGround = false)
        {
            target = Math.Max(target, 1);
            DistanceStatistics @event = new()
            {
                _target = target,
                NeedContactGround = needContactGround
            };
            return @event;
        }
        public static void SetUp(DistanceStatistics @event)
        {
            GEListener.OnDistanceStatistics += @event.TryComplete;
            @event.OnCompleted += e => GEListener.OnDistanceStatistics -= @event.TryComplete;
        }
        public static DistanceStatistics CreateAndSetUp(float target = 1, bool needContactGround = false)
        {
            target = Math.Max(target, 1);
            DistanceStatistics @event = Create(target, needContactGround);
            SetUp(@event);
            return @event;
        }
        public void TryComplete(Player player, float distance)
        {
            if (NeedContactGround && player.velocity.Y != 0)
            {
                return;
            }
            Increase(distance);
        }
        public override IEnumerable<ConstructInfoTable<GameEvent>> GetConstructInfoTables()
        {
            ConstructInfoTable<GameEvent> table = new ConstructInfoTable<GameEvent>(t =>
            {
                IEnumerator<ConstructInfoTable<GameEvent>.Entry> e = t.GetEnumerator();
                e.MoveNext();
                int target = e.Current.GetValue<int>();
                e.MoveNext();
                bool needContactGround = e.Current.GetValue<bool>();
                DistanceStatistics ge = Create(target, needContactGround);
                return ge;
            }, nameof(DamageStatistics));
            table.AddEntry(new(typeof(int), "target"));
            table.AddEntry(new(typeof(bool), "needContactGround", false));
            table.Close();
            yield return table;
            yield break;
        }
    }
}
