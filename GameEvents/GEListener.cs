using System;
using Terraria;

namespace ProgressSystem.GameEvents
{
    public static class GEListener
    {
        public static event Action<Player, Item> OnCraftItem;
    }
}
