using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ProgressSystem.GameEvents;
using ProgressSystem.UIEditor.ExtraUI;
using System.IO;
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
        private string EditMod;
        private bool trySave;
        /// <summary>
        /// InternalModName, GroupIndex, GE
        /// </summary>
        private Dictionary<string, Dictionary<string, HashSet<UIGESlot>>> datas;
        public override void OnInitialization()
        {
            base.OnInitialization();
            if (Main.gameMenu)
                return;
            Info.IsVisible = true;
            RemoveAll();

            datas = [];
            using Stream stream = ProgressSystem.Instance.GetFileStream("Datas.nbt");
            if (stream != null)
            {
                TagCompound mods = TagIO.FromStream(stream);
                foreach (var (name, _) in mods)
                {
                    datas[name] = [];
                    TagCompound indexs = mods.GetCompound(name);
                    foreach (var (index, _) in indexs)
                    {
                        datas[name][index] = [];
                        TagCompound ges = indexs.GetCompound(index);
                        foreach (var (ge, _) in ges)
                        {
                            TagCompound geData = ges.GetCompound(ge);
                            datas[name][index].Add(GameEvent.HandleTag(ges.GetCompound(ge)));
                        }
                    }
                }
            }

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
            eventPanel.SetPos(130, 40);
            eventPanel.SetSize(-130, 0, 1, 1, false);
            bg.Register(eventPanel);

            UIDropDownList<UIText> indexList = new(bg, eventPanel, x =>
            {
                UIText text = new(x.text);
                text.SetPos(10, 5);
                return text;
            });

            indexList.showArea.SetPos(130, 0);
            indexList.showArea.SetSize(200, 30);

            indexList.expandArea.SetPos(130, 30);
            indexList.expandArea.SetSize(200, 100);

            indexList.expandView.autoPos[0] = true;

            UIVnlPanel newProgressPanel = new(300, 110);
            newProgressPanel.SetCenter(0, 0, 0.5f, 0.5f);
            newProgressPanel.Info.IsVisible = false;
            newProgressPanel.Info.SetMargin(10);
            Register(newProgressPanel);

            UIVnlPanel inputBg = new(200, 30);
            inputBg.Info.Left.Percent = 0.5f;
            newProgressPanel.Register(inputBg);

            UIText report = new("不可为空");
            report.SetSize(report.TextSize);
            report.Info.Left.Percent = 0.5f;
            report.Info.Top.Pixel = 30;
            newProgressPanel.Register(report);

            UIInputBox indexInputer = new("输入进度组名称");
            indexInputer.SetPos(10, 5);
            indexInputer.OnInputText += evt =>
            {
                string text = indexInputer.Text;
                if (text.Any())
                {
                    if (datas.TryGetValue(EditMod, out var mod))
                    {
                        if (mod.ContainsKey(text))
                        {
                            report.ChangeText("名称重复");
                            report.color = Color.Red;
                        }
                        else
                        {
                            report.ChangeText("名称可用");
                            report.color = Color.Green;
                        }
                    }
                }
                else
                {
                    report.ChangeText("不可为空");
                    report.color = Color.Red;
                }
            };
            inputBg.Register(indexInputer);

            UIText submit = new("创建");
            submit.SetSize(submit.TextSize);
            submit.SetCenter(0, 60, 0.3f);
            submit.Events.OnLeftDown += evt =>
            {
                if (report.color == Color.Green)
                {
                    datas[EditMod][indexInputer.Text] = [];
                    SaveProgress();
                }
            };
            newProgressPanel.Register(submit);

            UIText cancel = new("取消");
            cancel.SetSize(cancel.TextSize);
            cancel.SetCenter(0, 60, 0.7f);
            newProgressPanel.Register(cancel);

            UIText newProgress = new("新建进度表");
            newProgress.SetPos(groupFilter.Width + indexList.showArea.Width + 20, 0);
            newProgress.SetSize(newProgress.TextSize + new Vector2(10, 5));
            newProgress.Events.OnLeftDown += evt =>
            {

                bg.LockInteract(false);
            };
            bg.Register(newProgress);

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

            UIContainerPanel dataView = new()
            {
                spaceY = 5,
            };
            dataView.autoPos[0] = true;
            dataView.SetSize(-30, 0, 1, 1);
            dataInput.Register(dataView);

            VerticalScrollbar dataVsroll = new(28, canDrag: false);
            dataView.SetVerticalScrollbar(dataVsroll);
            dataInput.Register(dataVsroll);

            UIDropDownList<UIText> typeSelector = new(taskPanel, dataInput, x =>
            {
                UIText text = new(x.text);
                text.SetPos(10, 5);
                return text;
            })
            {
                buttonXoffset = 10
            };

            typeSelector.showArea.SetSize(0, 30, 1);

            typeSelector.expandArea.SetPos(0, 30);
            typeSelector.expandArea.SetSize(0, 150, 1);

            typeSelector.expandView.autoPos[0] = true;

            foreach (var (label, table) in GEM._constructInfoTables)
            {
                UIText type = new(label.Split('.')[^1]);
                type.SetSize(type.TextSize);
                type.Events.OnMouseOver += evt => type.color = Color.Gold;
                type.Events.OnMouseOut += evt => type.color = Color.White;
                type.Events.OnLeftDown += evt =>
                {
                    dataView.ClearAllElements();
                    var constructs = table;
                    foreach (var constructData in constructs)
                    {
                        UIVnlPanel constructPanel = new(0, 0);
                        dataView.AddElement(constructPanel);
                        constructPanel.Info.SetMargin(10);
                        int innerY = 0;
                        foreach (var info in constructData)
                        {
                            UIText name = new(info.Name);
                            name.SetPos(0, innerY);
                            name.SetSize(name.TextSize);
                            constructPanel.Register(name);
                            innerY += 28;
                        }

                        UIText create = new("创建进度");
                        create.SetSize(create.TextSize);
                        create.SetPos(10, innerY);
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
                        constructPanel.Register(create);
                        constructPanel.SetSize(0, innerY + 28, 1);
                    }
                };
                typeSelector.AddElement(type);
            }
            typeSelector.ChangeShowElement(typeSelector.expandView.InnerUIE[0] as UIText);
        }
        public override void Update(GameTime gt)
        {
            KeyboardState state = Keyboard.GetState();
            LeftShift = state.IsKeyDown(Keys.LeftShift);
            LeftCtrl = state.IsKeyDown(Keys.LeftControl);
            bool pressS = state.IsKeyDown(Keys.S);
            if (!pressS)
                trySave = false;
            if (!trySave && LeftCtrl && pressS)
            {
                SaveProgress();
                trySave = true;
            }
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
                else
                    ge.selected = frameSelect.Add(ge);
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
                        else
                            ge.selected = frameSelect.Add(ge);
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
        private void SaveProgress()
        {
            TagCompound data = [];
            foreach (var (mods, indexs) in datas)
            {
                TagCompound mod = [];
                foreach (var (index, ges) in indexs)
                {
                    TagCompound group = [];
                    int i = 0;
                    foreach (UIGESlot slot in ges)
                    {
                        group[(i++).ToString()] = slot.ge.SaveData();
                    }
                    mod[index] = group;
                }
                data[mods] = mod;
            }
            TagIO.ToStream(data, ProgressSystem.Instance.GetFileStream("Datas.nbt", true));
        }
    }
}
