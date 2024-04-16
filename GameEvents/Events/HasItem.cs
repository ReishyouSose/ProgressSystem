using System.Collections.ObjectModel;

namespace ProgressSystem.GameEvents.Events
{
    public class HasItem : GameEvent, ISaveable
    {
        public int Type { get; private set; }
        public int Target { get; private set; }
        public static HasItem Create(int type, int target = 1)
        {
            target = Math.Max(target, 1);
            HasItem @event = new()
            {
                Type = type,
                Target = target
            };
            return @event;
        }
        public static void SetUp(HasItem @event)
        {
            GEListener.OnStatisticInventory += @event.TryComplete;
            @event.OnCompleted += e => GEListener.OnStatisticInventory -= @event.TryComplete;
        }
        public static HasItem CreateAndSetUp(int type, int target = 1)
        {
            target = Math.Max(target, 1);
            HasItem @event = Create(type, target);
            SetUp(@event);
            return @event;
        }
        public void LoadData(TagCompound tag)
        {
            if (tag.TryGet(nameof(IsCompleted), out bool isCompleted))
            {
                IsCompleted = isCompleted;
            }
            if (tag.TryGet(nameof(Type), out string type))
            {
                Type = int.TryParse(type, out int num) ? num : ModContent.TryFind(type, out ModItem modItem) ? modItem.Type : -1;
            }
        }
        public void SaveData(TagCompound tag)
        {
            tag[nameof(IsCompleted)] = IsCompleted;
            tag[nameof(Type)] = Type >= ItemID.Count ? ItemLoader.GetItem(Type).FullName : Type.ToString();
        }
        public void TryComplete(Player player, ReadOnlyDictionary<int, int> invItems)
        {
            if (invItems.TryGetValue(Type, out int count) && count > Target)
            {
                Complete();
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
            }, nameof(HasItem));
            table.AddEntry(new(typeof(int), "type"));
            table.AddEntry(new(typeof(int), "target"));
            table.Close();
            yield return table;
            yield break;
        }
    }
}
