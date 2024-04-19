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
            delete.SetCenter(20, 0, 0, 0.5f);
            Register(delete);

            text = new(requirement.DisplayName.Value ?? requirement.GetType().Name);
            text.SetPos(30, 0);
            text.SetSize(text.TextSize);
            Register(text);

            SetSize(text.TextSize + Vector2.UnitX * 30);
        }
    }
}
