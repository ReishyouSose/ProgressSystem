global using Microsoft.Xna.Framework;
global using ProgressSystem.Core;
global using ProgressSystem.TheUtils;
global using RUIModule.RUIElements;
global using RUIModule.RUISys;
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using Terraria;
global using Terraria.DataStructures;
global using Terraria.ID;
global using Terraria.ModLoader;
global using Terraria.ModLoader.IO;
global using static ProgressSystem.TheUtils.TigerClasses;
global using static ProgressSystem.TheUtils.TigerUtils;

namespace ProgressSystem;

public class ProgressSystem : Mod
{
    internal static ProgressSystem Instance { get; private set; }
    public override void Load()
    {
        /* RUIManager.mod =*/
        Instance = this;
        AddContent<RUIManager>();
    }
}
