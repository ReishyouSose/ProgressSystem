using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ProgressSystem.GameEvents;
using ProgressSystem.UIEditor.ExtraUI;
using RUIModule;
using System.IO;
using Terraria.GameContent;

namespace ProgressSystem.UIEditor
{
    public class GEEditor : ContainerElement
    {
        internal static GEEditor Ins;
        /// <summary>
        /// 当前进度组GESlot位置
        /// </summary>
        internal static HashSet<Vector2> GEPos;
        public GEEditor() => Ins = this;
        private UIContainerPanel eventView;
        private bool dragging;
        private bool draggingSelected;
        private Vector2 selectedStart;
        /// <summary>
        /// 已经被选中的GESlot
        /// </summary>
        private HashSet<UIGESlot> frameSelect;
        /// <summary>
        /// 临时被选中的GESlot
        /// </summary>
        private HashSet<UIGESlot> tempSelect;
        /// <summary>
        /// 本次碰撞判定已经交互过的GESlot
        /// </summary>
        private HashSet<UIGESlot> interacted;
        /// <summary>
        /// 用于判定包含的GE鼠标碰撞箱
        /// </summary>
        private static UIGECollision collision;
        private static bool LeftShift;
        private static bool LeftCtrl;
        /// <summary>
        /// 正在编辑的modName
        /// </summary>
        private string EditMod;
        /// <summary>
        /// 正在编辑的进度组名
        /// </summary>
        private string EditPage;
        /// <summary>
        /// 检测是否已按下ctrl + S
        /// </summary>
        private bool trySave;
        /// <summary>
        /// InternalModName, GroupIndex, GE
        /// </summary>
        private Dictionary<string, Dictionary<string, HashSet<UIGESlot>>> datas;
        public override void OnInitialization()
        {
            base.OnInitialization();
            datas ??= [];
            if (datas.Count == 0)
            {
                LoadProgress();
            }
            if (Main.gameMenu)
                return;
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
            eventPanel.SetPos(130, 40);
            eventPanel.SetSize(-130, -40, 1, 1, false);
            bg.Register(eventPanel);

            UIDropDownList<UIText> indexList = new(bg, eventPanel, x =>
            {
                UIText text = new(x.text);
                text.SetPos(10, 5);
                return text;
            })
            { buttonXoffset = 10 };

            indexList.showArea.SetPos(130, 0);
            indexList.showArea.SetSize(200, 30);

            indexList.expandArea.SetPos(130, 30);
            indexList.expandArea.SetSize(200, 100);

            indexList.expandView.autoPos[0] = true;

            bool first = false;
            foreach (Mod mod in ModLoader.Mods)
            {
                string modName = mod.Name;
                UIImage modSlot = new(RUIHelper.T2D(modName + "/icon"));
                modSlot.SetSize(100, 100);
                modSlot.Events.OnLeftDown += evt =>
                {
                    indexList.RemoveAll();
                    EditMod = modName;
                    if (datas.TryGetValue(modName, out var modPages))
                    {
                        UIText firstText = null;
                        foreach (var (page, ges) in modPages)
                        {
                            UIText pageName = new(page);
                            pageName.SetSize(pageName.TextSize);
                            pageName.Events.OnMouseOver += evt => pageName.color = Color.Gold;
                            pageName.Events.OnMouseOut += evt => pageName.color = Color.White;
                            pageName.Events.OnLeftDown += evt =>
                            {
                                EditPage = pageName.text;
                                eventView.InnerUIE.RemoveAll(MatchGESlot);
                                GEPos.Clear();
                                foreach (UIGESlot ge in datas[EditMod][EditPage])
                                {
                                    GEPos.Add(ge.pos);
                                    eventView.Register(ge);
                                }
                            };
                            indexList.AddElement(pageName);
                            firstText ??= pageName;
                        }
                        indexList.ChangeShowElement(firstText);
                        firstText.Events.LeftDown(firstText);
                    }
                };
                groupView.AddElement(modSlot);
                if (first)
                {
                    modSlot.Events.LeftDown(modSlot);
                    var firstIndex = indexList.expandView.InnerUIE[0];
                    firstIndex.Events.LeftDown(firstIndex);
                }
            }

            UIVnlPanel newProgressPanel = new(300, 200, opacity: 1);
            newProgressPanel.SetCenter(0, 0, 0.5f, 0.5f);
            newProgressPanel.Info.IsVisible = false;
            newProgressPanel.Info.SetMargin(10);
            Register(newProgressPanel);

            UIVnlPanel inputBg = new(200, 30, color: Color.White);
            inputBg.SetCenter(0, 0, 0.5f, 0.25f);
            newProgressPanel.Register(inputBg);

            UIText report = new("不可为空")
            {
                color = Color.Red
            };
            report.SetSize(report.TextSize);
            report.SetCenter(0, 0, 0.5f, 0.5f);
            newProgressPanel.Register(report);

            UIInputBox indexInputer = new("输入进度组名称");
            indexInputer.SetSize(-40, 0, 1, 1);
            indexInputer.OnInputText += text =>
            {
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
            submit.SetCenter(0, 0, 0.3f, 0.75f);
            submit.Events.OnLeftDown += evt =>
            {
                if (report.color == Color.Green)
                {
                    datas[EditMod] ??= [];
                    EditPage = indexInputer.Text;
                    datas[EditMod][EditPage] = [];
                    bg.LockInteract(true);
                    newProgressPanel.Info.IsVisible = false;
                    SaveProgress();
                }
            };
            newProgressPanel.Register(submit);

            UIText cancel = new("取消");
            cancel.SetSize(cancel.TextSize);
            cancel.SetCenter(0, 0, 0.7f, 0.75f);
            cancel.Events.OnLeftDown += evt =>
            {
                bg.LockInteract(true);
                newProgressPanel.Info.IsVisible = false;
            };
            newProgressPanel.Register(cancel);

            UIText newProgress = new("新建进度表");
            newProgress.SetPos(groupFilter.Width + indexList.showArea.Width + 20, 5);
            newProgress.SetSize(newProgress.TextSize + new Vector2(10, 5));
            newProgress.Events.OnLeftDown += evt =>
            {
                indexInputer.ClearText();
                newProgressPanel.Info.IsVisible = true;
                bg.LockInteract(false);
            };
            newProgress.Events.OnMouseOver += evt => newProgress.color = Color.Gold;
            newProgress.Events.OnMouseOut += evt => newProgress.color = Color.White;
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

            UIVnlPanel taskPanel = new(300, 300) { canDrag = true };
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
            { buttonXoffset = 10 };

            typeSelector.showArea.SetSize(0, 30, 1);

            typeSelector.expandArea.SetPos(0, 30);
            typeSelector.expandArea.SetSize(0, 150, 1);

            typeSelector.expandView.autoPos[0] = true;

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
                        dataView.ClearAllElements();
                        foreach (var constructData in constructs)
                        {
                            UIVnlPanel constructPanel = new(0, 0);
                            constructPanel.Info.SetMargin(10);
                            dataView.AddElement(constructPanel);
                            int innerY = 0;
                            foreach (var info in constructData)
                            {
                                UIText name = new(info.Name ?? "Anonymous");
                                name.SetPos(0, innerY);
                                name.SetSize(name.TextSize);
                                constructPanel.Register(name);

                                UIText legal = new(info.Important ? "可以为空" : "不可为空");
                                legal.SetPos(name.TextSize.X + 10, innerY);
                                constructPanel.Register(legal);
                                innerY += 28;

                                UIVnlPanel valueInputBg = new(0, 28, color: Color.White);
                                valueInputBg.Info.Width.Percent = 1;
                                valueInputBg.SetPos(0, innerY);
                                constructPanel.Register(valueInputBg);

                                UIInputBox valueInputer = new("类型为" + info.Type.Name, color: Color.Black);
                                valueInputer.SetSize(-40, 0, 1, 1);
                                valueInputer.OnInputText += text =>
                                {
                                    if (text.Any())
                                    {
                                        info.SetValue(text);
                                        legal.ChangeText(info.IsMet ? ("合法值：" + info.GetValue()) : "不合法");
                                    }
                                    else legal.ChangeText(info.Important ? "可以为空" : "不可为空");
                                };
                                valueInputBg.Register(valueInputer);

                                UIClose clear = new();
                                clear.SetCenter(-10, 0, 1, 0.5f);
                                clear.Events.OnLeftDown += evt => valueInputer.ClearText();
                                valueInputBg.Register(clear);
                                innerY += 28;
                            }

                            UIText create = new("创建进度");
                            create.SetSize(create.TextSize);
                            create.SetPos(0, innerY);
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
                            constructPanel.SetSize(0, innerY + 48, 1);
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
            string root = Path.Combine(Main.SavePath, "Mods", ProgressSystem.Instance.Name);
            Directory.CreateDirectory(root);
            foreach ((string modName, Dictionary<string, HashSet<UIGESlot>> pages) in datas)
            {
                Directory.CreateDirectory(Path.Combine(root, modName));
                foreach ((string pageName, HashSet<UIGESlot> slots) in pages)
                {
                    using FileStream stream = File.OpenWrite(Path.Combine(root, modName, pageName + ".dat"));
                    TagCompound tag = [];
                    List<TagCompound> subTags = [];
                    foreach (var slot in slots)
                    {
                        if (slot.ge is null)
                        {
                            continue;
                        }
                        var subtag = GEManager.Save(slot.ge);
                        if (subtag != null)
                        {
                            Vector2 pos = slot.pos;
                            subtag["posX"] = pos.X;
                            subtag["posY"] = pos.Y;
                            subTags.Add(subtag);
                        }
                    }
                    tag["data"] = subTags;
                    TagIO.ToStream(tag, stream);
                }
            }
        }
        private void LoadProgress()
        {
            string root = Path.Combine(Main.SavePath, "Mods", ProgressSystem.Instance.Name);
            if (!Directory.Exists(root))
            {
                return;
            }
            string[] modDirs = Directory.GetDirectories(root);
            foreach (var modDir in modDirs)
            {
                string modName = modDir.Split(Path.PathSeparator)[^1];
                datas[modName] = [];
                string[] pageFiles = Directory.GetFiles(modDir);
                foreach (var pageFile in pageFiles)
                {
                    string pageName = Path.GetFileNameWithoutExtension(pageFile);
                    datas[modName][pageName] = [];
                    try
                    {
                        var tag = TagIO.FromStream(File.OpenRead(pageFile));
                        tag.TryGet("data", out List<TagCompound> tags);
                        foreach (var data in tags)
                        {
                            var ge = GEManager.Load(data);
                            if (ge != null)
                            {
                                Vector2 pos = new(data.GetFloat("posX"), data.GetFloat("posY"));
                                datas[modName][pageName].Add(new UIGESlot(ge, pos));
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }
        private static bool MatchGESlot(BaseUIElement uie) => uie is UIGESlot;
    }
}
