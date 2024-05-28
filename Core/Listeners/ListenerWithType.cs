namespace ProgressSystem.Core.Listeners;

public class ListenerWithType<T> where T : Delegate
{
    public T? Any;
    public Dictionary<int, T> WithType = [];
    public void Add(int type, T hook)
    {
        if (WithType.TryGetValue(type, out var dictValue))
        {
            WithType[type] = (T)Delegate.Combine(dictValue, hook);
        }
        else
        {
            WithType[type] = hook;
        }
    }
    public void Remove(int type, T hook)
    {
        if (!WithType.TryGetValue(type, out var dictValue))
        {
            return;
        }
        var removed = (T?)Delegate.Remove(dictValue, hook);
        if (removed == null)
        {
            WithType.Remove(type);
        }
        else
        {
            WithType[type] = removed;
        }
    }
    public void Invoke(int type, Action<T> invoker)
    {
        if (Any != null)
        {
            invoker(Any);
        }
        if (WithType.TryGetValue(type, out T? value))
        {
            invoker(value);
        }
    }
}
