using ProgressSystem.Core.Requirements;
using ProgressSystem.Core.Rewards;
using ProgressSystem.TheUtils;
using System.Collections;

namespace ProgressSystem.Core.Rewards;

public class RewardList : IList<Reward>, IReadOnlyList<Reward>
{
    private readonly List<Reward> data;
    
    public event Action<Reward>? OnAdd;
    public event Action<Reward>? OnRemove;
    public void AddOnAddAndDo(Action<Reward> onAdd)
    {
        OnAdd += onAdd;
        foreach (var reward in data)
        {
            onAdd(reward);
        }
    }
    
    public RewardList(Achievement achievement, IEnumerable<Reward>? rewards = null, Action<Reward>? onAdd = null, Action<Reward>? onRemove = null)
        : this(rewards, onAdd + (a => a.Initialize(achievement)), onRemove) { }
    public RewardList(IEnumerable<Reward>? rewards = null, Action<Reward>? onAdd = null, Action<Reward>? onRemove = null)
    {
        data = rewards == null ? [] : [.. rewards];
        OnAdd = onAdd;
        OnRemove = onRemove;
        if (OnAdd != null)
        {
            data.ForeachDo(OnAdd);
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
            OnRemove?.Invoke(data[index]);
            data[index] = value;
            OnAdd?.Invoke(value);
        }
    }

    public void Add(Reward reward)
    {
        data.Add(reward);
        OnAdd?.Invoke(reward);
    }
    public void Insert(int index, Reward reward)
    {
        data.Insert(index, reward);
        OnAdd?.Invoke(reward);
    }
    public void AddRange(IEnumerable<Reward> rewards)
    {
        data.AddRange(rewards);
        foreach (var reward in rewards)
        {
            OnAdd?.Invoke(reward);
        }
    }

    public bool Contains(Reward reward) => data.Contains(reward);
    public int IndexOf(Reward reward) => data.IndexOf(reward);
    public IEnumerator<Reward> GetEnumerator() => data.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => data.GetEnumerator();

    public bool Remove(Reward item)
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
        if (OnRemove != null)
        {
            foreach (var item in data)
            {
                OnRemove(item);
            }
        }
        data.Clear();
    }

    public void CopyTo(Reward[] array, int arrayIndex) => Range(Count).ForeachDo(i => array[arrayIndex + i] = this[i]);
}
