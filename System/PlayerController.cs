using Microsoft.Xna.Framework.Input;
using ProgressSystem.UIEditor;
using Terraria.GameInput;

namespace ProgressSystem.System
{
    public class PlayerController : ModPlayer
    {
        public static ModKeybind check;
        public override void Load()
        {
            check = KeybindLoader.RegisterKeybind(Mod, "check", Keys.K);
        }
        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (check.JustPressed)
            {
                GEEditor.Ins.OnInitialization();
            }
        }
    }
}
