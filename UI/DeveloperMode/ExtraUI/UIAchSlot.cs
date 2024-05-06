using Microsoft.Xna.Framework.Graphics;
using RUIModule;
using Terraria.GameContent;
using Terraria.UI.Chat;

namespace ProgressSystem.UI.DeveloperMode.ExtraUI
{
    public class UIAchSlot : UIImage
    {
        public Achievement ach;
        private bool dragging;
        private Vector2 oldlocal;
        private readonly Texture2DGetter Icon;

        /// <summary>
        /// 吸附位置
        /// </summary>
        private Vector2? adsorption;
        public Vector2 pos;

        /// <summary>
        /// 被框选中
        /// </summary>
        public bool selected;

        /// <summary>
        /// 正在作为前置选中
        /// </summary>
        public bool preSetting;
        public HashSet<UIRequireLine> preLine;
        public IReadOnlySet<UIAchSlot> PreAch => preLine.Select(x => x.start).ToHashSet();
        public UIAchSlot(Achievement ach, Vector2? pos = null) : base(AssetLoader.Slot)
        {
            this.ach = ach;
            Icon = ach.Texture;
            if (pos != null)
            {
                this.pos = pos.Value;
                SetPos(this.pos * 80);
            }
            preLine = [];
        }
        public override void OnInitialization()
        {
            base.OnInitialization();

        }
        public override void LoadEvents()
        {
            Events.OnLeftDown += evt =>
            {
                if (!dragging)
                {
                    dragging = true;
                }

                oldlocal = Main.MouseScreen;
                color = Color.White * 0.75f;
            };
            Events.OnLeftUp += evt =>
            {
                dragging = false;
                if (adsorption != null)
                {
                    SetPos(pos * 80);
                    adsorption = null;
                }
                color = Color.White;
            };
            Events.OnLeftDoubleClick += evt => dragging = false;
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
                    if (!AchEditor.AchEditor.AchPos.Contains(p))
                    {
                        adsorption = new(x, y);
                        pos = adsorption.Value;
                    }
                    SetCenter(mouse - origin, false);
                    if (Info.Left.Pixel < 0)
                    {
                        Info.Left.Pixel = 0;
                    }

                    if (Info.Top.Pixel < 0)
                    {
                        Info.Top.Pixel = 0;
                    }

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
            if (preSetting)
            {
                RUIHelper.DrawRec(sb, HitBox(), 2, Color.Gold, false);
            }
            else if (selected)
            {
                RUIHelper.DrawRec(sb, HitBox(), 2, Color.Red, false);
            }
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
            if (adsorption != null)
            {
                sb.SimpleDraw(Tex, adsorption.Value * 80 + ParentElement.HitBox(false).TopLeft(), null, Vector2.Zero, color: Color.White * 0.5f);
            }
            ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.MouseText.Value, pos.ToString(),
                hitbox.TopLeft() + new Vector2(0, 55), Color.White, 0, Vector2.Zero, Vector2.One, -1, 1.5f);
        }
    }
}
