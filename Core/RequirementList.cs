using System.Collections;

namespace ProgressSystem.Core;

public class RequirementList(Achievement achievement, IEnumerable<Requirement>? requirements = null) : IList<Requirement>, IReadOnlyList<Requirement>
{
    private readonly Achievement Achievement = achievement;

    private readonly List<Requirement> data = requirements == null ? [] : [.. requirements.Select(r => r.WithAction(r => r.Initialize(achievement)))];

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
            value.Initialize(Achievement);
        }
    }

    public void Add(Requirement requirement)
    {
        data.Add(requirement);
        requirement.Initialize(Achievement);
    }
    public void Insert(int index, Requirement requirement)
    {
        data.Insert(index, requirement);
        requirement.Initialize(Achievement);
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
