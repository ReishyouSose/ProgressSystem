using System.Collections;

namespace ProgressSystem.Core;

public class RewardList(Achievement achievement, IEnumerable<Reward>? rewards) : IList<Reward>, IReadOnlyList<Reward>
{
    private readonly Achievement Achievement = achievement;

    private readonly List<Reward> data = rewards == null ? [] : [.. rewards.Select(r => r.WithAction(r => r.Initialize(achievement)))];
    
    public int Count => data.Count;
    public bool IsReadOnly => false;

    public Reward this[int index]
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

    public void Add(Reward reward)
    {
        data.Add(reward);
        reward.Initialize(Achievement);
    }
    public void Insert(int index, Reward reward)
    {
        data.Insert(index, reward);
        reward.Initialize(Achievement);
    }
    
    public bool Contains(Reward reward) => data.Contains(reward);
    public int IndexOf(Reward reward) => data.IndexOf(reward);
    public IEnumerator<Reward> GetEnumerator() => data.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => data.GetEnumerator();

    public bool Remove(Reward reward) => data.Remove(reward);
    public void RemoveAt(int index) => data.RemoveAt(index);
    public void Clear() => data.Clear();

    public void CopyTo(Reward[] array, int arrayIndex) => Range(Count).ForeachDo(i => array[arrayIndex + i] = this[i]);
}
