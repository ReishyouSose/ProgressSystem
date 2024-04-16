namespace ProgressSystem.GameEvents.Events
{
    public class TimeRange : GameEvent, IProgressable, ISaveable
    {
        private const int TimePerDay = 86400;
        private const int TimePerHour = 3600;
        private const int DayStart = 16200;
        private const int NightStart = 70200;
        private double _timeMax, _timeMin;
        public double TimeMax => _timeMax;
        public double TimeMin => _timeMin;
        public float Progress => Math.Clamp((float)((GetCurrentTime() - TimeMin) / (TimeMax - TimeMin)), 0, 1);
        public override bool IsCompleted
        {
            get
            {
                double time = GetCurrentTime();
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
            _timeMin = ((startHour * TimePerHour) + startMinute) % TimePerDay;
            _timeMax = ((endHour * TimePerHour) + endMinute) % TimePerDay;
            while (_timeMax < _timeMin)
            {
                _timeMax += TimePerDay;
            }
        }
        public static double GetCurrentTime()
        {
            return Main.dayTime ? DayStart + Main.time : (NightStart + Main.time) % TimePerDay;
        }
        public override void Complete()
        {

        }
        public void SaveData(TagCompound tag)
        {
            tag[nameof(_timeMin)] = _timeMin;
            tag[nameof(_timeMax)] = _timeMax;
        }
        public void LoadData(TagCompound tag)
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
            ConstructInfoTable<GameEvent> table = new ConstructInfoTable<GameEvent>(t =>
            {
                IEnumerator<ConstructInfoTable<GameEvent>.Entry> e = t.GetEnumerator();
                e.MoveNext();
                double timeMin = e.Current.GetValue<double>();
                e.MoveNext();
                double timeMax = e.Current.GetValue<double>();
                return Create(timeMin, timeMax);
            }, nameof(TimeRange));
            table.AddEntry(new(typeof(double), "timeMin"));
            table.AddEntry(new(typeof(double), "timeMax"));
            table.Close();
            yield return table;
            yield break;
        }
    }
}
