﻿namespace ProgressSystem.GameEvents.Events;
public class PickItem : CountInt
{
    public int Type { get; private set; }
    public float Progress => Math.Clamp(Count / (float)Target, 0, 1);
    public static PickItem Create(int type, int target = 1)
    {
        target = Math.Max(target, 1);
        PickItem @event = new()
        {
            Type = type,
            _target = target
        };
        return @event;
    }
    public static void SetUp(PickItem @event)
    {
        GEListener.OnPickItem += @event.TryComplete;
        @event.OnCompleted += e => GEListener.OnPickItem -= @event.TryComplete;
    }
    public static PickItem CreateAndSetUp(int type, int target = 1)
    {
        target = Math.Max(target, 1);
        PickItem @event = Create(type, target);
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
    public override void Save(TagCompound tag)
    {
        tag[nameof(IsCompleted)] = IsCompleted;
        tag[nameof(Type)] = Type;
        base.Save(tag);
    }
    public void TryComplete(Player player, Item item)
    {
        if (item.type == Type)
        {
            Increase(item.stack);
        }
    }
}