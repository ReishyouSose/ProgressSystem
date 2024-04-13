using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;

namespace ProgressSystem.GameEvents.Events;

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
        GEListener.OnCreateItem += @event.TryComplete;
        @event.OnCompleted += e => GEListener.OnCreateItem -= @event.TryComplete;
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
    public override (Texture2D, Rectangle?) DrawData()
    {
        Main.instance.LoadItem(Type);
        Texture2D tex = TextureAssets.Item[Type].Value;
        int frame = Main.itemFrame[Type];
        return (tex, new Rectangle(0, 0, tex.Width, tex.Height / frame));
    }
    public void TryComplete(Player player, Item item, RecipeItemCreationContext context)
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
        }, nameof(CraftItem));
        table.AddEntry(new(typeof(int), "type"));
        table.AddEntry(new(typeof(int), "target"));
        table.Close();
        yield return table;
        yield break;
    }
}
