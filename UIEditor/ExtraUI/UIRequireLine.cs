﻿using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;

namespace ProgressSystem.UIEditor.ExtraUI
{
    public class UIRequireLine(UIGESlot start, UIGESlot end) : BaseUIElement
    {
        public readonly UIGESlot start = start;
        public readonly UIGESlot end = end;
        private readonly static Color pink = Color.LightPink;
        private readonly static Color blue = Color.LightBlue;
        public override void DrawSelf(SpriteBatch sb)
        {
            Vector2 startPos = start.Center();
            Vector2 endPos = end.Center();
            Vector2 target = endPos - startPos;
            float len = target.Length();
            target.SafeNormalize(Vector2.Zero);
            float rot = target.ToRotation();
            for (int i = 0; i < len; i++)
            {
                Color color = LerpColor(pink, blue, i / len);
                sb.Draw(TextureAssets.MagicPixel.Value, startPos + target * i,
                    new Rectangle(0, 0, 2, 2), color, rot, Vector2.One, 1f, 0, 0);
            }
        }
        public static Color LerpColor(Color color1, Color color2, float t)
        {
            // Clamp t in the range [0, 1]
            t = Math.Max(0, Math.Min(1, t));

            // Interpolate the RGB values
            int r = (int)(color1.R + (color2.R - color1.R) * t);
            int g = (int)(color1.G + (color2.G - color1.G) * t);
            int b = (int)(color1.B + (color2.B - color1.B) * t);
            int a = (int)(color1.A + (color2.A - color1.A) * t);

            // Return the interpolated color
            return new(a, r, g, b);
        }
    }
}
