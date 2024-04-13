namespace ProgressSystem.GameEvents.Events
{
    public class TimeRange : GameEvent, IProgressable, ISaveable
    {
        const int TimePerDay = 86400;
        const int TimePerHour = 3600;
        const int DayStart = 16200;
        const int NightStart = 70200;
        double _timeMax, _timeMin;
        public double TimeMax => _timeMax;
        public double TimeMin => _timeMin;
        public float Progress => Math.Clamp((float)((GetCurrentTime() - TimeMin) / (TimeMax - TimeMin)), 0, 1);
        public override bool IsCompleted
        {
            get
            {
                var time = GetCurrentTime();
                while (time < _timeMin)
                {
                    time += TimePerDay;
                }
                return time == Math.Clamp(time, _timeMin, _timeMax);
            }
        }
        public TimeRange(double timeMin, double timeMax)
        {
            _timeMin = timeMin % TimePerDay;
            _timeMax = timeMax % TimePerDay;
            while (_timeMax < _timeMin)
            {
                _timeMax += TimePerDay;
            }
        }
        public TimeRange(int startHour, int endHour, int startMinute = 0, int endMinute = 0)
        {
            _timeMin = (startHour * TimePerHour + startMinute) % TimePerDay;
            _timeMax = (endHour * TimePerHour + endMinute) % TimePerDay;
            while (_timeMax < _timeMin)
            {
                _timeMax += TimePerDay;
            }
        }
        public static double GetCurrentTime()
        {
            if (Main.dayTime)
            {
                return DayStart + Main.time;
            }
            return (NightStart + Main.time) % TimePerDay;
        }
        public override void Complete()
        {

        }
        public void Save(TagCompound tag)
        {
            tag[nameof(_timeMin)] = _timeMin;
            tag[nameof(_timeMax)] = _timeMax;
        }
        public void Load(TagCompound tag)
        {
            tag.TryGet(nameof(_timeMin), out _timeMin);
            tag.TryGet(nameof(_timeMax), out _timeMax);
        }
        public static TimeRange DayRange()
        {
            return new TimeRange(DayStart, NightStart);
        }
        public static TimeRange NightRange()
        {
            return new TimeRange(NightStart, DayStart);
        }
        public static TimeRange Create(double timeMin, double timeMax)
        {
            return new TimeRange(timeMin, timeMax);
        }
        public override IEnumerable<ConstructInfoTable<GameEvent>> GetConstructInfoTables()
        {
            var table = new ConstructInfoTable<GameEvent>(t =>
            {
                var e = t.GetEnumerator();
                e.MoveNext();
                double timeMin = e.Current.GetValue<double>();
                e.MoveNext();
                double timeMax = e.Current.GetValue<double>();
                return Create(timeMin,timeMax);
            }, nameof(TimeRange));
            table.AddEntry(new(typeof(double), "timeMin"));
            table.AddEntry(new(typeof(double), "timeMax"));
            table.Close();
            yield return table;
            yield break;
        }
    }
}
