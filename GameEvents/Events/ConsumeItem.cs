﻿namespace ProgressSystem.GameEvents.Events;
public class ConsumeItem : CountInt
{
    public int Type { get; private set; }

    public static ConsumeItem Create(int type, int target = 1)
    {
        target = Math.Max(target, 1);
        ConsumeItem @event = new()
        {
            Type = type,
            _target = target
        };
        return @event;
    }

    public static void SetUp(ConsumeItem @event)
    {
        GEListener.OnConsumeItem += @event.TryComplete;
        @event.OnCompleted += e => GEListener.OnConsumeItem -= @event.TryComplete;
    }

    public static ConsumeItem CreateAndSetUp(int type, int target = 1)
    {
        target = Math.Max(target, 1);
        ConsumeItem @event = Create(type, target);
        SetUp(@event);
        return @event;
    }

    public override void Load(TagCompound tag)
    {
        if (tag.TryGet(nameof(IsCompleted), out bool isCompleted))
        {
            IsCompleted = isCompleted;
        }
        if (tag.TryGet(nameof(Type), out int type))
        {
            Type = type;
        }
        base.Load(tag);
    }

    public void Save(TagCompound tag)
    {
        tag[nameof(IsCompleted)] = IsCompleted;
        tag[nameof(Type)] = Type;
        base.Save(tag);
    }
    public void TryComplete(Player player, Item item)
    {
        if (item.type == Type)
        {
            Increase(1);
        }
    }
    public override IEnumerable<ConstructInfoTable<GameEvent>> GetConstructInfoTables()
    {
        var table = new ConstructInfoTable<GameEvent>(t =>
        {
            var e = t.GetEnumerator();
            e.MoveNext();
            int type = e.Current.GetValue<int>();
            e.MoveNext();
            int target = e.Current.GetValue<int>();
            return Create(type, target);
        }, nameof(ConsumeItem));
        table.AddEntry(new(typeof(int), "type"));
        table.AddEntry(new(typeof(int), "target"));
        table.Close();
        yield return table;
        yield break;
    }
}

