namespace ProgressSystem.GameEvents;

public static class GEM
{
    static Dictionary<string, GameEvent> _events = [];
    internal static Dictionary<string, List<ConstructInfoTable<GameEvent>>> _constructInfoTables = [];
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
    public static bool GetConstructorInfoTable(string fullName, out List<ConstructInfoTable<GameEvent>> tables)
    {
        if (_constructInfoTables.TryGetValue(fullName, out var origTables))
        {
            tables = new();
            foreach (var table in origTables)
            {
                tables.Add(table.Clone());
            }
            return true;
        }
        tables = null;
        return false;
    }
}