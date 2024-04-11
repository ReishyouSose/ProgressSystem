namespace ProgressSystem.GameEvents.Events;

public class TileBreak : CountInt
{
    public int Type { get; private set; }
    public static TileBreak Create(int type, int target = 1)
    {
        target = Math.Max(target, 1);
        TileBreak @event = new()
        {
            Type = type,
            _target = target
        };
        return @event;
    }
    public static void SetUp(TileBreak @event)
    {
        GEListener.OnTileBreak += @event.TryComplete;
        @event.OnCompleted += e => GEListener.OnTileBreak -= @event.TryComplete;
    }
    public static TileBreak CreateAndSetUp(int type, int target = 1)
    {
        target = Math.Max(target, 1);
        TileBreak @event = Create(type, target);
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
    public void TryComplete(Player player, int x, int y, Tile tile)
    {
        if (tile.TileType == Type)
        {
            Increase(1);
        }
    }
}
