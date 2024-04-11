using Microsoft.Xna.Framework.Graphics;
using ProgressSystem.GameEvents;
using RUIModule;
using Terraria.GameContent;
using Terraria.UI.Chat;

namespace ProgressSystem.UIEditor.ExtraUI
{
    public class UIGESlot : UIImage
    {
        public GameEvent ge;
        private bool dragging;
        private Vector2 oldlocal;
        public Texture2D Icon;
        /// <summary>
        /// 吸附位置
        /// </summary>
        private Vector2? adsorption;
        public Vector2 pos;
        public bool selected;
        public UIGESlot(GameEvent ge = null, Texture2D tex = null) : base(AssetLoader.Slot)
        {
            this.ge = ge;
            Icon = tex;
        }
        public override void OnInitialization()
        {
            base.OnInitialization();

        }
        public override void LoadEvents()
        {
            Events.OnLeftDown += evt =>
            {
                if (!dragging) dragging = true;
                oldlocal = Main.MouseScreen;
                GEEditor.GEPos.Remove(pos);
                color = Color.White * 0.75f;
            };
            Events.OnLeftUp += evt =>
            {
                dragging = false;
                if (adsorption != null)
                {
                    SetPos(adsorption.Value * 80);
                    GEEditor.GEPos.Add(pos);
                    adsorption = null;
                }
                color = Color.White;
            };
            Events.OnLeftDoubleClick += evt => dragging = false;
            Events.OnRightDoubleClick += evt =>
            {
                ParentElement.Remove(this);
                GEEditor.GEPos.Remove(pos);
            };
        }

        public override void Update(GameTime gt)
        {
            base.Update(gt);
            if (dragging && !selected)
            {
                Vector2 mouse = Main.MouseScreen;
                if (oldlocal != mouse)
                {
                    Vector2 origin = ParentElement.HitBox(false).TopLeft();
                    int x = (int)(mouse.X - origin.X) / 80;
                    int y = (int)(mouse.Y - origin.Y) / 80;
                    x = Math.Max(x, 0);
                    y = Math.Max(y, 0);
                    Vector2 p = new(x, y);
                    if (!GEEditor.GEPos.Contains(p))
                    {
                        adsorption = new(x, y);
                        pos = adsorption.Value;
                    }
                    SetCenter(mouse - origin, false);
                    if (Info.Left.Pixel < 0) Info.Left.Pixel = 0;
                    if (Info.Top.Pixel < 0) Info.Top.Pixel = 0;
                    Calculation();
                }
                oldlocal = mouse;
            }
        }
        public override void Calculation()
        {
            base.Calculation();
        }
        public override void DrawSelf(SpriteBatch sb)
        {
            base.DrawSelf(sb);
            if (selected)
            {
                RUIHelper.DrawRec(sb, HitBox(), 2, Color.Red, false);
            }
            Rectangle hitbox = HitBox();
            if (Icon != null)
            {
                sb.SimpleDraw(Icon, hitbox.Center(), null, Icon.Size() / 2f, color: color);
            }
            if (adsorption != null)
            {
                sb.SimpleDraw(Tex, adsorption.Value * 80 + ParentElement.HitBox(false).TopLeft(), null, Vector2.Zero, color: Color.White * 0.5f);
            }
            ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.MouseText.Value, pos.ToString(),
                hitbox.TopLeft() + new Vector2(0, 55), Color.White, 0, Vector2.Zero, Vector2.One, -1, 1.5f);
        }
    }
}
