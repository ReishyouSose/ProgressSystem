namespace ProgressSystem.GameEvents.Events
{
    public class CountInt : GameEvent, IProgressable, ISaveable
    {
        public int _count, _target;
        public int Count => _count;
        public int Target => _target;
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
        public virtual void Load(TagCompound tag)
        {
            tag.TryGet(nameof(_count), out _count);
            tag.TryGet(nameof(_target), out _target);
        }
        public virtual void Save(TagCompound tag)
        {
            tag[nameof(_count)] = _count;
            tag[nameof(_target)] = _target;
        }
        public void Increase(int count)
        {
            _count += Math.Min(_target - _count, count);
            Complete();
        }
    }
    public class Countfloat : GameEvent, IProgressable, ISaveable
    {
        float _count, _target;
        public float Count => _count;
        public float Target => _target;
        public virtual float Progress => Math.Clamp(_count / _target, 0, 1);
        public override bool IsCompleted => _count >= _target;
        public virtual void Load(TagCompound tag)
        {
            tag.TryGet(nameof(_count), out _count);
            tag.TryGet(nameof(_target), out _target);
        }
        public virtual void Save(TagCompound tag)
        {
            tag[nameof(_count)] = _count;
            tag[nameof(_target)] = _target;
        }
        public void Increase(float count)
        {
            _count += Math.Min(_target - _count, count);
            Complete();
        }
    }
    public class Countdouble : GameEvent, IProgressable, ISaveable
    {
        double _count, _target;
        public double Count => _count;
        public double Target => _target;
        public virtual float Progress => (float)Math.Clamp(_count / _target, 0, 1);
        public override bool IsCompleted => _count >= _target;
        public virtual void Load(TagCompound tag)
        {
            tag.TryGet(nameof(_count), out _count);
            tag.TryGet(nameof(_target), out _target);
        }
        public virtual void Save(TagCompound tag)
        {
            tag[nameof(_count)] = _count;
            tag[nameof(_target)] = _target;
        }
        public void Increase(double count)
        {
            _count += Math.Min(_target - _count, count);
            Complete();
        }
    }
}