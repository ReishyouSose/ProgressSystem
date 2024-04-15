﻿using Microsoft.Xna.Framework.Graphics;

namespace ProgressSystem.UIEditor.ExtraUI
{
    public class UIModSlot(Texture2D tex, string modName) : UIImage(tex)
    {
        public readonly string modName = modName;
    }
}
