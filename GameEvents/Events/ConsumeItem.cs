namespace ProgressSystem.GameEvents.Events;
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
    public void TryComplete(Player player, Item item)
    {
        if (item.type == Type)
        {
            Increase(1);
        }
    }
    /*public override void Load(TagCompound tag)
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
    }*/

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
        }, nameof(ConsumeItem));
        table.AddEntry(new(typeof(int), "type"));
        table.AddEntry(new(typeof(int), "target"));
        table.Close();
        yield return table;
        yield break;
    }
}

