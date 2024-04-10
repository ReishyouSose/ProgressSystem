using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProgressSystem.GameEvents.Events;
using ProgressSystem.UIEditor.ExtraUI;
using RUIModule.RUIElements;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace ProgressSystem.UIEditor
{
    public class EventEditor : ContainerElement
    {
        internal static EventEditor Ins;
        public EventEditor() => Ins = this;
        private UIContainerPanel eventView;
        private bool dragging;
        public override void OnInitialization()
        {
            base.OnInitialization();
            Info.IsVisible = true;
            RemoveAll();

            UIVnlPanel bg = new(1000, 800);
            bg.SetCenter(0, 0, 0.55f, 0.5f);
            bg.Info.SetMargin(10);
            bg.canDrag = true;
            Register(bg);

            UIVnlPanel groupFilter = new(0, 0);
            groupFilter.SetSize(120, 0, 0, 1, false);
            groupFilter.Info.SetMargin(10);
            bg.Register(groupFilter);

            UIContainerPanel groupView = new();
            groupFilter.Register(groupView);

            VerticalScrollbar gv = new(100, canDrag: false);
            groupView.SetVerticalScrollbar(gv);
            groupFilter.Register(gv);

            UIVnlPanel eventPanel = new(0, 0);
            eventPanel.Info.SetMargin(10);
            eventPanel.SetPos(130, 0);
            eventPanel.SetSize(-130, 0, 1, 1, false);
            bg.Register(eventPanel);

            eventView = new();
            eventView.SetSize(-20, -20, 1, 1);
            eventPanel.Register(eventView);

            VerticalScrollbar ev = new(80);
            ev.Info.Left.Pixel += 10;
            eventView.SetVerticalScrollbar(ev);
            eventPanel.Register(ev);

            HorizontalScrollbar eh = new(80)
            {
                useScrollWheel = false
            };
            eh.Info.Top.Pixel += 10;
            eventView.SetHorizontalScrollbar(eh);
            eventPanel.Register(eh);

            Texture2D line = TextureAssets.MagicPixel.Value;
            Color color = Color.White;
            for (int i = 0; i < 50; i++)
            {
                UIImage hline = new(line, 5000, 2, color: color);
                hline.SetPos(0, i * 80);
                eventView.AddElement(hline);

                UIImage vline = new(line, 2, 5000, color: color);
                vline.SetPos(80 * i, 0);
                eventView.AddElement(vline);
            }

            UIVnlPanel taskPanel = new(200, 100);
            taskPanel.SetCenter(100, 0, 0, 0.5f);
            taskPanel.Info.SetMargin(10);
            Register(taskPanel);

            UIItemSlot itemSlot = new();
            itemSlot.Events.OnLeftDown += evt =>
            {
                if (itemSlot.ContainedItem.type <= 0)
                {
                    if (Main.mouseItem.type > ItemID.None)
                    {
                        itemSlot.ContainedItem = Main.mouseItem.Clone();
                    }
                }
                else
                {
                    itemSlot.ContainedItem.SetDefaults();
                }
            };
            taskPanel.Register(itemSlot);

            UIText create = new("创建制造物品任务");
            create.SetSize(create.TextSize);
            create.SetPos(0, 62);
            create.Events.OnMouseOver += evt => create.color = Color.Gold;
            create.Events.OnMouseOut += evt => create.color = Color.White;
            create.Events.OnLeftDown += evt =>
            {
                int id = itemSlot.ContainedItem?.type ?? -1;
                if (id <= 0) return;
                CraftItem task = new(id);
                Main.instance.LoadItem(id);
                UIGESlot ge = new(task, TextureAssets.Item[id].Value);
                ge.Events.OnMouseOver += evt =>
                {
                    ev.canDrag = false;
                    eh.canDrag = false;
                };
                ge.Events.OnMouseOut += evt =>
                {
                    ev.canDrag = true;
                    eh.canDrag = true;
                };
                ge.Events.OnLeftDown += evt => dragging = true;
                ge.Events.OnLeftUp += evt => dragging = false;
                eventView.AddElement(ge);
            };
            taskPanel.Register(create);
        }
        public override void Update(GameTime gt)
        {
            base.Update(gt);
            if (dragging)
            {
                Point target = Main.MouseScreen.ToPoint();
                var eh = eventView.Hscroll;
                var ev = eventView.Vscroll;
                if (target.X > eventView.Right && eh.Real < 1)
                {
                    eh.MoveView(target.X - eventView.Right, 15);
                }
                else if (target.X < eventView.Left && eh.Real > 0)
                {
                    eh.MoveView(target.X - eventView.Left, 40);
                }
                if (target.Y > eventView.Bottom && ev.Real < 1)
                {
                    ev.MoveView(target.Y - eventView.Bottom, 15);
                }
                else if (target.Y < eventView.Top && ev.Real > 0)
                {
                    ev.MoveView(target.Y - eventView.Top, 40);
                }
            }
        }

    }
}
