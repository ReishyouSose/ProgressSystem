using System.Collections;

namespace ProgressSystem.Core.Requirements;

public class RequirementList : IList<Requirement>, IReadOnlyList<Requirement>
{
    private readonly List<Requirement> data;

    public event Action<Requirement>? OnAdd;
    public event Action<Requirement>? OnRemove;
    public void AddOnAddAndDo(Action<Requirement> onAdd)
    {
        OnAdd += onAdd;
        foreach (var requirement in data)
        {
            onAdd(requirement);
        }
    }

    public RequirementList(Achievement achievement, IEnumerable<Requirement>? requirements = null, Action<Requirement>? onAdd = null, Action<Requirement>? onRemove = null)
        : this(requirements, onAdd + (a => a.Initialize(achievement)), onRemove) { }
    public RequirementList(IEnumerable<Requirement>? requirements = null, Action<Requirement>? onAdd = null, Action<Requirement>? onRemove = null)
    {
        data = requirements == null ? [] : [.. requirements];
        OnAdd = onAdd;
        OnRemove = onRemove;
        if (OnAdd != null)
        {
            data.ForeachDo(OnAdd);
        }
    }

    public int Count => data.Count;
    public bool IsReadOnly => false;

    public Requirement this[int index]
    {
        get => data[index];
        set
        {
            if (data[index] == value)
            {
                return;
            }
            OnRemove?.Invoke(data[index]);
            data[index] = value;
            OnAdd?.Invoke(value);
        }
    }

    public void Add(Requirement requirement)
    {
        data.Add(requirement);
        OnAdd?.Invoke(requirement);
    }
    public void Insert(int index, Requirement requirement)
    {
        data.Insert(index, requirement);
        OnAdd?.Invoke(requirement);
    }
    public void AddRange(IEnumerable<Requirement> requirements)
    {
        data.AddRange(requirements);
        foreach (var requirement in requirements)
        {
            OnAdd?.Invoke(requirement);
        }
    }

    public bool Contains(Requirement requirement) => data.Contains(requirement);
    public int IndexOf(Requirement requirement) => data.IndexOf(requirement);
    public IEnumerator<Requirement> GetEnumerator() => data.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => data.GetEnumerator();

    public bool Remove(Requirement item)
    {
        if(data.Remove(item))
        {
            OnRemove?.Invoke(item);
            return true;
        }
        return false;
    }

    public void RemoveAt(int index)
    {
        OnRemove?.Invoke(data[index]);
        data.RemoveAt(index);
    }

    public void Clear()
    {
        if (OnRemove != null && data.Count > 0)
        {
            foreach (var item in data)
            {
                OnRemove(item);
            }
        }
        data.Clear();
    }

    public void CopyTo(Requirement[] array, int arrayIndex) => Range(Count).ForeachDo(i => array[arrayIndex + i] = this[i]);
}
