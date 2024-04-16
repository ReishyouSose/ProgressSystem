namespace ProgressSystem.GameEvents;

public static class GEManager
{
    private static Dictionary<string, GameEvent> _events = [];
    public static bool Register(string uniqueLabel, GameEvent gameEvent, bool cover = true)
    {
        if (cover)
        {
            _events[uniqueLabel] = gameEvent;
            return true;
        }
        return _events.TryAdd(uniqueLabel, gameEvent);
    }
    public static bool TryGet(string uniqueLabel, out GameEvent? gameEvent)
    {
        return _events.TryGetValue(uniqueLabel, out gameEvent);
    }
    public static TagCompound? Save(GameEvent gameEvent)
    {
        TagCompound? tag = null;
        if (gameEvent is ISaveable sge)
        {
            tag = new()
            {
                ["Type"] = gameEvent.GetType().FullName
            };
            TagCompound data = [];
            sge.SaveData(data);
            tag["data"] = data;
        }
        return tag;
    }
    public static GameEvent? Load(TagCompound tag)
    {
        GameEvent? ge = null;
        try
        {
            if (tag.TryGet("Type", out string fullName) && tag.TryGet("data", out TagCompound data))
            {
                GameEvent? e = ModContent.GetContent<GameEvent>().FirstOrDefault(e => e?.GetType().FullName == fullName && e is ISaveable, null);
                if (e is not null)
                {
                    ge = (GameEvent?)Activator.CreateInstance(e.GetType());
                    if (ge is not null)
                    {
                        ((ISaveable)ge).LoadData(data);
                        return ge;
                    }
                }
            }
        }
        catch (Exception)
        {
            ge = null;
        }
        return ge;
    }
}
