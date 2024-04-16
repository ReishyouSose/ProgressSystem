namespace ProgressSystem.GameEvents.Events
{
    public class CountInt : GameEvent, IProgressable, ISaveable
    {
        protected int _count, _target;
        public int Count => _count;
        public int Target => _target;
        public static CountInt Create(int target)
        {
            return new() { _target = Math.Max(target, 0) };
        }
        public virtual float Progress => Math.Clamp(_count / (float)_target, 0, 1);
        public override bool IsCompleted
        {
            get => _count == _target;
            protected set
            {
                if (value)
                {
                    _count = _target;
                }
            }
        }
        public virtual void LoadData(TagCompound tag)
        {
            tag.TryGet(nameof(_count), out _count);
            tag.TryGet(nameof(_target), out _target);
        }
        public virtual void SaveData(TagCompound tag)
        {
            tag[nameof(_count)] = _count;
            tag[nameof(_target)] = _target;
        }
        public void Increase(int count)
        {
            _count += Math.Min(_target - _count, count);
            Complete();
        }
        public override IEnumerable<ConstructInfoTable<GameEvent>> GetConstructInfoTables()
        {
            ConstructInfoTable<GameEvent> table = new ConstructInfoTable<GameEvent>(t =>
            {
                IEnumerator<ConstructInfoTable<GameEvent>.Entry> e = t.GetEnumerator();
                int target = e.Current.GetValue<int>();
                return Create(target);
            }, nameof(CountInt));
            table.AddEntry(new(typeof(int), "target"));
            table.Close();
            yield return table;
            yield break;
        }
    }
    public class CountFloat : GameEvent, IProgressable, ISaveable
    {
        protected float _count, _target;
        public float Count => _count;
        public float Target => _target;
        public static CountFloat Create(float target)
        {
            return new() { _target = Math.Max(target, 0) };
        }
        public virtual float Progress => Math.Clamp(_count / _target, 0, 1);
        public override bool IsCompleted => _count >= _target;
        public virtual void LoadData(TagCompound tag)
        {
            tag.TryGet(nameof(_count), out _count);
            tag.TryGet(nameof(_target), out _target);
        }
        public virtual void SaveData(TagCompound tag)
        {
            tag[nameof(_count)] = _count;
            tag[nameof(_target)] = _target;
        }
        public void Increase(float count)
        {
            _count += Math.Min(_target - _count, count);
            Complete();
        }
        public override IEnumerable<ConstructInfoTable<GameEvent>> GetConstructInfoTables()
        {
            ConstructInfoTable<GameEvent> table = new ConstructInfoTable<GameEvent>(t =>
            {
                IEnumerator<ConstructInfoTable<GameEvent>.Entry> e = t.GetEnumerator();
                e.MoveNext();
                float target = e.Current.GetValue<float>();
                return Create(target);
            }, nameof(CountFloat));
            table.AddEntry(new(typeof(float), "target"));
            table.Close();
            yield return table;
            yield break;
        }
    }
    public class CountDouble : GameEvent, IProgressable, ISaveable
    {
        protected double _count, _target;
        public double Count => _count;
        public double Target => _target;

        public static CountDouble Create(double target)
        {
            return new() { _target = Math.Max(target, 0) };
        }
        public virtual float Progress => (float)Math.Clamp(_count / _target, 0, 1);
        public override bool IsCompleted => _count >= _target;
        public virtual void LoadData(TagCompound tag)
        {
            tag.TryGet(nameof(_count), out _count);
            tag.TryGet(nameof(_target), out _target);
        }
        public virtual void SaveData(TagCompound tag)
        {
            tag[nameof(_count)] = _count;
            tag[nameof(_target)] = _target;
        }
        public void Increase(double count)
        {
            _count += Math.Min(_target - _count, count);
            Complete();
        }
        public override IEnumerable<ConstructInfoTable<GameEvent>> GetConstructInfoTables()
        {
            ConstructInfoTable<GameEvent> table = new ConstructInfoTable<GameEvent>(t =>
            {
                IEnumerator<ConstructInfoTable<GameEvent>.Entry> e = t.GetEnumerator();
                double target = e.Current.GetValue<double>();
                return Create(target);
            }, nameof(CountDouble));
            table.AddEntry(new(typeof(double), "target"));
            table.Close();
            yield return table;
            yield break;
        }
    }
}
