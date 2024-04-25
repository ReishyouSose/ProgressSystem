using Microsoft.Xna.Framework.Input;

namespace ProgressSystem.Common.Systems;

public class KeyBinds : ModSystem
{
    public static ModKeybind Check { get; private set; } = null!;
    public override void Load()
    {
        Check = KeybindLoader.RegisterKeybind(Mod, "check", Keys.K);
    }
}
