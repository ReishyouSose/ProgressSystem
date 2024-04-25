namespace ProgressSystem.Core.Rewards
{
    public class CombineReward : Reward
    {
        private int selectCount;
        public int SelectCount
        {
            get => selectCount;
            set => selectCount = Math.Max(value, 1);
        }
        public RewardList Rewards;
        public CombineReward()
        {
            SelectCount = 1;
            Rewards = new() { Parent = this };
        }
        protected override bool Receive() => Rewards.All(x => x.Received);
    }
}
