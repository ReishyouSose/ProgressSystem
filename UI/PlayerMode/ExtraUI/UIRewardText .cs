using ProgressSystem.Core.Rewards;
using Terraria.GameContent;

namespace ProgressSystem.UI.PlayerMode.ExtraUI
{
    public class UIRewardText : BaseUIElement
    {
        public readonly Reward reward;
        public readonly UIImage selected;
        public readonly UIText text;
        public readonly int index;
        public UIRewardText(Reward reward)
        {
            this.reward = reward;
            //color = reward.Recieve ? Color.Green : Color.Ye
            selected = new(TextureAssets.MagicPixel.Value, new(16));
            selected.Events.OnUpdate += evt => selected.color = reward.State switch
            {
                Reward.StateEnum.Locked => Color.DimGray,
                Reward.StateEnum.Unlocked => Color.White,
                Reward.StateEnum.Receiving => Color.Yellow,
                Reward.StateEnum.Received => Color.Green,
                Reward.StateEnum.Closed => Color.Gray,
                _ => Color.Black
            };
            selected.SetCenter(20, -3, 0, 0.5f);
            // selected.Info.IsHidden = !(reward.IsReceived() || reward.IsReceiving());
            Register(selected);
            string tooltip = (reward.DisplayName.Value ?? reward.GetType().Name)
            + (reward.ReportDetails(out string details) ? details : string.Empty);
            text = new(tooltip);
            text.SetPos(30, 0);
            text.SetSize(text.TextSize);

            Register(text);

            SetSize(text.TextSize + Vector2.UnitX * 30);
        }
    }
}
