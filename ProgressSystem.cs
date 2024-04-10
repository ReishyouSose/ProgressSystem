using RUIModule.RUISys;
using Terraria.ModLoader;

namespace ProgressSystem
{
	public class ProgressSystem : Mod
	{
        public override void Load()
        {
            RUIManager.mod = this;
            AddContent<RUIManager>();
        }
    }
}