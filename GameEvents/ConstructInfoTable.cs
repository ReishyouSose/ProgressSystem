using System.Collections;
using System.Reflection;

namespace ProgressSystem.GameEvents
{
    public class ConstructInfoTable<T> : IEnumerable<ConstructInfoTable<T>.Entry>
    {
        public string Name { get; private set; }
        public string? ExtraInfo { get; private set; }

        private List<Entry> _entries;
        private Func<ConstructInfoTable<T>, T> _createFunc;
        public bool Closed { get; private set; }
        public ConstructInfoTable(Func<ConstructInfoTable<T>, T> createFunc, string name = "Anonymous", string? extraInfo = null)
        {
            Type t = typeof(T);
            _createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
            Name = name;
            ExtraInfo = extraInfo;
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
            foreach (Entry entry in _entries)
            {
                yield return entry;
            }
            yield break;
        }
        public bool TryCreate(out T? result)
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
        public bool AllEntryMeet => _entries.All(e => e.IsMet);
        public ConstructInfoTable<T> Clone()
        {
            ConstructInfoTable<T> table = new ConstructInfoTable<T>(_createFunc);
            foreach (Entry entry in _entries)
            {
                table.AddEntry(new(entry.Type, entry.Name, entry.Important));
            }
            return table;
        }
        public static bool TryAutoCreate(out List<ConstructInfoTable<T>> tables)
        {
            tables = [];
            ConstructorInfo[]? cs = typeof(T).GetConstructors(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (cs is null || cs.Length == 0)
            {
                return false;
            }
            foreach (ConstructorInfo c in cs)
            {
                tables.Add(Create(c));
            }
            return true;
        }
        public static ConstructInfoTable<T> Create(ConstructorInfo c, string? extraInfo = null)
        {
            ConstructInfoTable<T> table = new((t) =>
            {
                List<object?> objs = [];
                foreach (Entry entry in t)
                {
                    objs.Add(entry.GetValue());
                }
                return (T)c.Invoke([.. objs]);
            }, $"Constructor of {c.DeclaringType?.FullName ?? "Anonymous"}", extraInfo);
            foreach (ParameterInfo p in c.GetParameters())
            {
                table.AddEntry(new(p));
            }
            table.Close();
            return table;
        }
        public static ConstructInfoTable<T> Create(MethodInfo method, string? extraInfo = null)
        {
            if (method.ReturnType != typeof(T) && !method.ReturnType.IsSubclassOf(typeof(T)))
            {
                throw new ArgumentException($"Method return type is not defined from {typeof(T).FullName}");
            }
            bool isStatic = method.IsStatic;
            ConstructInfoTable<T> table = new((t) =>
            {
                List<object?> objs = [];
                foreach (Entry entry in t)
                {
                    objs.Add(entry.GetValue());
                }
                return (T)method.Invoke(isStatic ? null : objs[0], objs.ToArray()[(isStatic ? 0 : 1)..])!;
            }, method.IsSpecialName ? method.Name : "Anonymous", extraInfo);
            foreach (ParameterInfo p in method.GetParameters())
            {
                table.AddEntry(new Entry(p.ParameterType, p.Name));
            }
            table.Close();
            return table;
        }
        public static ConstructInfoTable<T> Create(Delegate @delegate, string? extraInfo = null)
        {
            return Create(@delegate.Method, extraInfo);
        }
        public static bool Create(Type type, string? extraInfo, out List<ConstructInfoTable<object>> tables)
        {
            tables = [];
            ConstructorInfo[]? cs = type.GetConstructors(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (cs is null || cs.Length == 0)
            {
                return false;
            }
            foreach (ConstructorInfo c in cs)
            {
                ConstructInfoTable<object> table = new((t) =>
                {
                    List<object?> objs = [];
                    foreach (ConstructInfoTable<object>.Entry entry in t)
                    {
                        objs.Add(entry.GetValue());
                    }
                    return (T)c.Invoke([.. objs]);
                }, $"Constructor of {c.DeclaringType?.FullName ?? "Anonymous"}", extraInfo);
                foreach (ParameterInfo p in c.GetParameters())
                {
                    table.AddEntry(new(p.ParameterType, p.Name));
                }
            }
            return true;
        }
        public class Entry
        {
            public readonly string? Name;
            public readonly Type Type;
            /// <summary>
            /// 是否必填
            /// </summary>
            public readonly bool Important;
            private object? _value;
            public Entry(Type type, string? name, bool important = true)
            {
                Type = type;
                Name = name;
                Important = important;
            }
            public Entry(ParameterInfo parameter)
            {
                Type = parameter.ParameterType;
                Name = parameter.Name;
                Important = !parameter.HasDefaultValue;
                if (parameter.HasDefaultValue)
                {
                    SetValue(parameter.DefaultValue);
                }
            }
            public object? GetValue() => HasValue ? _value : default;
            public EntryT? GetValue<EntryT>() => HasValue ? (EntryT?)_value : default;
            public bool SetValue(object? value)
            {
                try
                {
                    _value = Convert.ChangeType(value, Type);
                    HasValue = true;
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            /// <summary>
            /// 是否至少填入一次合法参数
            /// </summary>
            public bool HasValue { get; private set; }
            public bool IsMet
            {
                get
                {
                    return !Important || HasValue;
                }
            }
        }
    }
}
