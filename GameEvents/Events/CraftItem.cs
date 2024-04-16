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
    public override (Texture2D, Rectangle?) DrawData()
    {
        Main.instance.LoadItem(Type);
        Texture2D tex = TextureAssets.Item[Type].Value;
        if (Main.itemFrame.Contains(Type))
        {
            int frame = Math.Max(Main.itemFrame[Type], 1);
            return (tex, new Rectangle(0, 0, tex.Width, tex.Height / frame));
        }
        else
        {
            return (tex, null);
        }
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
        ConstructInfoTable<GameEvent> table = new ConstructInfoTable<GameEvent>(t =>
        {
            IEnumerator<ConstructInfoTable<GameEvent>.Entry> e = t.GetEnumerator();
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
