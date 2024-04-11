using System.Collections;
using System.Reflection;

namespace ProgressSystem.GameEvents
{
    public class ConstructInfoTable<T> : IEnumerable<ConstructInfoTable<T>.Entry>
    {
        List<Entry> _entries;
        Func<ConstructInfoTable<T>, T> _createFunc;
        public bool Closed { get; private set; }
        public ConstructInfoTable(Func<ConstructInfoTable<T>, T> createFunc)
        {
            Type t = typeof(T);
            _createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
            _entries = [];
            Closed = false;
        }
        public ConstructInfoTable<T> AddEntry(Entry entry)
        {
            if (!Closed)
            {
                _entries.Add(entry);
            }
            return this;
        }
        public void Close() => Closed = true;
        public IEnumerator<Entry> GetEnumerator()
        {
            foreach (var entry in _entries)
            {
                yield return entry;
            }
            yield break;
        }
        public bool TryCreate(out T result)
        {
            result = default;
            if (AllEntryMeet && Closed)
            {
                try
                {
                    result = _createFunc(this);
                    return result is not null;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public bool AllEntryMeet
        {
            get
            {
                return _entries.All(e => e.IsMet);
            }
        }
        public ConstructInfoTable<T> Clone()
        {
            var table = new ConstructInfoTable<T>(_createFunc);
            foreach (var entry in _entries)
            {
                table.AddEntry(new(entry.Type, entry.Name, entry.Important));
            }
            return table;
        }
        public static bool TryAutoCreate(out ConstructInfoTable<T> table)
        {
            table = default;
            var cs = typeof(T).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (cs is null || cs.Length == 0)
            {
                return false;
            }
            var c = cs[0];
            table = Create(c);
            return true;
        }
        public static ConstructInfoTable<T> Create(ConstructorInfo c)
        {
            ConstructInfoTable<T> table = new((t) =>
            {
                List<object> objs = [];
                foreach (var entry in t)
                {
                    objs.Add(entry.GetValue());
                }
                return (T)c.Invoke(objs.ToArray());
            });
            foreach (var p in c.GetParameters())
            {
                table.AddEntry(new Entry(p.ParameterType, p.Name));
            }
            table.Close();
            return table;
        }
        public class Entry
        {
            public readonly string Name;
            public readonly Type Type;
            /// <summary>
            /// 是否必填
            /// </summary>
            public readonly bool Important;
            private object _value;
            public Entry(Type type, string name, bool important = true)
            {
                Type = type;
                Name = name;
                Important = important;
            }
            public object GetValue() => _value;
            public EntryT GetValue<EntryT>() => (EntryT)_value;
            public bool SetValue(object value)
            {
                var v = Convert.ChangeType(value, Type);
                if (v != null)
                {
                    _value = v;
                    HasValue = true;
                    return true;
                }
                return false;
            }
            /// <summary>
            /// 是否至少填入一次合法参数
            /// </summary>
            public bool HasValue { get; private set; }
            public bool IsMet
            {
                get
                {
                    if (Important)
                    {
                        return HasValue;
                    }
                    return true;
                }
            }
        }
    }
}
