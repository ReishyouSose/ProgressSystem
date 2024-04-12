﻿using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ProgressSystem.GameEvents;
using ProgressSystem.GameEvents.Events;
using ProgressSystem.UIEditor.ExtraUI;
using System.Threading.Tasks;
using Terraria.GameContent;

namespace ProgressSystem.UIEditor
{
    public class GEEditor : ContainerElement
    {
        internal static GEEditor Ins;
        internal static HashSet<Vector2> GEPos;
        public GEEditor() => Ins = this;
        private UIContainerPanel eventView;
        private bool dragging;
        private bool draggingSelected;
        private Vector2 selectedStart;
        private HashSet<UIGESlot> frameSelect;
        private HashSet<UIGESlot> tempSelect;
        private HashSet<UIGESlot> interacted;
        private static UIGECollision collision;
        private static bool LeftShift;
        private static bool LeftCtrl;
        public override void OnInitialization()
        {
            base.OnInitialization();
            Info.IsVisible = true;
            RemoveAll();

            GEPos = [];
            tempSelect = [];
            frameSelect = [];
            interacted = [];

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
            eventView.Events.OnRightDown += evt =>
            {
                collision = new();
                eventView.AddElement(collision);
                if (!Keyboard.GetState().IsKeyDown(Keys.LeftShift))
                {
                    foreach (UIGESlot ge in frameSelect)
                    {
                        ge.selected = false;
                    }
                    frameSelect.Clear();
                }
            };
            eventView.Events.OnRightUp += evt =>
            {
                eventView.RemoveElement(collision);
                foreach (UIGESlot ge in tempSelect)
                {
                    frameSelect.Add(ge);
                }
                collision = null;
                tempSelect.Clear();
                interacted.Clear();
            };
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

            UIVnlPanel taskPanel = new(300, 300);
            taskPanel.canDrag = true;
            taskPanel.SetCenter(150, 0, 0, 0.5f);
            taskPanel.Info.SetMargin(10);
            Register(taskPanel);

            /*UIItemSlot itemSlot = new();
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
            taskPanel.Register(itemSlot);*/

            UIVnlPanel dataInput = new(0, 0);
            dataInput.SetPos(0, 30);
            dataInput.SetSize(0, -30, 1, 1);
            taskPanel.Register(dataInput);

            UIDropDownList<UIText> typeSelector = new(taskPanel, dataInput, x =>
            {
                UIText text = new(x.text);
                text.SetPos(10, 5);
                return text;
            });
            typeSelector.SetSize(0, 30, 1);
            typeSelector.buttonXoffset = 10;

            typeSelector.showArea.SetSize(0, 30, 1);

            typeSelector.expandArea.SetPos(0, 30);
            typeSelector.expandArea.SetSize(0, 150, 1);

            typeSelector.expandView.autoPos[0] = true;
            taskPanel.Register(typeSelector);

            foreach (var ge in ModContent.GetContent<GameEvent>())
            {
                var tables = ge.GetConstructInfoTables();
                foreach (var t in tables)
                {
                    string label = t.Name;
                    UIText type = new(label is null ? "Anonymous" : label.Split('.')[^1]);
                    type.SetSize(type.TextSize);
                    type.Events.OnMouseOver += evt => type.color = Color.Gold;
                    type.Events.OnMouseOut += evt => type.color = Color.White;
                    type.Events.OnLeftDown += evt =>
                    {
                        var constructs = tables;
                        int y = 0;
                        foreach (var constructData in constructs)
                        {
                            UIVnlPanel constructPanel = new(0, 0);
                            dataInput.Register(constructPanel);
                            constructPanel.Info.SetMargin(10);
                            int innerY = 0;
                            foreach (var info in constructData)
                            {
                                UIText name = new(info.Name ?? "Anonymous");
                                name.SetPos(0, innerY);
                                name.SetSize(name.TextSize);
                                constructPanel.Register(name);
                            }
                            constructPanel.SetSize(0, y, 1);
                            y += constructPanel.Height + 10;

                            UIText create = new("创建进度");
                            create.SetSize(create.TextSize);
                            create.SetPos(10, y);
                            create.Events.OnMouseOver += evt => create.color = Color.Gold;
                            create.Events.OnMouseOut += evt => create.color = Color.White;
                            create.Events.OnLeftDown += evt =>
                            {
                                if (constructData.TryCreate(out GameEvent task))
                                {
                                    UIGESlot ge = new(task);
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
                                    ge.Events.OnLeftDown += GESlotDragCheck;
                                    ge.Events.OnLeftUp += evt =>
                                    {
                                        dragging = false;
                                        draggingSelected = false;
                                    };
                                    ge.Events.OnUpdate += GESlotUpdate;
                                    {
                                    };
                                    Vector2 pos = Vector2.Zero;
                                    while (GEPos.Contains(pos))
                                    {
                                        pos.X++;
                                    }
                                    ge.pos = pos;
                                    ge.SetPos(pos * 80);
                                    GEPos.Add(pos);
                                    eventView.AddElement(ge);
                                }
                            };
                            dataInput.Register(create);
                        }
                    };
                    typeSelector.AddElement(type);
                }
            }
            typeSelector.ChangeShowElement(typeSelector.expandView.InnerUIE[0] as UIText);

            
        }
        public override void Update(GameTime gt)
        {
            KeyboardState state = Keyboard.GetState();
            LeftShift = state.IsKeyDown(Keys.LeftShift);
            LeftCtrl = state.IsKeyDown(Keys.LeftControl);
            if (dragging || collision != null)
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
            base.Update(gt);
            if (draggingSelected)
            {
                Vector2 mouse = Main.MouseScreen;
                Vector2 origin = eventView.ChildrenElements[0].HitBox(false).TopLeft();
                int x = (int)(mouse.X - origin.X) / 80;
                int y = (int)(mouse.Y - origin.Y) / 80;
                x = Math.Max(x, 0);
                y = Math.Max(y, 0);
                Vector2 p = new(x, y);
                Vector2 offset = p - selectedStart;
                if (offset != Vector2.Zero)
                {
                    bool allMet = true;
                    foreach (UIGESlot ge in frameSelect)
                    {
                        Vector2 newPos = ge.pos + offset;
                        if (newPos.X < 0)
                        {
                            allMet = false;
                            break;
                        }
                        if (newPos.Y < 0)
                        {
                            allMet = false;
                            break;
                        }
                        if (GEPos.Contains(ge.pos + offset))
                        {
                            allMet = false;
                            break;
                        }
                    }
                    if (allMet)
                    {
                        foreach (UIGESlot ge in frameSelect)
                        {
                            GEPos.Remove(ge.pos);
                            ge.pos += offset;
                            GEPos.Add(ge.pos);
                            ge.SetPos(ge.pos * 80);
                        }
                        selectedStart = p;
                    }
                }
            }
        }
        private void GESlotDragCheck(BaseUIElement uie)
        {
            UIGESlot ge = uie as UIGESlot;
            if (LeftCtrl)
            {
                if (frameSelect.Contains(ge))
                {
                    frameSelect.Remove(ge);
                    ge.selected = false;
                }
                else ge.selected = frameSelect.Add(ge);
            }
            else if (frameSelect.Any())
            {
                draggingSelected = true;
                Point mouse = (Main.MouseScreen - eventView.ChildrenElements[0]
                .HitBox(false).TopLeft()).ToPoint();
                selectedStart = new(mouse.X / 80, mouse.Y / 80);
            }
            dragging = true;
        }
        private void GESlotUpdate(BaseUIElement uie)
        {
            UIGESlot ge = uie as UIGESlot;
            if (collision != null)
            {
                bool intersects = ge.HitBox().Intersects(collision.selector);
                if (LeftShift)
                {
                    if (!interacted.Contains(ge) && intersects)
                    {
                        if (frameSelect.Contains(ge))
                        {
                            frameSelect.Remove(ge);
                            ge.selected = false;
                        }
                        else ge.selected = frameSelect.Add(ge);
                        interacted.Add(ge);
                    }
                }
                else
                {
                    if (intersects)
                    {
                        tempSelect.Add(ge);
                        ge.selected = true;
                    }
                    else
                    {
                        tempSelect.Remove(ge);
                        ge.selected = false;
                    }
                }
            }
        }
    }
}
