using ProgressSystem.Core.Listeners.Hooks;

namespace ProgressSystem.Core.Listeners;

public static class BiomeListener
{
    public delegate void OnReachNewZoneDelegate(int zoneId);
    public delegate void OnReachNewModBiomeDelegate(ModBiome modBiome);

    public static event OnReachNewZoneDelegate? OnReachNewZone;
    public static event OnReachNewModBiomeDelegate? OnReachNewModBiome;

    public static bool ZoneReached(int zoneId) => Main.LocalPlayer.GetModPlayer<BiomeListenerHook>().ZoneReached(zoneId);
    public static bool ModBiomeReached(string modBiomeFullName) => Main.LocalPlayer.GetModPlayer<BiomeListenerHook>().ModBiomeReached(modBiomeFullName);
    public static bool ModBiomeReached<T>() where T : ModBiome => Main.LocalPlayer.GetModPlayer<BiomeListenerHook>().ModBiomeReached<T>();

    internal static void ListenReachNewZone(int zoneId)
    {
        OnReachNewZone?.Invoke(zoneId);
    }
    internal static void ListenReachNewModBiome(ModBiome modBiome)
    {
        OnReachNewModBiome?.Invoke(modBiome);
    }
}
