namespace ProgressSystem.GameEvents.Events
{
    public class PlayerHurt : CountInt
    {
        public static new PlayerHurt Create(int target = 1)
        {
            target = Math.Max(target, 1);
            PlayerHurt @event = new()
            {
                _target = target
            };
            return @event;
        }
        public static void SetUp(PlayerHurt @event)
        {
            GEListener.OnPlayerHurt += @event.TryComplete;
            @event.OnCompleted += e => GEListener.OnPlayerHurt -= @event.TryComplete;
        }
        public static PlayerHurt CreateAndSetUp(int target = 1, bool resetEveryTime = false)
        {
            target = Math.Max(target, 1);
            PlayerHurt @event = Create(target);
            SetUp(@event);
            return @event;
        }
        public void TryComplete(Player player, Player.HurtInfo hurtInfo)
        {
            Increase(hurtInfo.Damage);
        }
        public override IEnumerable<ConstructInfoTable<GameEvent>> GetConstructInfoTables()
        {
            ConstructInfoTable<GameEvent> table = new ConstructInfoTable<GameEvent>(t =>
            {
                IEnumerator<ConstructInfoTable<GameEvent>.Entry> e = t.GetEnumerator();
                e.MoveNext();
                int target = e.Current.GetValue<int>();
                PlayerHurt ge = Create(target);
                return ge;
            }, nameof(PlayerHurt));
            table.AddEntry(new(typeof(int), "target"));
            table.Close();
            yield return table;
            yield break;
        }
    }
}
