using ProgressSystem.Core.Rewards;

namespace ProgressSystem.UI.DeveloperMode.ExtraUI
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
            string tooltip = reward is CombineReward combine ? $"选择下列中的 {combine.Count} 项" :
                (reward.DisplayName.Value ?? reward.GetType().Name)
            + (reward.ReportDetails(out string details) ? details : string.Empty);
            text = new(rewards.IndexOf(reward) + 1 + ". " + tooltip);
            text.SetPos(30, 0);
            text.SetSize(text.TextSize);
            Register(text);

            SetSize(text.TextSize + Vector2.UnitX * 30);
        }
    }
}
