using Microsoft.Xna.Framework.Graphics;
using RUIModule;

namespace ProgressSystem.UI.PlayerMode.ExtraUI
{
    public class UIAchSlot : UIImage
    {
        public Achievement ach;
        private readonly Texture2DGetter Icon;
        public Vector2 pos;
        public HashSet<UIRequireLine> preLine;
        public IReadOnlySet<UIAchSlot> PreAch => preLine.Select(x => x.start).ToHashSet();
        public UIAchSlot(Achievement ach) : base(AssetLoader.Slot)
        {
            this.ach = ach;
            Icon = ach.Texture;
            preLine = [];
            SetPos(ach.Position!.Value * 80);
        }
        public override void DrawSelf(SpriteBatch sb)
        {
            base.DrawSelf(sb);
            Rectangle hitbox = HitBox();
            if (ach.PreDraw?.Invoke(sb, hitbox) != false)
            {
                var icon = Icon.Value;
                var frame = ach.SourceRect;
                if (icon != null)
                {
                    sb.SimpleDraw(icon, hitbox.Center(), frame, (frame?.Size() ?? icon.Size()) / 2f * (frame?.Size().AutoScale() ?? 1), color: color);
                }
            }
            ach.PostDraw?.Invoke(sb, hitbox);
        }
    }
}
