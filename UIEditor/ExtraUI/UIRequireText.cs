namespace ProgressSystem.UIEditor.ExtraUI
{
    public class UIRequireText : BaseUIElement
    {
        public readonly IList<Requirement> requirements;
        public readonly Requirement requirement;
        public readonly UIClose delete;
        public UIRequireText(Requirement requirement)
        {
            this.requirements = requirements;
            delete = new();
            delete.SetCenter(20, 0, 0, 0.5f);
            Register(delete);

            UIText text = new(requirement.DisplayName.Value ?? requirement.GetType().Name);
            text.SetPos(20, 0);
            text.SetMaxWidth(130);
            SetSize(0, text.TextSize.Y, 1);
            Register(text);

            this.requirement = requirement;
        }
    }
}
