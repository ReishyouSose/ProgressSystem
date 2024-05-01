using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;

namespace ProgressSystem.UI.PlayerMode.ExtraUI
{
    public class UIRequireLine(UIAchSlot start, UIAchSlot end) : BaseUIElement
    {
        public readonly UIAchSlot start = start;
        public readonly UIAchSlot end = end;
        private readonly static Color R = Color.Red;
        private readonly static Color G = Color.Gold;
        public override void Update(GameTime gt)
        {
            base.Update(gt);
            if (start.ParentElement == null || end.ParentElement == null)
            {
                end.ach.RemovePredecessor(start.ach.FullName, true);
                Info.NeedRemove = true;
            }
        }
        public override void DrawSelf(SpriteBatch sb)
        {
            if (end.ach.PreDrawLine?.Invoke(sb, start.HitBox(), end.HitBox(), start.ach) != false && start.ach.Visible && end.ach.Visible)
            {
                Vector2 startPos = start.Center();
                Vector2 endPos = end.Center();
                Vector2 target = endPos - startPos;
                float len = target.Length();
                target = target.SafeNormalize(Vector2.Zero);
                float rot = target.ToRotation();
                for (int i = 0; i < len; i++)
                {
                    Color color = LerpColor(R, G, i / len);
                    sb.Draw(TextureAssets.MagicPixel.Value, startPos + target * i,
                        new Rectangle(0, 0, 2, 2), color, rot, Vector2.One, 2f, 0, 0);
                }
            }
            end.ach.PostDrawLine?.Invoke(sb, start.HitBox(), end.HitBox(), start.ach);
        }
        public static Color LerpColor(Color color1, Color color2, float t)
        {
            // Clamp t in the range [0, 1]
            t = Math.Clamp(t, 0f, 1f);

            // Interpolate the RGB values
            byte r = (byte)(color1.R + (color2.R - color1.R) * t);
            byte g = (byte)(color1.G + (color2.G - color1.G) * t);
            byte b = (byte)(color1.B + (color2.B - color1.B) * t);
            byte a = (byte)(color1.A + (color2.A - color1.A) * t);

            // Return the interpolated color
            return new(r, g, b, a);
        }
    }
}
