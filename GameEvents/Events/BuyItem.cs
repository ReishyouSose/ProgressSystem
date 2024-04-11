namespace ProgressSystem.GameEvents.Events;
public class BuyItem : CountInt
{
    public int Type { get; private set; }
    public static BuyItem Create(int type, int target = 1)
    {
        target = Math.Max(target, 1);
        BuyItem @event = new()
        {
            Type = type,
            _target = target
        };
        return @event;
    }
    public static void SetUp(BuyItem @event)
    {
        GEListener.OnBuyItem += @event.Complete;
        @event.OnCompleted += e => GEListener.OnBuyItem -= @event.Complete;
    }
    public static BuyItem CreateAndSetUp(int type, int target = 1)
    {
        target = Math.Max(target, 1);
        BuyItem @event = Create(type, target);
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
        if (args.Length > 4 && args[1] is Player player && args[2] is NPC npc && args[3] is Item[] items && args[4] is Item item)
        {

            if (item.type == Type)
            {
                base.Complete(args);
            }
        }
    }
}

