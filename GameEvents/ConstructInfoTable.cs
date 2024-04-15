using MonoMod.Utils;
using System.Collections;
using System.Reflection;

namespace ProgressSystem.GameEvents
{
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method)]
    public class SpecializeAutoConstructAttribute : Attribute
    {
        public bool Disabled { get; set; }
    }

    public class ConstructInfoTable<T> : IEnumerable<ConstructInfoTable<T>.Entry>
    {
        public string Name { get; private set; }
        public string? ExtraInfo { get; private set; }
        List<Entry> _entries;
        Func<ConstructInfoTable<T>, T> _createFunc;
        public bool Closed { get; private set; }
        public ConstructInfoTable(Func<ConstructInfoTable<T>, T> createFunc, string name = "Anonymous", string? extraInfo = null)
        {
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
            foreach (var entry in _entries)
            {
                yield return entry;
            }
            yield break;
        }
        public bool TryConstruct(out T? result)
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
            var table = new ConstructInfoTable<T>(_createFunc);
            foreach (var entry in _entries)
            {
                table.AddEntry(new(entry.Type, entry.DisplayName, entry.Important));
            }
            return table;
        }
        public static bool TryAutoCreate(out List<ConstructInfoTable<T>> tables)
        {
            Type t = typeof(T);
            return TryAutoCreate(typeof(T), t.Name, out tables);
        }
        public static ConstructInfoTable<T> Create(ConstructorInfo c, string? extraInfo = null)
        {
            ConstructInfoTable<T> table = new((t) =>
            {
                List<object?> objs = [];
                foreach (var entry in t)
                {
                    objs.Add(entry.GetValue());
                }
                return (T)c.Invoke([.. objs]);
            }, $"Constructor of {c.DeclaringType?.FullName ?? "Anonymous"}", extraInfo);
            foreach (var p in c.GetParameters())
            {
                table.AddEntry(new(p));
            }
            table.Close();
            return table;
        }
        public static ConstructInfoTable<T> Create(MethodInfo method, string? extraInfo = null)
        {
            if (method.ReturnType != typeof(T) && !method.ReturnType.IsAssignableFrom(typeof(T)))
            {
                throw new ArgumentException($"Method return type is not assignable from {typeof(T).FullName}");
            }
            bool isStatic = method.IsStatic;
            ConstructInfoTable<T> table = new((t) =>
            {
                List<object?> objs = [];
                foreach (var entry in t)
                {
                    objs.Add(entry.GetValue());
                }
                return (T)method.Invoke(isStatic ? null : objs[0], objs.ToArray()[(isStatic ? 0 : 1)..])!;
            }, method.IsSpecialName ? method.Name : "Anonymous", extraInfo);
            foreach (var p in method.GetParameters())
            {
                table.AddEntry(new(p));
            }
            table.Close();
            return table;
        }
        public static ConstructInfoTable<T> Create(Delegate @delegate, string? extraInfo = null)
        {
            return Create(@delegate.Method, extraInfo);
        }
        public static bool TryAutoCreate<TResult>(Type type, string? extraInfo,out List<ConstructInfoTable<TResult>> tables)
        {
            if (type.IsAssignableTo(typeof(TResult)))
            {
                throw new ArgumentException($"Type {type.FullName} is not assignable to {typeof(TResult).FullName}");
            }
            tables = [];
            // 不要静态构造
            var cs = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (cs is null || cs.Length == 0)
            {
                return false;
            }
            foreach (var c in cs)
            {
                var paras = c.GetCustomAttribute<SpecializeAutoConstructAttribute>();
                if (paras != null)
                {
                    if (paras.Disabled)
                    {
                        continue;
                    }
                }

                ConstructInfoTable<TResult> table = new((t) =>
                {
                    List<object?> objs = [];
                    foreach (var entry in t)
                    {
                        objs.Add(entry.GetValue());
                    }
                    return (TResult)c.Invoke([.. objs]);
                }, $"Constructor of {c.DeclaringType?.FullName ?? "Anonymous"}", extraInfo);
                foreach (var p in c.GetParameters())
                {
                    table.AddEntry(new(p));
                }
            }
            return true;
        }
        public class Entry
        {
            public readonly TextGetter DisplayName;
            public readonly Type Type;
            /// <summary>
            /// 是否必填
            /// </summary>
            public readonly bool Important;
            private object? _value;
            public Entry(Type type, TextGetter name, bool important = true)
            {
                Type = type;
                DisplayName = name;
                Important = important;
            }
            public Entry(ParameterInfo parameter)
            {
                Type = parameter.ParameterType;
                DisplayName = parameter.Name;
                if (parameter.HasDefaultValue)
                {
                    SetValue(parameter.DefaultValue);
                }
            }
            public object? GetValue() => _value;
            public EntryT? GetValue<EntryT>() => (EntryT?)_value;
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
            public bool IsMet => !Important || HasValue;
        }
    }
}
