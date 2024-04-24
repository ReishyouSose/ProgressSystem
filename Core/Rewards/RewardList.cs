using System.Collections;

namespace ProgressSystem.Core.Rewards;

public class RewardList : IList<Reward>, IReadOnlyList<Reward>
{
    private Achievement? Achievement;

    private readonly List<Reward> data;

    public RewardList(Achievement achievement, IEnumerable<Reward>? rewards = null) : this(rewards) => Initialize(achievement);
    public RewardList(IEnumerable<Reward>? rewards = null) => data = rewards == null ? [] : [.. rewards];
    public void Initialize(Achievement achievement)
    {
        Achievement = achievement;
        foreach (var reward in data)
        {
            reward.Initialize(achievement);
        }
    }

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
            if (Achievement != null)
            {
                value.Initialize(Achievement);
            }
        }
    }

    public void Add(Reward reward)
    {
        data.Add(reward);
        if (Achievement != null)
        {
            reward.Initialize(Achievement);
        }
    }
    public void Insert(int index, Reward reward)
    {
        data.Insert(index, reward);
        if (Achievement != null)
        {
            reward.Initialize(Achievement);
        }
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
