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

    public override void LoadData(TagCompound tag)
    {
        if (tag.TryGet(nameof(IsCompleted), out bool isCompleted))
        {
            IsCompleted = isCompleted;
        }
        if (tag.TryGet(nameof(Type), out string type))
        {
            Type = int.TryParse(type, out int num) ? num : ModContent.TryFind(type, out ModTile modTile) ? modTile.Type : -1;
        }
        base.LoadData(tag);
    }
    public override void SaveData(TagCompound tag)
    {
        tag[nameof(IsCompleted)] = IsCompleted;
        tag[nameof(Type)] = Type >= TileID.Count ? TileLoader.GetTile(Type).FullName : Type.ToString();
        base.SaveData(tag);
    }
    public void TryComplete(Player player, int x, int y, Tile tile)
    {
        if (tile.TileType == Type)
        {
            Increase(1);
        }
    }
    public override IEnumerable<ConstructInfoTable<GameEvent>> GetConstructInfoTables()
    {
        ConstructInfoTable<GameEvent> table = new ConstructInfoTable<GameEvent>(t =>
        {
            IEnumerator<ConstructInfoTable<GameEvent>.Entry> e = t.GetEnumerator();
            e.MoveNext();
            int type = e.Current.GetValue<int>();
            e.MoveNext();
            int target = e.Current.GetValue<int>();
            return Create(type, target);
        }, nameof(TileBreak));
        table.AddEntry(new(typeof(int), "type"));
        table.AddEntry(new(typeof(int), "target"));
        table.Close();
        yield return table;
        yield break;
    }
}
