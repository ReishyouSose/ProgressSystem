using Microsoft.Xna.Framework.Graphics;
using RUIModule;
using Terraria.GameContent;

namespace ProgressSystem.UI.PlayerMode.ExtraUI
{
    public class UIAchSlot : UIImage
    {
        public Achievement ach;
        public Vector2 pos;
        public HashSet<UIRequireLine> preLine;
        public bool isFocus;
        public IReadOnlySet<UIAchSlot> PreAch => preLine.Select(x => x.start).ToHashSet();
        private const string Path = "ProgressSystem/Assets/Textures/UI/";
        private readonly static Texture2D isClosed = RUIHelper.T2D(Path + "IsClosed");
        private readonly static Texture2D isLocked = RUIHelper.T2D(Path + "IsLocked");
        public UIAchSlot(Achievement ach) : base(AssetLoader.Slot)
        {
            this.ach = ach;
            preLine = [];
            SetPos(ach.Position!.Value * 80);
        }
        public override void DrawSelf(SpriteBatch sb)
        {
            base.DrawSelf(sb);
            Rectangle hitbox = HitBox();
            if (ach.PreDraw?.Invoke(sb, hitbox) != false && ach.Visible)
            {
                var icon = ach.Texture.Value;
                var frame = ach.SourceRect;
                if (icon != null)
                {
                    sb.SimpleDraw(icon, hitbox.Center(), frame, (frame?.Size() ?? icon.Size()) / 2f * (frame?.Size().AutoScale() ?? 1), color: color);
                }
            }
            ach.PostDraw?.Invoke(sb, hitbox);
            Color? frameColor = null;
            var state = ach.State;
            if (state.IsCompleted())// 完成
                frameColor = Color.Orange;
            if (ach.AllRewardsReceived)// 
                frameColor = Color.GreenYellow;
            if (isFocus)// 
                frameColor = Color.DeepSkyBlue;
            if (Info.IsMouseHover)// 
                frameColor = Color.Gold;
            if (frameColor.HasValue)
                RUIHelper.DrawRec(sb, hitbox, 2f, frameColor.Value);

            bool locked = state.IsLocked(), closed = state.IsClosed(), gray = locked || closed;
            if (state.IsDisabled())
                locked = closed = true;
            if (gray)
                sb.Draw(TextureAssets.MagicPixel.Value, hitbox, Color.Black * 0.5f);
            if (closed)
                sb.SimpleDraw(isClosed, hitbox.Center(), null, isClosed.Size() / 2f);
            if (locked)
                sb.SimpleDraw(isLocked, hitbox.Center(), null, isLocked.Size() / 2f);
        }
    }
}
