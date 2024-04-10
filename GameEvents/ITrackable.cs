using Terraria.ModLoader.IO;

namespace ProgressSystem.GameEvents
{
    public interface ITrackable
    {
        public int Stat { get;set; }
        public int Require { get; init; }
        public void Save(TagCompound tag)=>tag["Stat"] = Stat;
        public void Load(TagCompound tag) => Stat = tag.GetInt("Stat");
    }
}