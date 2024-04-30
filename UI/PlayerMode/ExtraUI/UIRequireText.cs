using ProgressSystem.Core.Requirements;
using Terraria.GameContent;

namespace ProgressSystem.UI.PlayerMode.ExtraUI
{
    public class UIRequireText : BaseUIElement
    {
        public readonly IList<Requirement> requirements;
        public readonly Requirement requirement;
        public readonly UIImage complete;
        public readonly UIText text;
        public UIRequireText(Requirement requirement, IList<Requirement> requires)
        {
            requirements = requires;
            this.requirement = requirement;
            complete = new(TextureAssets.MagicPixel.Value, new(16));
            complete.Events.OnUpdate += evt => complete.color = requirement.State switch
            {
                Requirement.StateEnum.Completed => Color.Green,
                Requirement.StateEnum.Idle => Color.Red,
                Requirement.StateEnum.Closed => Color.Gray,
                _ => Color.Black
            };
            complete.SetCenter(20, -3, 0, 0.5f);
            Register(complete);
            string tooltip = requirement.DisplayName.Value ?? requirement.GetType().Name;
            text = new(requires.IndexOf(requirement) + 1 + ". " + tooltip);
            text.SetPos(30, 0);
            text.SetSize(text.TextSize);
            Register(text);

            SetSize(text.TextSize + Vector2.UnitX * 30);
        }
    }
}
