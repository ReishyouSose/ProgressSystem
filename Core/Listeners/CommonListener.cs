namespace ProgressSystem.Core.Listeners;

public static class CommonListener
{
    public static event Action? OnUpdate;
    internal static void ListenUpdate()
    {
        OnUpdate?.Invoke();
    }
}
