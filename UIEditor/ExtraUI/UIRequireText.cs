using ProgressSystem.Core.Requirements;

namespace ProgressSystem.UIEditor.ExtraUI
{
    public class UIRequireText : BaseUIElement
    {
        public readonly RequirementList requirements;
        public readonly Requirement requirement;
        public readonly UIClose delete;
        public readonly UIText text;
        public UIRequireText(Requirement requirement, RequirementList requires)
        {
            requirements = requires;
            this.requirement = requirement;
            delete = new();
            delete.SetCenter(20, -3, 0, 0.5f);
            Register(delete);
            text = new(requirement is CombineRequirement combine ? $"至少完成 {combine.needCount} 项" :
                requirement.DisplayName.Value ?? requirement.GetType().Name);
            text.SetPos(30, 0);
            text.SetSize(text.TextSize);
            Register(text);

            SetSize(text.TextSize + Vector2.UnitX * 30);
        }
    }
}
