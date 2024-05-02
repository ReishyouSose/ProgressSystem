using System.Collections;
using System.Reflection;

namespace ProgressSystem.Core
{
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method)]
    public class SpecializeAutoConstructAttribute : Attribute
    {
        public bool Disabled { get; set; }
        public bool EnableEvenNonPublic { get; set; }
    }

    public class ConstructInfoTable<T> : IEnumerable<ConstructInfoTable<T>.Entry>
    {
        public string Name { get; private set; }
        public string? ExtraInfo { get; private set; }

        private List<Entry> _entries;
        private Func<ConstructInfoTable<T>, T> _createFunc;
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
            foreach (Entry entry in _entries)
            {
                yield return entry;
            }
        }
        public bool TryConstruct(out T? result)
        {
            result = default;
            if (AllEntryMet && Closed)
            {
                try
                {
                    result = _createFunc(this);
                    return result is not null;
                }
                catch (Exception e)
                {
                    Main.NewText(e);
                    return false;
                }
            }
            return false;
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public bool AllEntryMet => _entries.All(e => e.IsMet);
        public ConstructInfoTable<T> Clone()
        {
            ConstructInfoTable<T> table = new(_createFunc);
            foreach (Entry entry in _entries)
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
            if (method.ReturnType != typeof(T) && !method.ReturnType.IsAssignableFrom(typeof(T)))
            {
                throw new ArgumentException($"Method return type is not assignable from {typeof(T).FullName}");
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
                table.AddEntry(new(p));
            }
            table.Close();
            return table;
        }
        public static ConstructInfoTable<T> Create(Delegate @delegate, string? extraInfo = null)
        {
            return Create(@delegate.Method, extraInfo);
        }
        public static bool TryAutoCreate<TResult>(Type type, string? extraInfo, out List<ConstructInfoTable<TResult>> tables)
        {
            if (!type.IsAssignableTo(typeof(TResult)))
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
            foreach (ConstructorInfo c in cs)
            {
                var paras = c.GetCustomAttribute<SpecializeAutoConstructAttribute>();
                bool disableForNonPublic = !c.IsPublic;
                if (paras != null)
                {
                    if (paras.Disabled)
                    {
                        continue;
                    }
                    if (paras.EnableEvenNonPublic)
                    {
                        disableForNonPublic = false;
                    }
                }
                if (disableForNonPublic)
                {
                    continue;
                }

                ConstructInfoTable<TResult> table = new((t) =>
                {
                    List<object?> objs = [];
                    foreach (ConstructInfoTable<TResult>.Entry entry in t)
                    {
                        objs.Add(entry.GetValue());
                    }
                    return (TResult)c.Invoke([.. objs]);
                }, $"Constructor of {c.DeclaringType?.FullName ?? "Anonymous"}", extraInfo);
                foreach (ParameterInfo p in c.GetParameters())
                {
                    table.AddEntry(new(p));
                }
                table.Close();
                tables.Add(table);
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
                Important = parameter.IsOptional;
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
                    _value = value is IConvertible convertible ? Convert.ChangeType(convertible, Type) : value;
                    HasValue = _value != null;
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
        public override string ToString()
        {
            string entryNames = string.Join(", ", _entries.Select(e => e.DisplayName.Value));
            return $"{nameof(ConstructInfoTable<T>)}<{typeof(T).Name}>: [{entryNames}]";
        }
    }
}
