using System.Collections;

namespace ProgressSystem.Core;

public class RequirementList : IList<Requirement>, IReadOnlyList<Requirement>
{
    private Achievement? Achievement;

    private readonly List<Requirement> data;

    public RequirementList(Achievement achievement, IEnumerable<Requirement>? requirements = null) : this(requirements) => Initialize(achievement);
    public RequirementList(IEnumerable<Requirement>? requirements = null) => data = requirements == null ? [] : [.. requirements];
    public void Initialize(Achievement achievement)
    {
        Achievement = achievement;
        foreach (var requirement in data)
        {
            requirement.Initialize(achievement);
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
            data[index] = value;
            if (Achievement != null)
            {
                value.Initialize(Achievement);
            }
        }
    }

    public void Add(Requirement requirement)
    {
        data.Add(requirement);
        if (Achievement != null)
        {
            requirement.Initialize(Achievement);
        }
    }
    public void Insert(int index, Requirement requirement)
    {
        data.Insert(index, requirement);
        if (Achievement != null)
        {
            requirement.Initialize(Achievement);
        }
    }

    public bool Contains(Requirement requirement) => data.Contains(requirement);
    public int IndexOf(Requirement requirement) => data.IndexOf(requirement);
    public IEnumerator<Requirement> GetEnumerator() => data.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => data.GetEnumerator();

    public bool Remove(Requirement requirement) => data.Remove(requirement);
    public void RemoveAt(int index) => data.RemoveAt(index);
    public void Clear() => data.Clear();

    public void CopyTo(Requirement[] array, int arrayIndex) => Range(Count).ForeachDo(i => array[arrayIndex + i] = this[i]);
}
