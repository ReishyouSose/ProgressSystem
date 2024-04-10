using Microsoft.Xna.Framework.Input;
using ProgressSystem.UIEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace ProgressSystem.System
{
    public class PlayerController :ModPlayer
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
                EventEditor.Ins.OnInitialization();
            }
        }
    }
}
