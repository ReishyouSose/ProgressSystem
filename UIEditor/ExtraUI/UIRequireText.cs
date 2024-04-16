using ProgressSystem.Core;

namespace ProgressSystem.UIEditor.ExtraUI
{
    public class UIRequireText(Requirement require) : UIText(require.DisplayName.Value)
    {
        public readonly Requirement requirement = require;
    }
}
