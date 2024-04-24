using ProgressSystem.Core.Rewards;

namespace ProgressSystem.UIEditor.ExtraUI
{
    public class UIRewardText : BaseUIElement
    {
        public readonly RewardList rewards;
        public readonly Reward reward;
        public readonly UIClose delete;
        public readonly UIText text;
        public UIRewardText(Reward reward, RewardList rewards)
        {
            this.rewards = rewards;
            this.reward = reward;
            delete = new();
            delete.SetCenter(20, -3, 0, 0.5f);
            Register(delete);
            text = new(reward.ReportDetails(out string details) ? details : reward.DisplayName.Value ?? reward.GetType().Name);
            text.SetPos(30, 0);
            text.SetSize(text.TextSize);
            Register(text);

            SetSize(text.TextSize + Vector2.UnitX * 30);
        }
    }
}
