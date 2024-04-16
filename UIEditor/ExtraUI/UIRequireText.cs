namespace ProgressSystem.UIEditor.ExtraUI
{
    public class UIRequireText : BaseUIElement
    {
        public readonly Requirement requirement;
        public readonly UIClose delete;
        public UIRequireText(Requirement requirement)
        {
            delete = new();
            delete.SetCenter(20, 0, 0, 0.5f);
            Register(delete);

            UIText text = new(requirement.ToString());
            text.SetPos(20, 0);
            text.SetMaxWidth(130);
            SetSize(0, text.TextSize.Y, 1);
            Register(text);

            this.requirement = requirement;
        }
    }
}
