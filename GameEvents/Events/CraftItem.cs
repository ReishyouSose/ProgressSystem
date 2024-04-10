namespace ProgressSystem.GameEvents.Events
{
    public class CraftItem : GameEvent, ITrackable
    {
        public readonly int itemID;
        public int Stat { get; set; }
        public int Require { get; init; }
        public CraftItem(int itemID, bool register = true)
        {

        }
    }
}
