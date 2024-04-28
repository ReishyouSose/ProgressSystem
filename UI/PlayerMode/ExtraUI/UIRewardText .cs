using ProgressSystem.Core.Rewards;
using Terraria.GameContent;

namespace ProgressSystem.UI.PlayerMode.ExtraUI
{
    public class UIRewardText : BaseUIElement
    {
        public readonly RewardList rewards;
        public readonly Reward reward;
        public readonly UIImage selected;
        public readonly UIText text;
        public readonly int index;
        public UIRewardText(Reward reward, RewardList rewards)
        {
            this.rewards = rewards;
            this.reward = reward;
            //color = reward.Recieve ? Color.Green : Color.Ye
            selected = new(TextureAssets.MagicPixel.Value, new(16), reward.IsReceived() ? Color.Green : Color.Yellow);
            selected.SetCenter(20, -3, 0, 0.5f);
            selected.Info.IsHidden = !(reward.IsReceived() || reward.IsReceiving());
            Register(selected);
            string tooltip = reward is CombineReward combine ? $"选择下列中的 {combine.Count} 项" :
                (reward.DisplayName.Value ?? reward.GetType().Name)
            + (reward.ReportDetails(out string details) ? details : string.Empty);
            index = rewards.IndexOf(reward);
            text = new(index + 1 + ". " + tooltip);
            text.SetPos(30, 0);
            text.SetSize(text.TextSize);

            Register(text);

            SetSize(text.TextSize + Vector2.UnitX * 30);
        }
    }
}
