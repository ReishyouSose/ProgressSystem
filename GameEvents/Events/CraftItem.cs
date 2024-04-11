﻿namespace ProgressSystem.GameEvents.Events;

public class CraftItem : CountInt
{
    public int Type { get; private set; }
    public static CraftItem Create(int type, int target = 1)
    {
        target = Math.Max(target, 1);
        Main.instance.LoadItem(type);
        CraftItem @event = new()
        {
            Type = type,
            _target = target
        };
        return @event;
    }
    public static void SetUp(CraftItem @event)
    {
        GEListener.OnCreateItem += @event.Complete;
        @event.OnCompleted += e => GEListener.OnCreateItem -= @event.Complete;
    }
    public static CraftItem CreateAndSetUp(int type, int target = 1)
    {
        target = Math.Max(target, 1);
        CraftItem createItem = Create(type, target);
        CraftItem @event = createItem;
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
    public override void Complete(params object[] args)
    {
        if (args.Length > 3 && args[1] is Player player && args[2] is Item item && args[3] is ItemCreationContext context)
        {
            if (item.type == Type)
            {
                base.Complete(args);
            }
        }
    }
}
