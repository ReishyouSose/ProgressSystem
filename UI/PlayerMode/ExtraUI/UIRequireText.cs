﻿using ProgressSystem.Core.Requirements;
using Terraria.GameContent;

namespace ProgressSystem.UI.PlayerMode.ExtraUI
{
    public class UIRequireText : BaseUIElement
    {
        public readonly RequirementList requirements;
        public readonly Requirement requirement;
        public readonly UIImage complete;
        public readonly UIText text;
        public UIRequireText(Requirement requirement, RequirementList requires)
        {
            requirements = requires;
            this.requirement = requirement;
            complete = new(TextureAssets.MagicPixel.Value, new(16), Color.Red);
            complete.Events.OnUpdate += evt => complete.color = requirement.Completed ? Color.Green : Color.Red;
            complete.SetCenter(20, -3, 0, 0.5f);
            Register(complete);
            string tooltip;
            if (requirement is CombineRequirement combine)
            {
                int needCount = combine.Count;
                tooltip = needCount switch
                {
                    0 => "达成下列所有",
                    1 => "达成下列任意",
                    _ => $"达成至少 {needCount} 项"
                };
            }
            else
                tooltip = requirement.DisplayName.Value ?? requirement.GetType().Name;
            text = new(requires.IndexOf(requirement) + 1 + ". " + tooltip);
            text.SetPos(30, 0);
            text.SetSize(text.TextSize);
            Register(text);

            SetSize(text.TextSize + Vector2.UnitX * 30);
        }
    }
}
