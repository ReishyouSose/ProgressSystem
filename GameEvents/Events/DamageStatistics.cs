using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                _target = target
            };
            @event.ResetEveryTime = resetEveryTime;
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
            var table = new ConstructInfoTable<GameEvent>(t =>
            {
                var e = t.GetEnumerator();
                e.MoveNext();
                var ge = Create(e.Current.GetValue<int>());
                return ge;
            }, nameof(DamageStatistics));
            yield return table;
            yield break;
        }
    }
}