using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ProgressSystem.GameEvents;
using ProgressSystem.UIEditor.ExtraUI;
using RUIModule;
using System.IO;
using System.Text;
using Terraria.GameContent;
using Terraria.UI.Chat;

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
        private Dictionary<string, Dictionary<string, Dictionary<GameEvent, Vector2>>> datas;
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

            EditMod = "";
            EditPage = "";
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
            groupFilter.SetSize(100, 0, 0, 1);
            groupFilter.Info.SetMargin(10);
            bg.Register(groupFilter);

            UIContainerPanel groupView = new();
            groupView.SetSize(0, 0, 1, 1);
            groupView.autoPos[0] = true;
            groupView.spaceY = 10;
            groupFilter.Register(groupView);

            VerticalScrollbar gv = new(100, canDrag: false);
            groupView.SetVerticalScrollbar(gv);
            groupFilter.Register(gv);

            UIVnlPanel eventPanel = new(0, 0);
            eventPanel.Info.SetMargin(10);
            eventPanel.SetPos(110, 40);
            eventPanel.SetSize(-110, -40, 1, 1, false);
            bg.Register(eventPanel);

            UIDropDownList<UIText> indexList = new(bg, eventPanel, x =>
            {
                UIText text = new(x.text);
                text.SetPos(10, 5);
                return text;
            })
            { buttonXoffset = 10 };

            indexList.showArea.SetPos(110, 0);
            indexList.showArea.SetSize(200, 30);

            indexList.expandArea.SetPos(110, 30);
            indexList.expandArea.SetSize(200, 100);

            indexList.expandView.autoPos[0] = true;

            bool first = false;
            foreach (Mod mod in ModLoader.Mods)
            {
                if (!mod.HasAsset("icon")) continue;
                string modName = mod.Name;
                UIImage modSlot = new(RUIHelper.T2D(modName + "/icon")) { hoverText = mod.DisplayName };
                modSlot.DrawRec[2] = Color.Red;
                modSlot.Events.OnLeftDown += evt =>
                {
                    indexList.ClearAllElements();
                    EditMod = modName;
                    ClearTemp();
                    if (datas.TryGetValue(modName, out var modPages))
                    {
                        bool firstPage = false;
                        foreach (var (page, ges) in modPages)
                        {
                            UIText pageName = new(page);
                            pageName.SetSize(pageName.TextSize);
                            pageName.Events.OnMouseOver += evt => pageName.color = Color.Gold;
                            pageName.Events.OnMouseOut += evt => pageName.color = Color.White;
                            pageName.Events.OnLeftDown += evt => LoadGEs(pageName.text);
                            indexList.AddElement(pageName);
                            if (!firstPage)
                            {
                                indexList.ChangeShowElement(pageName);
                                EditPage = page;
                                firstPage = true;
                            }
                        }
                    }
                };
                groupView.AddElement(modSlot);
                if (!first)
                {
                    modSlot.Events.LeftDown(modSlot);
                    var innerUIE = indexList.expandView.InnerUIE;
                    if (innerUIE.Any())
                    {
                        var firstIndex = innerUIE[0];
                        firstIndex.Events.LeftDown(firstIndex);
                        first = true;
                    }
                }
            }

            UIVnlPanel newProgressPanel = new(300, 200, opacity: 1);
            newProgressPanel.SetCenter(0, 0, 0.5f, 0.5f);
            newProgressPanel.Info.IsVisible = false;
            newProgressPanel.Info.SetMargin(10);
            Register(newProgressPanel);

            UIVnlPanel inputBg = new(200, 30);
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
                    else
                    {
                        report.ChangeText("名称可用");
                        report.color = Color.Green;
                    }
                }
                else
                {
                    report.ChangeText("不可为空");
                    report.color = Color.Red;
                }
            };
            inputBg.Register(indexInputer);

            UIClose clearText = new();
            clearText.Events.OnLeftDown += evt => indexInputer.ClearText();
            clearText.SetCenter(-10, 0, 1, 0.5f);
            inputBg.Register(clearText);

            UIText submit = new("创建");
            submit.SetSize(submit.TextSize);
            submit.SetCenter(0, 0, 0.3f, 0.75f);
            submit.Events.OnMouseOver += evt => submit.color = Color.Gold;
            submit.Events.OnMouseOut += evt => submit.color = Color.White;
            submit.Events.OnLeftDown += evt =>
            {
                if (report.color == Color.Green)
                {
                    if (!datas.ContainsKey(EditMod)) datas[EditMod] = [];
                    EditPage = indexInputer.Text;
                    datas[EditMod][EditPage] = [];
                    bg.LockInteract(true);
                    newProgressPanel.Info.IsVisible = false;
                    UIText pageName = new(EditPage);
                    pageName.SetSize(pageName.TextSize);
                    pageName.Events.OnMouseOver += evt => pageName.color = Color.Gold;
                    pageName.Events.OnMouseOut += evt => pageName.color = Color.White;
                    pageName.Events.OnLeftDown += evt => LoadGEs(pageName.text);
                    indexList.AddElement(pageName);
                    indexList.ChangeShowElement(pageName);
                    ClearTemp();
                    SaveProgress();
                }
            };
            newProgressPanel.Register(submit);

            UIText cancel = new("取消");
            cancel.SetSize(cancel.TextSize);
            cancel.SetCenter(0, 0, 0.7f, 0.75f);
            cancel.Events.OnMouseOver += evt => cancel.color = Color.Gold;
            cancel.Events.OnMouseOut += evt => cancel.color = Color.White;
            cancel.Events.OnLeftDown += evt =>
            {
                bg.LockInteract(true);
                newProgressPanel.Info.IsVisible = false;
            };
            newProgressPanel.Register(cancel);

            UIText newProgress = new("新建进度表");
            newProgress.SetPos(groupFilter.Width + indexList.showArea.Width + 20, 5);
            newProgress.SetSize(newProgress.TextSize);
            newProgress.Events.OnMouseOver += evt => newProgress.color = Color.Gold;
            newProgress.Events.OnMouseOut += evt => newProgress.color = Color.White;
            newProgress.Events.OnLeftDown += evt =>
            {
                indexInputer.ClearText();
                newProgressPanel.Info.IsVisible = true;
                bg.LockInteract(false);
            };
            bg.Register(newProgress);

            UIText deleteProgress = new("删除进度表");
            deleteProgress.SetPos(groupFilter.Width + indexList.showArea.Width + newProgress.Width + 30, 5);
            deleteProgress.SetSize(deleteProgress.TextSize);
            deleteProgress.Events.OnMouseOver += evt => deleteProgress.color = Color.Gold;
            deleteProgress.Events.OnMouseOut += evt => deleteProgress.color = Color.White;
            deleteProgress.Events.OnLeftDown += evt =>
            {
                if (datas.TryGetValue(EditMod, out var pages) && pages.ContainsKey(EditPage))
                {
                    var inner = indexList.expandView.InnerUIE;
                    inner.Remove(x => x is UIText index && index.text == EditPage);
                    datas[EditMod].Remove(EditPage);
                    if (inner.Any())
                    {
                        UIText firstIndex = inner[0] as UIText;
                        indexList.ChangeShowElement(firstIndex);
                        firstIndex.Events.LeftDown(firstIndex);
                    }
                    else
                    {
                        indexList.showArea.RemoveAll();
                    }
                    ClearTemp();
                }
            };
            bg.Register(deleteProgress);

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

            UIItemSlot itemSlot = new();
            itemSlot.SetCenter(taskPanel.Width + 26, 0, 0, 0.5f);
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
            itemSlot.ReDraw = sb =>
            {
                itemSlot.DrawSelf(sb);
                Item item = itemSlot.ContainedItem;
                StringBuilder text = new();
                text.Append("物品ID：" + item.type);
                text.AppendLine();
                text.Append("对应物块ID：" + item.createTile);
                text.AppendLine();
                text.Append("对应BuffID：" + item.buffType);
                ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.MouseText.Value, text.ToString(),
                    itemSlot.HitBox().BottomLeft() + Vector2.UnitY * 5, Color.White, 0, Vector2.Zero, Vector2.One);
            };
            Register(itemSlot);

            UIVnlPanel dataInput = new(0, 0);
            dataInput.SetPos(0, 30);
            dataInput.SetSize(0, -30, 1, 1);
            taskPanel.Register(dataInput);

            UIContainerPanel dataView = new() { spaceY = 5 };
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

                                UIVnlPanel valueInputBg = new(0, 28);
                                valueInputBg.Info.Width.Percent = 1;
                                valueInputBg.SetPos(0, innerY);
                                constructPanel.Register(valueInputBg);

                                UIInputBox valueInputer = new("类型为" + info.Type.Name);
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
                                innerY += 48;
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
                                        datas[EditMod][EditPage][task] = ge.pos;
                                    };
                                    ge.Events.OnUpdate += GESlotUpdate;
                                    ge.Events.OnRightDoubleClick += evt =>
                                    {
                                        datas[EditMod][EditPage].Remove(task);
                                        GEPos.Remove(ge.pos);
                                        eventView.InnerUIE.Remove(ge);
                                    };
                                    Vector2 pos = Vector2.Zero;
                                    while (GEPos.Contains(pos)) pos.X++;
                                    ge.pos = pos;
                                    ge.SetPos(pos * 80);
                                    GEPos.Add(pos);
                                    eventView.AddElement(ge);
                                    datas[EditMod][EditPage].Add(task, pos);
                                }
                            };
                            constructPanel.Register(create);
                            constructPanel.SetSize(0, innerY + 48, 1);
                        }
                    };
                    typeSelector.AddElement(type);
                }
            }
            var inner = typeSelector.expandView.InnerUIE;
            UIText firstConstruct = inner[0] as UIText;
            typeSelector.ChangeShowElement(firstConstruct);
            firstConstruct.Events.LeftDown(firstConstruct);
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
            foreach ((string modName, Dictionary<string, Dictionary<GameEvent, Vector2>> pages) in datas)
            {
                Directory.CreateDirectory(Path.Combine(root, modName));
                foreach ((string pageName, Dictionary<GameEvent, Vector2> ges) in pages)
                {
                    using FileStream stream = File.OpenWrite(Path.Combine(root, modName, pageName + ".dat"));
                    TagCompound tag = [];
                    List<TagCompound> subTags = [];
                    foreach (var (ge, pos) in ges)
                    {
                        var subtag = GEManager.Save(ge);
                        if (subtag != null)
                        {
                            subtag["posX"] = pos.X;
                            subtag["posY"] = pos.Y;
                            subTags.Add(subtag);
                        }
                    }
                    tag["data"] = subTags;
                    TagIO.ToStream(tag, stream);
                }
            }
            Main.NewText("保存成功");
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
                                datas[modName][pageName].Add(ge, pos);
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
        private void ClearTemp()
        {
            GEPos.Clear();
            tempSelect.Clear();
            frameSelect.Clear();
            interacted.Clear();
            eventView?.InnerUIE.RemoveAll(MatchGESlot);
            eventView?.Vscroll.ForceSetPixel(0);
            eventView?.Hscroll.ForceSetPixel(0);
        }
        private void LoadGEs(string pageName)
        {
            EditPage = pageName;
            ClearTemp();
            foreach (var (ge, pos) in datas[EditMod][EditPage])
            {
                GEPos.Add(pos);
                UIGESlot slot = new(ge, pos);
                slot.SetPos(pos * 80);
                eventView.AddElement(slot);
            }
        }
    }
}
