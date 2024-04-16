using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ProgressSystem.GameEvents;
using ProgressSystem.UIEditor.ExtraUI;
using RUIModule;
using System.Diagnostics;
using System.IO;
using System.Text;
using Terraria.GameContent;
using Terraria.UI.Chat;

namespace ProgressSystem.UIEditor
{
    public class GEEditor : ContainerElement
    {
        private const string BaseName = "成就";
        internal static GEEditor Ins;
        /// <summary>
        /// 当前进度组GESlot位置
        /// </summary>
        internal static HashSet<Vector2> AchPos;
        public GEEditor() => Ins = this;
        private bool dragging;
        private bool draggingSelected;
        private Vector2 selectedStart;

        /// <summary>
        /// 已经被选中的GESlot
        /// </summary>
        private HashSet<UIAchSlot> frameSelect;

        /// <summary>
        /// 临时被选中的GESlot
        /// </summary>
        private HashSet<UIAchSlot> tempSelect;

        /// <summary>
        /// 本次碰撞判定已经交互过的GESlot
        /// </summary>
        private HashSet<UIAchSlot> interacted;

        /// <summary>
        /// 成就视区
        /// </summary>
        private UIContainerPanel achView;

        /// <summary>
        /// Require信息输入视区
        /// </summary>
        private UIContainerPanel dataView;

        /// <summary>
        /// 已添加的条件视区
        /// </summary>
        private UIContainerPanel conditionView;
        private UIVnlPanel editPanel;
        private UIVnlPanel pagePanel;
        private UIInputBox pageInputer;
        private UIInputBox achNameInputer;
        private UIInputBox savePathInputer;
        private UIDropDownList<UIText> pageList;
        /// <summary>
        /// 用于判定包含的GE鼠标碰撞箱
        /// </summary>
        private static UIAchCollision collision;

        /// <summary>
        /// 选中的作为前置的Ach
        /// </summary>
        private UIAchSlot preSetting;
        private UIAchSlot editingAch;
        private UIText saveTip;
        private static bool LeftShift;
        private static bool LeftCtrl;
        private static bool LeftAlt;
        /// <summary>
        /// 正在编辑的modName
        /// </summary>
        private Mod editingMod;
        /// <summary>
        /// 正在编辑的进度组名
        /// </summary>
        private string editingPage;
        private AchievementPage EditingPage => AchievementManager.PagesByMod[editingMod][editingPage];
        /// <summary>
        /// 检测是否已按下ctrl + S
        /// </summary>
        private bool trySave;
        private bool tryDelete;
        public override void OnInitialization()
        {
            base.OnInitialization();
            if (Main.gameMenu) return;
            Info.IsVisible = true;
            RemoveAll();

            editingMod = ProgressSystem.Instance;
            AchPos = [];
            tempSelect = [];
            frameSelect = [];
            interacted = [];

            RegisterEditPagePanel();
            RegisterEditAchPanel();
            RegisterNewPagePanel();
        }
        public override void Update(GameTime gt)
        {
            KeyboardState state = Keyboard.GetState();
            LeftShift = state.IsKeyDown(Keys.LeftShift);
            LeftCtrl = state.IsKeyDown(Keys.LeftControl);
            LeftAlt = state.IsKeyDown(Keys.LeftAlt);
            bool pressS = state.IsKeyDown(Keys.S);
            if (!pressS) trySave = false;
            if (!trySave && LeftCtrl && pressS)
            {
                SaveProgress();
                trySave = true;
                ChangeSaveState(true);
            }
            if (!tryDelete && state.IsKeyDown(Keys.Delete))
            {
                foreach (UIAchSlot slot in frameSelect)
                {
                    AchPos.Remove(slot.pos);
                    EditingPage.Achievements.Remove(slot.ach.FullName);
                    achView.RemoveElement(slot);
                }

            }
            if (dragging || collision != null)
            {
                Point target = Main.MouseScreen.ToPoint();
                var eh = achView.Hscroll;
                var ev = achView.Vscroll;
                if (target.X > achView.Right && eh.Real < 1)
                {
                    eh.MoveView(target.X - achView.Right, 15);
                }
                else if (target.X < achView.Left && eh.Real > 0)
                {
                    eh.MoveView(target.X - achView.Left, 40);
                }
                if (target.Y > achView.Bottom && ev.Real < 1)
                {
                    ev.MoveView(target.Y - achView.Bottom, 15);
                }
                else if (target.Y < achView.Top && ev.Real > 0)
                {
                    ev.MoveView(target.Y - achView.Top, 40);
                }
            }
            base.Update(gt);
            if (draggingSelected)
            {
                Vector2 mouse = Main.MouseScreen;
                Vector2 origin = achView.ChildrenElements[0].HitBox(false).TopLeft();
                int x = (int)(mouse.X - origin.X) / 80;
                int y = (int)(mouse.Y - origin.Y) / 80;
                x = Math.Max(x, 0);
                y = Math.Max(y, 0);
                Vector2 p = new(x, y);
                Vector2 offset = p - selectedStart;
                if (offset != Vector2.Zero)
                {
                    bool allMet = true;
                    foreach (UIAchSlot ge in frameSelect)
                    {
                        AchPos.Remove(ge.pos);
                    }
                    foreach (UIAchSlot ge in frameSelect)
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
                        Vector2 pos = ge.pos + offset;
                        if (AchPos.Contains(pos))
                        {
                            allMet = false;
                            break;
                        }
                    }
                    if (allMet)
                    {
                        foreach (UIAchSlot ge in frameSelect)
                        {
                            ge.pos += offset;
                            AchPos.Add(ge.pos);
                            ge.SetPos(ge.pos * 80);
                        }
                        selectedStart = p;
                        ChangeSaveState(false);
                    }
                }
            }
        }
        private void RegisterEditAchPanel()
        {
            UIVnlPanel newAchBg = new(430, 300) { canDrag = true };
            newAchBg.SetCenter(newAchBg.Width / 2, 0, 0, 0.5f);
            newAchBg.Info.SetMargin(10);
            Register(newAchBg);

            UIVnlPanel dataPanel = new(0, 0);
            dataPanel.SetPos(0, 40);
            dataPanel.SetSize(230, -40, 0, 1);
            newAchBg.Register(dataPanel);

            dataView = new() { spaceY = 10 };
            dataView.autoPos[0] = true;
            dataView.SetPos(10, 10);
            dataView.SetSize(-40, -20, 1, 1);
            dataPanel.Register(dataView);

            VerticalScrollbar dataV = new(28, false, false);
            dataV.Info.Top.Pixel += 5;
            dataV.Info.Height.Pixel -= 10;
            dataView.SetVerticalScrollbar(dataV);
            dataPanel.Register(dataV);

            UIVnlPanel conditionPanel = new(0, 0);
            conditionPanel.SetSize(170, -65, 0, 1);
            conditionPanel.SetPos(240, 40);
            conditionPanel.Info.SetMargin(10);
            newAchBg.Register(conditionPanel);

            conditionView = new();
            conditionView.SetSize(-10, 0, 1, 1);
            conditionView.autoPos[0] = true;
            conditionPanel.Register(conditionView);

            VerticalScrollbar cdsV = new();
            cdsV.Info.Left.Pixel += 10;
            conditionView.SetVerticalScrollbar(cdsV);
            conditionPanel.Register(cdsV);

            UIVnlPanel achNameInputBg = new(0, 0);
            achNameInputBg.SetSize(170, 30);
            achNameInputBg.SetPos(240, 0);
            newAchBg.Register(achNameInputBg);

            achNameInputer = new("输入成就名");
            achNameInputer.SetSize(-40, 0, 1, 1);
            achNameInputBg.Register(achNameInputer);

            UIClose clearName = new();
            clearName.SetCenter(-10, 0, 1, 0.5f);
            clearName.Events.OnLeftDown += evt => achNameInputer.ClearText();
            achNameInputBg.Register(clearName);

            UIText saveChange = new("保存更改");
            saveChange.SetSize(saveChange.TextSize);
            saveChange.SetCenter(-85, -5, 1, 1);
            saveChange.HoverToGold();
            saveChange.Events.OnLeftDown += evt =>
            {
                string text = achNameInputer.Text;
                if (text.Any())
                {
                    EditingPage.Achievements.Remove(editingAch.ach.FullName);
                    Achievement ach = Achievement.Create(EditingPage, editingMod, text);
                    foreach (UIRequireText require in conditionView.InnerUIE.Cast<UIRequireText>())
                    {
                        ach.Requirements.Add(require.requirement);
                    }
                    editingAch.ach = ach;
                    ChangeSaveState(false);
                }
            };
            newAchBg.Register(saveChange);

            UIDropDownList<UIText> constrcutList = new(newAchBg, dataPanel, x =>
            {
                UIText text = new(x.text);
                text.SetPos(10, 5);
                return text;
            })
            { buttonXoffset = 10 };

            constrcutList.showArea.SetSize(230, 30);

            constrcutList.expandArea.SetPos(0, 40);
            constrcutList.expandArea.SetSize(230, 150);

            constrcutList.expandView.autoPos[0] = true;
            constrcutList.expandView.Vscroll.canDrag = false;

            foreach (var require in ModContent.GetContent<Requirement>())
            {
                var tables = require.GetConstructInfoTables();
                UIText requireType = new(require.GetType().Name.Replace("Requirement", ""));
                requireType.SetSize(requireType.TextSize);
                requireType.HoverToGold();
                requireType.Events.OnLeftDown += evt =>
                {
                    dataView.ClearAllElements();
                    foreach (var constructInfo in tables)
                    {
                        RegisterRequireDataPanel(constructInfo);
                    }
                };
                constrcutList.AddElement(requireType);
            }
            var inner = constrcutList.expandView.InnerUIE;
            constrcutList.ChangeShowElement(0);
        }
        private void RegisterEditPagePanel()
        {
            editPanel = new(1000, 800);
            editPanel.SetCenter(0, 0, 0.55f, 0.5f);
            editPanel.Info.SetMargin(10);
            editPanel.canDrag = true;
            Register(editPanel);

            UIItemSlot itemSlot = new();
            itemSlot.Events.OnLeftDown += evt =>
            {
                if (Main.mouseItem.type > ItemID.None)
                {
                    itemSlot.ContainedItem = Main.mouseItem.Clone();
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
                text.Append("物块ID：" + item.createTile);
                text.AppendLine();
                text.Append("BuffID：" + item.buffType);
                ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.MouseText.Value, text.ToString(),
                    itemSlot.HitBox().BottomLeft() + Vector2.UnitY * 5, Color.White, 0, Vector2.Zero, Vector2.One);
            };
            editPanel.Register(itemSlot);

            UIVnlPanel groupFilter = new(0, 0);
            groupFilter.SetPos(0, 150);
            groupFilter.SetSize(100, -150, 0, 1);
            groupFilter.Info.SetMargin(10);
            editPanel.Register(groupFilter);

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
            editPanel.Register(eventPanel);

            pageList = new(editPanel, eventPanel, x =>
            {
                UIText text = new(x.text);
                text.SetPos(10, 5);
                return text;
            })
            { buttonXoffset = 10 };

            pageList.showArea.SetPos(110, 0);
            pageList.showArea.SetSize(200, 30);

            pageList.expandArea.SetPos(110, 30);
            pageList.expandArea.SetSize(200, 100);

            pageList.expandView.autoPos[0] = true;


            foreach (Mod mod in ModLoader.Mods)
            {
                if (mod.Side != ModSide.Both || !mod.HasAsset("icon")) continue;
                string modName = mod.Name;
                UIModSlot modSlot = new(RUIHelper.T2D(modName + "/icon"), modName) { hoverText = mod.DisplayName };
                modSlot.ReDraw = sb =>
                {
                    modSlot.DrawSelf(sb);
                    if (editingMod.Name == modSlot.modName)
                    {
                        RUIHelper.DrawRec(sb, modSlot.HitBox(), 5f, Color.Red);
                    }
                };
                modSlot.Events.OnLeftDown += evt =>
                {
                    pageList.ClearAllElements();
                    editingMod = ModLoader.GetMod(modSlot.modName);
                    ClearTemp();
                    if (AchievementManager.PagesByMod.TryGetValue(editingMod, out var modPages))
                    {
                        bool have = false;
                        foreach (var (page, ges) in modPages)
                        {
                            UIText pageName = new(page);
                            pageName.SetSize(pageName.TextSize);
                            pageName.Events.OnMouseOver += evt => pageName.color = Color.Gold;
                            pageName.Events.OnMouseOut += evt => pageName.color = Color.White;
                            pageName.Events.OnLeftDown += evt => LoadPage(pageName.text);
                            pageList.AddElement(pageName);
                            have = true;
                        }
                        if (have)
                        {
                            pageList.ChangeShowElement(0);
                            pageList.expandView.InnerUIE[0].Events.LeftDown(null);
                        }
                    }
                };
                groupView.AddElement(modSlot);
            }
            groupView.InnerUIE[0].Events.LeftDown(null);

            UIText newProgress = new("新建进度表");
            newProgress.SetPos(groupFilter.Width + pageList.showArea.Width + 20, 5);
            newProgress.SetSize(newProgress.TextSize);
            newProgress.Events.OnMouseOver += evt => newProgress.color = Color.Gold;
            newProgress.Events.OnMouseOut += evt => newProgress.color = Color.White;
            newProgress.Events.OnLeftDown += evt =>
            {
                pageInputer.ClearText();
                pagePanel.Info.IsVisible = true;
                editPanel.LockInteract(false);
            };
            editPanel.Register(newProgress);

            UIText deleteProgress = new("删除进度表");
            deleteProgress.SetPos(groupFilter.Width + pageList.showArea.Width + newProgress.Width + 30, 5);
            deleteProgress.SetSize(deleteProgress.TextSize);
            deleteProgress.Events.OnMouseOver += evt => deleteProgress.color = Color.Gold;
            deleteProgress.Events.OnMouseOut += evt => deleteProgress.color = Color.White;
            deleteProgress.Events.OnLeftDown += evt =>
            {
                if (AchievementManager.RemovePage(EditingPage))
                {
                    pageList.ChangeShowElement(0);
                    ClearTemp();
                }
            };
            editPanel.Register(deleteProgress);

            saveTip = new("已保存", Color.Green);
            saveTip.SetSize(saveTip.TextSize);
            saveTip.SetPos(groupFilter.Width + pageList.showArea.Width +
                newProgress.Width + deleteProgress.Width + 40, 5);
            saveTip.Events.OnLeftDown += evt =>
            {
                SaveProgress();
                ChangeSaveState(true);
            };
            editPanel.Register(saveTip);

            UIVnlPanel savePathInputBg = new(350, 30);
            savePathInputBg.SetPos(-360, 0, 1);
            editPanel.Register(savePathInputBg);

            savePathInputer = new("输入保存路径");
            savePathInputer.SetSize(-40, 0, 1, 1);
            savePathInputBg.Register(savePathInputer);

            UIAdjust selectSavePath = new(AssetLoader.VnlAdjust)
            {
                hoverText = "打开资源管理器选择复制路径"
            };
            selectSavePath.SetCenter(-30, 0, 1, 0.5f);
            selectSavePath.Events.OnLeftDown += evt =>
            {
                Main.QueueMainThreadAction(() =>
                {
                    Process.Start("explorer.exe", "/select");
                });
            };
            savePathInputBg.Register(selectSavePath);

            UIClose clearSavePath = new();
            clearSavePath.SetCenter(-10, 0, 1, 0.5f);
            clearSavePath.Events.OnLeftDown += evt => savePathInputer.ClearText();
            savePathInputBg.Register(clearSavePath);

            achView = new();
            achView.SetSize(-20, -20, 1, 1);
            achView.Events.OnLeftDown += evt =>
            {
                if (!LeftAlt && preSetting != null)
                {
                    preSetting.preSetting = false;
                    preSetting = null;
                }
            };
            achView.Events.OnLeftDoubleClick += evt =>
            {
                Point mouse = (Main.MouseScreen - achView.ChildrenElements[0].HitBox(false).TopLeft()).ToPoint();
                Vector2 pos = new(mouse.X / 80, mouse.Y / 80);
                if (AchPos.Contains(pos)) return;
                string name = BaseName;
                int i = 1;
                while (EditingPage.Achievements.ContainsKey(editingMod.Name + "." + name + i)) i++;
                Achievement ach = Achievement.Create(EditingPage, editingMod, name + i);
                ach.Position = pos;
                UIAchSlot slot = new(ach, pos);
                RegisterEventToGESlot(slot);
                slot.SetPos(pos * 80);
                achView.AddElement(slot);
            };
            achView.Events.OnRightDown += evt =>
            {
                collision = new();
                achView.AddElement(collision);
                if (!Keyboard.GetState().IsKeyDown(Keys.LeftShift))
                {
                    foreach (UIAchSlot ge in frameSelect)
                    {
                        ge.selected = false;
                    }
                    frameSelect.Clear();
                }
            };
            achView.Events.OnRightUp += evt =>
            {
                achView.RemoveElement(collision);
                foreach (UIAchSlot ge in tempSelect)
                {
                    frameSelect.Add(ge);
                }
                collision = null;
                tempSelect.Clear();
                interacted.Clear();
                if (LeftAlt && preSetting != null)
                {
                    if (frameSelect.Any())
                    {
                        foreach (UIAchSlot ge in frameSelect)
                        {
                            if (preSetting != ge)
                            {
                                if (preSetting.PostGE.Contains(ge))
                                {
                                    UIRequireLine line = preSetting.postGE.First(x => x.end == ge);
                                    preSetting.postGE.Remove(line);
                                    achView.RemoveElement(line);
                                }
                                else
                                {
                                    UIRequireLine line = new(preSetting, ge);
                                    achView.AddElement(line, 100);
                                    preSetting.postGE.Add(line);
                                }
                            }
                            ge.selected = false;
                        }
                        ChangeSaveState(false);
                        frameSelect.Clear();
                    }
                }
            };
            eventPanel.Register(achView);

            VerticalScrollbar ev = new(80);
            ev.Info.Left.Pixel += 10;
            achView.SetVerticalScrollbar(ev);
            eventPanel.Register(ev);

            HorizontalScrollbar eh = new(80) { useScrollWheel = false };
            eh.Info.Top.Pixel += 10;
            achView.SetHorizontalScrollbar(eh);
            eventPanel.Register(eh);

            Texture2D line = TextureAssets.MagicPixel.Value;
            Color color = Color.White;
            for (int i = 0; i < 50; i++)
            {
                UIImage hline = new(line, 5000, 2, color: color);
                hline.SetPos(0, i * 80);
                achView.AddElement(hline);

                UIImage vline = new(line, 2, 5000, color: color);
                vline.SetPos(80 * i, 0);
                achView.AddElement(vline);
            }
        }
        private void RegisterNewPagePanel()
        {
            pagePanel = new(300, 200, opacity: 1);
            pagePanel.SetCenter(300, 0, 0.5f, 0.5f);
            pagePanel.Info.IsVisible = false;
            pagePanel.Info.SetMargin(10);
            Register(pagePanel);

            UIVnlPanel inputBg = new(200, 30);
            inputBg.SetCenter(0, 0, 0.5f, 0.25f);
            pagePanel.Register(inputBg);

            UIText report = new("不可为空")
            {
                color = Color.Red
            };
            report.SetSize(report.TextSize);
            report.SetCenter(0, 0, 0.5f, 0.5f);
            pagePanel.Register(report);

            pageInputer = new("输入进度组名称");
            pageInputer.SetSize(-40, 0, 1, 1);
            pageInputer.OnInputText += text => MatchPageName(text, report);
            inputBg.Register(pageInputer);

            UIClose clearText = new();
            clearText.Events.OnLeftDown += evt => pageInputer.ClearText();
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
                    editingPage = pageInputer.Text;
                    //datas[EditMod][EditPage] = [];
                    editPanel.LockInteract(true);
                    pagePanel.Info.IsVisible = false;
                    UIText pageName = new(editingPage);
                    pageName.SetSize(pageName.TextSize);
                    pageName.Events.OnMouseOver += evt => pageName.color = Color.Gold;
                    pageName.Events.OnMouseOut += evt => pageName.color = Color.White;
                    pageName.Events.OnLeftDown += evt => LoadPage(pageName.text);
                    pageList.AddElement(pageName);
                    pageList.ChangeShowElement(pageName);
                    AchievementPage.Create(editingMod, editingPage);
                    ClearTemp();
                    SaveProgress();
                }
            };
            pagePanel.Register(submit);

            UIText cancel = new("取消");
            cancel.SetSize(cancel.TextSize);
            cancel.SetCenter(0, 0, 0.7f, 0.75f);
            cancel.Events.OnMouseOver += evt => cancel.color = Color.Gold;
            cancel.Events.OnMouseOut += evt => cancel.color = Color.White;
            cancel.Events.OnLeftDown += evt =>
            {
                editPanel.LockInteract(true);
                pagePanel.Info.IsVisible = false;
            };
            pagePanel.Register(cancel);
        }
        private void GESlotLeftCheck(BaseUIElement uie)
        {
            UIAchSlot ge = uie as UIAchSlot;
            if (LeftAlt)
            {
                frameSelect.Clear();
                tempSelect.Clear();
                if (preSetting == null)
                {
                    preSetting = ge;
                    ge.preSetting = true;
                }
                else
                {
                    if (preSetting != ge)
                    {
                        if (preSetting.PostGE.Contains(ge))
                        {
                            UIRequireLine line = preSetting.postGE.First(x => x.end == ge);
                            preSetting.postGE.Remove(line);
                            achView.RemoveElement(line);
                        }
                        else
                        {
                            UIRequireLine line = new(preSetting, ge);
                            achView.AddElement(line, 100);
                            preSetting.postGE.Add(line);
                        }
                        ChangeSaveState(false);
                    }
                }
                return;
            }
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
                Point mouse = (Main.MouseScreen - achView.ChildrenElements[0].HitBox(false).TopLeft()).ToPoint();
                selectedStart = new(mouse.X / 80, mouse.Y / 80);
            }
            editingAch = ge;
            achNameInputer.Text = ge.ach.Name;
            conditionView.ClearAllElements();
            foreach (Requirement require in editingAch.ach.Requirements)
            {
                UIRequireText req = new(require);
                req.delete.Events.OnLeftDown += evt =>
                {
                    conditionView.InnerUIE.Remove(req);
                };
                conditionView.AddElement(req);
            }
            dragging = true;
        }
        private void GESlotUpdate(BaseUIElement uie)
        {
            UIAchSlot ge = uie as UIAchSlot;
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
            try
            {
                string path = savePathInputer.Text;
                if (path == null || !File.Exists(path))
                {
                    path = string.Empty;
                }

                string directory;
                if (path == string.Empty)
                {
                    path = Path.Combine(Main.SavePath, "ModSources", ModName);
                }
                if (path.EndsWith(".dat"))
                {
                    var splitedPath = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    directory = string.Join(Path.DirectorySeparatorChar, splitedPath[..^1]);
                }
                else
                {
                    directory = path;
                    path = string.Join(Path.DirectorySeparatorChar, path, "Achievements.dat");
                }
                Directory.CreateDirectory(directory);
                using var stream = File.OpenWrite(path);
                AchievementManager.SaveStaticDataToStream(stream);
                Main.NewText("保存成功");
            }
            catch
            {
                Main.NewText("保存失败");
            }
        }
        private static bool MatchTempGE(BaseUIElement uie) => uie is UIAchSlot or UIRequireLine;
        private void ClearTemp()
        {
            AchPos.Clear();
            tempSelect.Clear();
            frameSelect.Clear();
            interacted.Clear();
            achView?.InnerUIE.RemoveAll(MatchTempGE);
            achView?.Vscroll.ForceSetPixel(0);
            achView?.Hscroll.ForceSetPixel(0);
        }
        private void LoadPage(string pageName)
        {
            editingPage = pageName;
            ClearTemp();
            foreach (Achievement ach in EditingPage.Achievements.Values)
            {
                UIAchSlot slot = new(ach, ach.Position);
                RegisterEventToGESlot(slot);
                AchPos.Add(ach.Position ?? Vector2.Zero);
                achView.AddElement(slot);
            }
        }
        private void RegisterEventToGESlot(UIAchSlot ge)
        {
            var ev = achView.Vscroll;
            var eh = achView.Hscroll;
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
            ge.Events.OnLeftDown += GESlotLeftCheck;
            ge.Events.OnLeftUp += evt =>
            {
                dragging = false;
                draggingSelected = false;
                if (ge.ach.Position != ge.pos)
                {
                    ge.ach.Position = ge.pos;
                    ChangeSaveState(false);
                }
            };
            ge.Events.OnUpdate += GESlotUpdate;
            ge.Events.OnRightDoubleClick += evt =>
            {
                AchievementManager.PagesByMod[editingMod][editingPage].Achievements.Remove(ge.ach.FullName);
                AchPos.Remove(ge.pos);
                achView.InnerUIE.Remove(ge);
                ChangeSaveState(false);
            };
            ge.ReDraw = sb =>
            {
                ge.DrawSelf(sb);
                if (ge.ach == editingAch.ach)
                {
                    RUIHelper.DrawRec(sb, ge.HitBox().Modified(4, 4, -8, -8), 2f, Color.SkyBlue);
                }
            };
        }
        private void RegisterRequireDataPanel(ConstructInfoTable<Requirement> data)
        {
            UIVnlPanel constructPanel = new(0, 0);
            constructPanel.Info.SetMargin(10);
            dataView.AddElement(constructPanel);
            int innerY = 0;
            foreach (var info in data)
            {
                UIText name = new(info.DisplayName.Value ?? "Anonymous");
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
                var bind = info;
                valueInputer.OnInputText += text =>
                {
                    if (text.Any())
                    {
                        bind.SetValue(text);
                        legal.ChangeText(bind.IsMet ? ("合法值：" + bind.GetValue()) : "不合法");
                    }
                    else legal.ChangeText(bind.Important ? "可以为空" : "不可为空");
                };
                valueInputBg.Register(valueInputer);

                UIClose clear = new();
                clear.SetCenter(-10, 0, 1, 0.5f);
                clear.Events.OnLeftDown += evt => valueInputer.ClearText();
                valueInputBg.Register(clear);
                innerY += 48;
            }
            UIText create = new("添加条件");
            create.SetSize(create.TextSize);
            create.SetPos(0, innerY);
            create.Events.OnMouseOver += evt => create.color = Color.Gold;
            create.Events.OnMouseOut += evt => create.color = Color.White;
            create.Events.OnLeftDown += evt =>
            {
                if (data.TryConstruct(out Requirement condition))
                {
                    int count = conditionView.InnerUIE.Count;
                    UIRequireText require = new(condition);
                    require.delete.Events.OnLeftDown += evt =>
                    {
                        conditionView.InnerUIE.Remove(require);
                    };
                    conditionView.AddElement(require);
                }
            };
            constructPanel.Register(create);
            constructPanel.SetSize(0, innerY + 48, 1);
            dataView.Calculation();
        }
        private void MatchPageName(string text, UIText report)
        {
            if (text.Any())
            {
                if (AchievementManager.PagesByMod.TryGetValue(editingMod, out var mod))
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
        }
        private void ChangeSaveState(bool saved)
        {
            if (saved)
            {
                saveTip.ChangeText("已保存");
                saveTip.color = Color.Green;
            }
            else
            {
                saveTip.ChangeText("未保存");
                saveTip.color = Color.Red;
            }
        }
    }
}
