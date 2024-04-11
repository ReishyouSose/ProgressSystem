namespace ProgressSystem.GameEvents;

public static class GEM
{
    static Dictionary<string, GameEvent> _events = [];
    public static bool Register(string uniqueLabel, GameEvent gameEvent, bool cover = true)
    {
        if (cover)
        {
            _events[uniqueLabel] = gameEvent;
            return true;
        }
        return _events.TryAdd(uniqueLabel, gameEvent);
    }
    public static bool TryGet(string uniqueLabel, out GameEvent gameEvent)
    {
        return _events.TryGetValue(uniqueLabel, out gameEvent);
    }
    public static bool GetConstructorInfoTable(string fullName, out ConstructInfoTable<GameEvent> table)
    {
        table = null;
        return false;
    }
}