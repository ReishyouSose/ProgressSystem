using MonoMod.Utils;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Triple = (System.Collections.Generic.Dictionary<string, (System.Type type, System.Func<object?> getter, System.Action<object?> setter)> theStatic,
    System.Collections.Generic.Dictionary<string, (System.Type type, System.Func<object, object?> getter, System.Action<object, object?> setter)> theInstance,
    System.Collections.Generic.HashSet<string> notSupported);

namespace ProgressSystem.Core;

public interface IEditable
{
    /// <summary>
    /// 表示这个可编辑类型中可以编辑的数据
    /// </summary>
    IList<Entry> EditList { get; }

    public class Entry
    {
        public TextGetter DisplayName;
        public Type Type;
        public Func<object?> GetValue;
        public Action<object?> SetValue;
        public Entry(TextGetter displayName, Type type, Func<object?> getter, Action<object?> setter)
        {
            DisplayName = displayName;
            Type = type;
            GetValue = getter;
            SetValue = setter;
        }
        public static Entry? Create<TObj>(TextGetter displayName, string staticFieldOrPropertyName)
        {
            return CreateHelper.Get(typeof(TObj), staticFieldOrPropertyName, true)?.ToEntry(displayName, null);
        }
        public static Entry? Create<TObj>(TextGetter displayName, TObj obj, string instanceFieldOrPropertyName)
        {
            return CreateHelper.Get(typeof(TObj), instanceFieldOrPropertyName, false)?.ToEntry(displayName, obj);
        }
        public static Entry? Create(TextGetter displayName, Type objType, string staticFieldOrPropertyName)
        {
            return CreateHelper.Get(objType, staticFieldOrPropertyName, true)?.ToEntry(displayName, null);
        }
        public static Entry? Create(TextGetter displayName, Type objType, object obj, string instanceFieldOrPropertyName)
        {
            return CreateHelper.Get(objType, instanceFieldOrPropertyName, false)?.ToEntry(displayName, obj);
        }

        private static class CreateHelper
        {
            /*
            static readonly Dictionary<Type, (Dictionary<string, (Type type, Func<object?> getter, Action<object?> setter)> theStatic, 
                Dictionary<string, (Type type, Func<object, object?> getter, Action<object, object?> setter)> theInstance,
                HashSet<string> notSupported)> cache = [];
            */
            static readonly Dictionary<Type, Dictionary<string, EntryData?>> cache = [];
            public class EntryData
            {
                public bool IsStatic;
                public Type EntryType;
                public FastReflectionHelper.FastInvoker Getter;
                public FastReflectionHelper.FastInvoker Setter;
                public EntryData(bool isStatic, Type entryType, FastReflectionHelper.FastInvoker getter, FastReflectionHelper.FastInvoker setter)
                {
                    IsStatic = isStatic;
                    EntryType = entryType;
                    Getter = getter;
                    Setter = setter;
                }
                public Entry ToEntry(TextGetter displayName, object? obj)
                {
                    return new(displayName, EntryType, () => Getter.Invoke(obj, null), v => Setter.Invoke(obj, [v]));
                }
            }
            public static EntryData? Get(Type objType, string name, bool isStatic)
            {
                if (!cache.TryGetValue(objType, out var dict))
                {
                    dict = [];
                    cache.Add(objType, dict);
                }
                if (!dict.TryGetValue(name, out var entryData))
                {
                    entryData = Create(objType, name);
                    dict.Add(name, entryData);
                }
                if (entryData == null || entryData.IsStatic != isStatic)
                {
                    return null;
                }
                return entryData;
            }

            static EntryData? Create(Type objType, string name)
            {
                var propertyInfo = objType.GetProperty(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (propertyInfo != null)
                {
                    return Create(propertyInfo);
                }
                var fieldInfo = objType.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (fieldInfo != null)
                {
                    return Create(fieldInfo);
                }
                return null;
            }

            static EntryData? Create(PropertyInfo propertyInfo)
            {
                var getMethodInfo = propertyInfo.GetMethod;
                var setMethodInfo = propertyInfo.SetMethod;
                var type = propertyInfo.PropertyType;
                if (getMethodInfo == null || setMethodInfo == null || getMethodInfo.IsStatic != setMethodInfo.IsStatic)
                {
                    return null;
                }
                var getter = getMethodInfo.GetFastInvoker();
                var setter = setMethodInfo.GetFastInvoker();
                return new EntryData(getMethodInfo.IsStatic, propertyInfo.PropertyType, getter, setter);
            }
            static EntryData? Create(FieldInfo fieldInfo)
            {
                var fast = fieldInfo.GetFastInvoker();
                return new(fieldInfo.IsStatic, fieldInfo.FieldType, fast, fast);
            }
        }
    }
    public class Entry<T> : Entry
    {
        public new Func<T> GetValue;
        public new Action<T> SetValue;
        public Entry(TextGetter displayName, Func<T> getter, Action<T> setter) : base(displayName, typeof(T), () => getter(), v => setter((T)v!))
        {
            GetValue = getter;
            SetValue = setter;
        }
        // TODO: Create
    }
}
