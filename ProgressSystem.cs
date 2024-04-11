global using RUIModule.RUISys;
global using System;
global using System.Collections.Generic;
global using Terraria;
global using Terraria.DataStructures;
global using Terraria.ID;
global using Terraria.ModLoader;
global using Terraria.ModLoader.IO;
global using Microsoft.Xna.Framework;
global using RUIModule.RUIElements;

namespace ProgressSystem
{
    public class ProgressSystem : Mod
    {
        internal static ProgressSystem Ins;
        public ProgressSystem() => Ins = this;
        public override void Load()
        {
            RUIManager.mod = this;
            AddContent<RUIManager>();
        }
    }
}