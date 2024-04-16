namespace ProgressSystem.GameEvents.Events;
public class PickItem : CountInt
{
    public int Type { get; private set; }
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
    public override void LoadData(TagCompound tag)
    {
        if (tag.TryGet(nameof(IsCompleted), out bool isCompleted))
        {
            IsCompleted = isCompleted;
        }
        if (tag.TryGet(nameof(Type), out string type))
        {
            Type = int.TryParse(type, out int num) ? num : ModContent.TryFind(type, out ModItem modItem) ? modItem.Type : -1;
        }
        base.LoadData(tag);
    }
    public override void SaveData(TagCompound tag)
    {
        tag[nameof(IsCompleted)] = IsCompleted;
        tag[nameof(Type)] = Type >= ItemID.Count ? ItemLoader.GetItem(Type).FullName : Type.ToString();
        base.SaveData(tag);
    }
    public void TryComplete(Player player, Item item)
    {
        if (item.type == Type)
        {
            Increase(item.stack);
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
        }, nameof(PickItem));
        table.AddEntry(new(typeof(int), "type"));
        table.AddEntry(new(typeof(int), "target"));
        table.Close();
        yield return table;
        yield break;
    }
}
