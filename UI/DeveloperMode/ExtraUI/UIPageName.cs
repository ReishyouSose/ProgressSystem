namespace ProgressSystem.UI.DeveloperMode.ExtraUI
{
    public class UIPageName(string value, string key = null) : UIText(value)
    {
        public readonly string key = key ?? value;
    }
}
