using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ProgressSystem
{
    public class ReflectionTable : IEnumerable<ReflectionTable.Entry>
    {
        static Dictionary<Type, ReflectionTable> _tables = [];
        public static ReflectionTable GetOrCreate<T>()
        {
            Type type = typeof(T);
            if (!_tables.ContainsKey(type))
            {
                _tables[type] = new(type);
            }
            return _tables[type];
        }
        public static ReflectionTable GetOrCreate(object obj)
        {
            Type type = obj.GetType();
            if (!_tables.ContainsKey(type))
            {
                _tables[type] = new(type);
            }
            return _tables[type];
        }
        public ReflectionTable(Type type)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            foreach (var f in type.GetFields(flag))
            {
                _entries.Add(new(f));
            }
            foreach (var p in type.GetProperties(flag))
            {
                _entries.Add(new(p));
            }
            foreach (var m in type.GetMethods(flag))
            {
                _entries.Add(new(m));
            }
        }
        List<Entry> _entries = [];
        [StructLayout(LayoutKind.Explicit)]
        public struct Entry
        {
            public enum EntryTypeEnum : byte
            {
                Field,
                Property,
                Method
            }
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
            internal Entry(FieldInfo field)
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
            {
                _field = field;
                Name = field.Name;
                EntryType = EntryTypeEnum.Field;
            }
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
            internal Entry(PropertyInfo property)
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
            {
                _property = property;
                Name = property.Name;
                EntryType = EntryTypeEnum.Property;
            }
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
            internal Entry(MethodInfo method)
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
            {
                _method = method;
                Name = method.Name;
                EntryType = EntryTypeEnum.Method;
            }
            [FieldOffset(0)]
            public readonly EntryTypeEnum EntryType;
            [FieldOffset(8)]
            public readonly string Name;
            [FieldOffset(16)]
            FieldInfo _field;
            [FieldOffset(16)]
            PropertyInfo _property;
            [FieldOffset(16)]
            MethodInfo _method;
            public class ParamterRef
            {
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
                internal ParamterRef(Type type, object? value)
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
                {
                    _type = type;
                    if (value is null)
                    {
                        return;
                    }
                    if (!value.GetType().IsAssignableFrom(type))
                    {
                        throw new ArgumentException($"Can't set {value.GetType().FullName} as {type.FullName}");
                    }
                    _value = value;
                }
                object _value;
                internal readonly Type _type;
                public object GetValue() => _value;
                public bool SetValue(object value)
                {
                    try
                    {
                        _value = value is IConvertible convertible ? Convert.ChangeType(convertible, _type) : value;
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
            public Type Type
            {
                get
                {
                    return EntryType switch
                    {
                        EntryTypeEnum.Field => _field.FieldType,
                        EntryTypeEnum.Property => _property.PropertyType,
                        EntryTypeEnum.Method => _method.ReturnType,
                        _ => throw new DataException("Invalid EntryType")
                    };
                }
            }
            public bool IsStatic
            {
                get
                {
                    return EntryType switch
                    {
                        EntryTypeEnum.Field => _field.IsStatic,
                        EntryTypeEnum.Property => false,
                        EntryTypeEnum.Method => _method.IsStatic,
                        _ => throw new DataException("Invalid EntryType")
                    };
                }
            }
            public Type? ReflectedType
            {
                get
                {
                    return EntryType switch
                    {
                        EntryTypeEnum.Field => _field.ReflectedType,
                        EntryTypeEnum.Property => _property.ReflectedType,
                        EntryTypeEnum.Method => _method.ReflectedType,
                        _ => throw new DataException("Invalid EntryType")
                    };
                }
            }
            public Attribute[] Attributes
            {
                get
                {
                    object[] objs;
                    objs= EntryType switch
                    {
                        EntryTypeEnum.Field => _field.GetCustomAttributes(true),
                        EntryTypeEnum.Property => _property.GetCustomAttributes(true),
                        EntryTypeEnum.Method => _method.GetCustomAttributes(true),
                        _ => throw new DataException("Invalid EntryType")
                    };
                    List<Attribute> list = [];
                    foreach (var obj in objs)
                    {
                        if(obj is Attribute attribute)
                        {
                            list.Add(attribute);
                        }
                    }
                    return [.. list];
                }
            }
            public T? GetCustomAttribute<T>() where T : Attribute
            {
                return EntryType switch
                {
                    EntryTypeEnum.Field => _field.GetCustomAttribute<T>(),
                    EntryTypeEnum.Property => _property.GetCustomAttribute<T>(),
                    EntryTypeEnum.Method => _method.GetCustomAttribute<T>(),
                    _ => throw new DataException("Invalid EntryType")
                };
            }
            public object? GetFieldValue(object target)
            {
                if (EntryType == EntryTypeEnum.Field && CheckTarget(target))
                {
                    return _field.GetValue(target);
                }
                return null;
            }
            /// <summary>
            /// if target is struct should use <see cref="SetFieldValue(TypedReference, object)"/>
            /// </summary>
            /// <param name="target"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            public bool SetFieldValue(object target, object value)
            {
                if (EntryType == EntryTypeEnum.Field && CheckTarget(target))
                {
                    if (value.GetType().IsAssignableFrom(_field.FieldType))
                    {
                        _field.SetValue(target, value);
                        return true;
                    }
                }
                return false;
            }
            /// <summary>
            /// use __makeref(target) to create TypedReference instance.also can handle target is class
            /// </summary>
            /// <param name="target"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            public bool SetFieldValue(TypedReference target, object value)
            {
                if (EntryType == EntryTypeEnum.Field && CheckTarget(target))
                {
                    if (value.GetType().IsAssignableFrom(_field.FieldType))
                    {
                        _field.SetValueDirect(target, value);
                        return true;
                    }
                }
                return false;
            }
            public bool HasGetPorperty => EntryType == EntryTypeEnum.Property && _property?.GetMethod is not null;
            public bool HasSetPorperty => EntryType == EntryTypeEnum.Property && _property?.SetMethod is not null;
            ParamterRef[]? GetParamters(MethodInfo method)
            {
                var ps = method.GetParameters();
                ParamterRef[]? res = new ParamterRef[ps.Length];
                for (int i = 0; i < ps.Length; i++)
                {
                    var p = ps[i];
                    res[i] = new ParamterRef(p.ParameterType, null);
                }
                return res;
            }
            bool CheckParamters(MethodInfo method, ParamterRef[] paramters)
            {
                var ps = method.GetParameters();
                if (ps.Length != paramters.Length)
                {
                    return false;
                }
                for (int i = 0; i < ps.Length; i++)
                {
                    if (!paramters[i]._type.IsAssignableFrom(ps[i].ParameterType))
                    {
                        return false;
                    }
                }
                return true;
            }
            bool CheckTarget(object target)
            {
                Type? declaringType = ReflectedType;
                if (declaringType == null)
                {
                    return false;
                }
                return target.GetType().IsAssignableFrom(ReflectedType);
            }
            bool CheckTarget(TypedReference target)
            {
                Type? declaringType = ReflectedType;
                if (declaringType == null)
                {
                    return false;
                }
                return __reftype(target).IsAssignableFrom(ReflectedType);
            }
            public ParamterRef[]? GetPropertyGetParamters()
            {
                ParamterRef[]? res = null;
                if (EntryType == EntryTypeEnum.Property)
                {
                    if (_property.GetMethod is not null)
                    {
                        res = GetParamters(_property.GetMethod);
                    }
                }
                return res;
            }
            public ParamterRef[]? GetPropertySetParamters()
            {
                ParamterRef[]? res = null;
                if (EntryType == EntryTypeEnum.Property)
                {
                    if (_property.SetMethod is not null)
                    {
                        res = GetParamters(_property.SetMethod);
                    }
                }
                return res;
            }
            public ParamterRef[]? GetMethodParamters()
            {
                ParamterRef[]? res = null;
                if (EntryType == EntryTypeEnum.Method)
                {
                    res = GetParamters(_method);
                }
                return res;
            }
            public bool GetPropertyValue(object target, ParamterRef[] paramters, out object? value, bool checkParamters = true)
            {
                value = null;
                if (EntryType != EntryTypeEnum.Property || _property.GetMethod is null || !CheckTarget(target))
                {
                    return false;
                }
                if (checkParamters && !CheckParamters(_property.GetMethod, paramters))
                {
                    return false;
                }
                object[] objs = new object[paramters.Length];
                for (int i = 0; i < objs.Length; i++)
                {
                    objs[i] = paramters[i].GetValue();
                }
                value = _property.GetMethod.Invoke(target, objs);
                for (int i = 0; i < objs.Length; i++)
                {
                    paramters[i].SetValue(objs[i]);
                }
                return true;
            }
            public bool GetPropertyValue(TypedReference target, ParamterRef[] paramters, out object? value, bool checkParamters = true)
            {
                value = null;
                if (EntryType != EntryTypeEnum.Property || _property.GetMethod is null || !CheckTarget(target))
                {
                    return false;
                }
                if (checkParamters && !CheckParamters(_property.GetMethod, paramters))
                {
                    return false;
                }
                object[] objs = new object[paramters.Length];
                for (int i = 0; i < objs.Length; i++)
                {
                    objs[i] = paramters[i].GetValue();
                }
                value = _property.GetMethod.Invoke(TypedReference.ToObject(target), objs);
                for (int i = 0; i < objs.Length; i++)
                {
                    paramters[i].SetValue(objs[i]);
                }
                return true;
            }
            public bool SetPropertyValue(object target, ParamterRef[] paramters, bool checkParamters = true)
            {
                if (EntryType != EntryTypeEnum.Property || _property.SetMethod is null || !CheckTarget(target))
                {
                    return false;
                }
                if (checkParamters && !CheckParamters(_property.SetMethod, paramters))
                {
                    return false;
                }
                object[] objs = new object[paramters.Length];
                for (int i = 0; i < objs.Length; i++)
                {
                    objs[i] = paramters[i].GetValue();
                }
                _property.SetMethod.Invoke(target, objs);
                for (int i = 0; i < objs.Length; i++)
                {
                    paramters[i].SetValue(objs[i]);
                }
                return true;
            }
            public bool SetPropertyValue(TypedReference target, ParamterRef[] paramters, bool checkParamters = true)
            {
                if (EntryType != EntryTypeEnum.Property || _property.SetMethod is null || !CheckTarget(target))
                {
                    return false;
                }
                if (checkParamters && !CheckParamters(_property.SetMethod, paramters))
                {
                    return false;
                }
                object[] objs = new object[paramters.Length];
                for (int i = 0; i < objs.Length; i++)
                {
                    objs[i] = paramters[i].GetValue();
                }
                _property.SetMethod.Invoke(TypedReference.ToObject(target), objs);
                for (int i = 0; i < objs.Length; i++)
                {
                    paramters[i].SetValue(objs[i]);
                }
                return true;
            }
            public bool RunMethod(object target, ParamterRef[] paramters, out object? value, bool checkParamters = true)
            {
                value = null;
                if (EntryType != EntryTypeEnum.Method || _method is null || !CheckTarget(target))
                {
                    return false;
                }
                if (checkParamters && !CheckParamters(_method, paramters))
                {
                    return false;
                }
                object[] objs = new object[paramters.Length];
                for (int i = 0; i < objs.Length; i++)
                {
                    objs[i] = paramters[i].GetValue();
                }
                value = _method.Invoke(target, objs);
                for (int i = 0; i < objs.Length; i++)
                {
                    paramters[i].SetValue(objs[i]);
                }
                return true;
            }
            public bool RunMethod(TypedReference target, ParamterRef[] paramters, out object? value, bool checkParamters = true)
            {
                value = null;
                if (EntryType != EntryTypeEnum.Method || _method is null || !CheckTarget(target))
                {
                    return false;
                }
                if (checkParamters && !CheckParamters(_method, paramters))
                {
                    return false;
                }
                object[] objs = new object[paramters.Length];
                for (int i = 0; i < objs.Length; i++)
                {
                    objs[i] = paramters[i].GetValue();
                }
                value = _method.Invoke(TypedReference.ToObject(target), objs);
                for (int i = 0; i < objs.Length; i++)
                {
                    paramters[i].SetValue(objs[i]);
                }
                return true;
            }
        }
        public IEnumerator<Entry> GetEnumerator()
        {
            foreach (var e in _entries)
            {
                yield return e;
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
