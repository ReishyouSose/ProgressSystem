using Microsoft.Xna.Framework.Graphics;
using RUIModule;
using Terraria.GameContent;

namespace ProgressSystem.UIEditor.ExtraUI;

public class UIAchCollision : BaseUIElement
{
    public Rectangle selector;
    public override void OnInitialization()
    {
        base.OnInitialization();
        SetPos(Main.MouseScreen - ParentElement.HitBox(false).TopLeft());
    }
    public override void Update(GameTime gt)
    {
        base.Update(gt);
        Point origin = new(ParentElement.InnerLeft, ParentElement.InnerTop);
        Point mouse = Main.MouseScreen.ToPoint() - origin;
        int l = (int)Info.Left.Pixel;
        int t = (int)Info.Top.Pixel;
        int r = mouse.X;
        int b = mouse.Y;
        if (l > r) (l, r) = (r, l);
        if (t > b) (t, b) = (b, t);
        selector = new(l + origin.X, t + origin.Y, r - l, b - t);
    }
    public override void DrawSelf(SpriteBatch sb)
    {
        RUIHelper.DrawRec(sb, selector, 2, Color.White, false);
        sb.Draw(TextureAssets.MagicPixel.Value, selector, Color.White * 0.25f);
    }
}
