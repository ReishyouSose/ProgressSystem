namespace ProgressSystem.Core.Listeners.Hooks;

internal class BiomeListenerHook : ModPlayer
{
    class Zones(Player player)
    {
        public BitsByte this[int index] => index switch
        {
            0 => player.zone1,
            1 => player.zone2,
            2 => player.zone3,
            3 => player.zone4,
            4 => player.zone5,
            _ => throw new IndexOutOfRangeException()
        };
    }
    Zones? zones;
    readonly BitsByte[] reachedZones = new BitsByte[5];
    readonly HashSet<string> reachedModBiomes = [];
    public bool ZoneReached(int zoneId)
    {
        return reachedZones[zoneId / 8][zoneId % 8];
    }
    public bool ModBiomeReached(string modBiomeFullName)
    {
        return reachedModBiomes.Contains(modBiomeFullName);
    }
    public bool ModBiomeReached<T>() where T : ModBiome
    {
        return reachedModBiomes.Contains(ModContent.GetInstance<T>().FullName);
    }

    public override void OnEnterWorld()
    {
        zones = new(Player);
    }
    public override void PostUpdate()
    {
        if (zones == null)
        {
            return;
        }
        for (int i = 0; i < 5; ++i)
        {
            BitsByte oldReached = reachedZones[i];
            BitsByte newReached = (byte)(oldReached | zones[i]);
            if (newReached != oldReached)
            {
                reachedZones[i] = newReached;
                for (int j = 0; j < 8; ++j)
                {
                    if (newReached[j] && !oldReached[j])
                    {
                        BiomeListener.ListenReachNewZone(i * 8 + j);
                    }
                }
            }
        }
    }

    public override void SaveData(TagCompound tag)
    {
        tag["ReachedZones"] = reachedZones.Select(b => (byte)b).ToList();
        tag["ReachedModBiomes"] = reachedModBiomes.ToList();
    }
    public override void LoadData(TagCompound tag)
    {
        var savedReachedZones = tag.GetWithDefault<List<byte>>("ReachedZones")?.ToArray() ?? [];
        foreach (int i in Math.Min(reachedZones.Length, savedReachedZones.Length))
        {
            reachedZones[i] = savedReachedZones[i];
        }
        reachedModBiomes.AddRange(tag.GetWithDefault<List<string>>("ReachedModBiomes") ?? []);
    }

    public override void Load()
    {
        MonoModHooks.Add(typeof(ModBiome).GetMethod(nameof(ModBiome.OnEnter)), HookModBiomeOnEnter);
    }
    // TODO: 联机测试
    void HookModBiomeOnEnter(Action<ModBiome, Player> orig, ModBiome self, Player player)
    {
        orig(self, player);
        string fullName = self.FullName;
        if (!reachedModBiomes.Contains(fullName))
        {
            reachedModBiomes.Add(fullName);
            BiomeListener.ListenReachNewModBiome(self);
        }
    }
}
