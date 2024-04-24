using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ProgressSystem.Core.Requirements;
using ProgressSystem.Core.Rewards;
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
        /// 已添加的条件视区
        /// </summary>
        private UIContainerPanel conditionView;
        private UIContainerPanel rewardView;
        private UIVnlPanel editPanel;
        private UIVnlPanel pagePanel;
        private UIInputBox pageInputer;
        private UIInputBox achNameInputer;
        private UIInputBox savePathInputer;
        private UIText submit;
        private UIText cdsCount;
        private UIText preCount;
        private UIText combineCount;

        private UIDropDownList<UIText> pageList;
        /// <summary>
        /// 用于判定包含的GE鼠标碰撞箱
        /// </summary>
        private static UIAchCollision collision;

        /// <summary>
        /// 选中的作为前置的Ach
        /// </summary>
        private UIAchSlot preSetting;
        private string editingAchName;
        private RequirementList? editingRequires;
        private CombineRequirement? editingCombine;
        private Dictionary<string, UIAchSlot> slotByFullName;
        private AchievementPage? EditingPage => AchievementManager.PagesByMod.TryGetValue(editingMod, out var pages)
                    && pages.TryGetValue(editingPage, out var page) ? page : null;

        private Achievement? EditingAch => editingAchName == "" ? null : slotByFullName[editingAchName].ach;
        private UIAchSlot? EditingAchSlot => editingAchName == "" ? null : slotByFullName[editingAchName];
        private UIText saveTip;
        private static bool LeftShift;
        private static bool LeftCtrl;
        private static bool LeftAlt;
        private Mod editingMod;
        /// <summary>
        /// 正在编辑的进度组名
        /// </summary>
        private string editingPage;
        private bool trySave;
        private bool tryDelete;
        public override void OnInitialization()
        {
            base.OnInitialization();
            if (Main.gameMenu)
                return;
            Info.IsVisible = true;
            RemoveAll();

            editingMod = ProgressSystem.Instance;
            AchPos = [];
            tempSelect = [];
            frameSelect = [];
            interacted = [];
            slotByFullName = [];

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
            bool delete = state.IsKeyDown(Keys.Delete);
            bool pressR = state.IsKeyDown(Keys.R);
            if (!pressS)
                trySave = false;
            if (!delete)
                tryDelete = false;
            if (!trySave && LeftCtrl && pressS)
            {
                SaveProgress();
                trySave = true;
                ChangeSaveState(true);
            }
            if (!tryDelete && delete)
            {
                foreach (UIAchSlot slot in frameSelect)
                {
                    RemoveAchSlot(slot, true);
                }
                frameSelect.Clear();
                tryDelete = true;
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
            UIVnlPanel editBg = new(430, 300) { canDrag = true };
            editBg.Info.SetMargin(10);
            editBg.SetCenter(editBg.Width / 2f, 0, 0, 0.5f);
            Register(editBg);

            UIVnlPanel[] editPanels = new UIVnlPanel[3];
            for (int i = 0; i < 3; i++)
            {
                ref UIVnlPanel panel = ref editPanels[i];
                panel = new(0, 0);
                panel.Info.SetMargin(10);
                panel.SetSize(-30, 0, 1, 1);
                editBg.Register(panel);
            }
            editPanels[1].Info.IsVisible = false;
            editPanels[2].Info.IsVisible = false;
            string path = "ProgressSystem/Assets/Textures/UI/";

            void SwitchPanel(int id)
            {
                for (int i = 0; i < 3; i++)
                    editPanels[i].Info.IsVisible = i == id;
            }

            UIImage baseInfo = new(RUIHelper.T2D(path + "BaseInfo")) { hoverText = "基本信息" };
            baseInfo.SetPos(-22, 0, 1);
            baseInfo.Events.OnLeftDown += evt => SwitchPanel(0);
            editBg.Register(baseInfo);

            UIImage condition = new(RUIHelper.T2D(path + "Condition")) { hoverText = "条件设置" };
            condition.SetPos(-22, 32, 1);
            condition.Events.OnLeftDown += evt => SwitchPanel(1);
            editBg.Register(condition);

            UIImage reward = new(RUIHelper.T2D(path + "reward")) { hoverText = "奖励设置" };
            reward.SetPos(-22, 64, 1);
            reward.Events.OnLeftDown += evt => SwitchPanel(2);
            editBg.Register(reward);

            UIAdjust adjust = new(AssetLoader.VnlAdjust, new(20, 20));
            editBg.Register(adjust);

            //TODO: 分开注册基础、条件、奖励面板
            RegisterBaseInfoPanel(editPanels[0]);
            RegisterConditionPanel(editPanels[1]);
            RegisterRewardPanle(editPanels[2]);
        }
        private void RegisterBaseInfoPanel(BaseUIElement panel)
        {
            int y = 0;
            UIVnlPanel achNameInputBg = new(0, 0);
            achNameInputBg.SetSize(0, 30, 1);
            panel.Register(achNameInputBg);
            y += achNameInputBg.Height + 10;

            UIText nameChecker = new("未选中成就", Color.Red);
            nameChecker.SetPos(0, y);
            nameChecker.SetSize(nameChecker.TextSize);
            panel.Register(nameChecker);

            achNameInputer = new("输入成就名");
            achNameInputer.SetSize(-70, 0, 1, 1);
            achNameInputer.OnInputText += text =>
            {
                var achs = EditingPage?.Achievements;
                if (achs == null)
                    return;
                Achievement current = EditingAch;
                if (text.Any())
                {
                    if (text == EditingAch.Name)
                    {
                        nameChecker.ChangeText("名称可用", false);
                        nameChecker.color = Color.Green;
                        return;
                    }
                    if (achs.TryGetValue(current.Mod.Name + "." + text, out Achievement? ach))
                    {
                        nameChecker.ChangeText($"名称已被{ach.Position}占用", false);
                        nameChecker.color = Color.Red;
                    }
                    else
                    {
                        nameChecker.ChangeText("名称可用", false);
                        nameChecker.color = Color.Green;
                    }
                }
                else
                {
                    nameChecker.ChangeText("名称不可为空", false);
                    nameChecker.color = Color.Red;
                }
            };
            achNameInputBg.Register(achNameInputer);

            UIClose clearName = new();
            clearName.SetCenter(-10, 0, 1, 0.5f);
            clearName.Events.OnLeftDown += evt => achNameInputer.ClearText();
            achNameInputBg.Register(clearName);

            UIText changeName = new("修改名称");
            changeName.SetSize(changeName.TextSize);
            changeName.SetPos(-changeName.Width, y, 1);
            changeName.HoverToGold();
            changeName.Events.OnLeftDown += evt =>
            {
                if (nameChecker.color == Color.Green)
                {
                    Achievement current = EditingAch;
                    UIAchSlot currentSlot = EditingAchSlot;
                    slotByFullName.Remove(editingAchName);
                    EditingPage.Achievements.Remove(editingAchName);
                    current.Name = achNameInputer.Text;
                    editingAchName = current.FullName;
                    slotByFullName.Add(current.FullName, currentSlot);
                    EditingPage.Achievements.Add(editingAchName, current);
                    ChangeSaveState(false);
                }
            };
            achNameInputBg.Register(changeName);
            y += nameChecker.Height;

            UIVnlPanel submitBg = new(0, 0);
            submitBg.SetPos(0, y);
            submitBg.SetSize(0, 30, 1);
            panel.Register(submitBg);
            y += submitBg.Height + 10;

            submit = new("需要手动提交    否");
            submit.SetSize(submit.TextSize);
            submit.SetCenter(0, 5, 0.5f, 0.5f);
            submit.HoverToGold();
            submit.Events.OnLeftDown += evt =>
            {
                if (EditingAch == null)
                    return;
                ref bool needSubmit = ref EditingAch.NeedSubmit;
                needSubmit = !needSubmit;
                submit.ChangeText($"需要手动提交    {(needSubmit ? "是" : "否")}");
                EditingAch.ShouldSaveStaticData = true;
                ChangeSaveState(false);
            };
            submitBg.Register(submit);

            UIVnlPanel preNeedCountBg = new(0, 0);
            preNeedCountBg.SetPos(0, y);
            preNeedCountBg.SetSize(0, 30, 1);
            panel.Register(preNeedCountBg);
            y += preNeedCountBg.Height + 10;

            UIText preNeedCount = new("前置进度需求量");
            preNeedCount.SetPos(10, 5);
            preNeedCount.SetSize(preNeedCount.TextSize);
            preNeedCountBg.Register(preNeedCount);

            int left = 0;
            UIImage preIncrease = new(AssetLoader.Increase);
            preIncrease.Info.Left.Set(-30, 1);
            preIncrease.Info.Top.Pixel = 5;
            preIncrease.Events.OnLeftDown += evt =>
            {
                if (EditingAch == null)
                    return;
                ref int? need = ref EditingAch.PredecessorCountNeeded;
                if (need == null)
                    return;
                need++;
                preCount.ChangeText(need.ToString(), false);
                EditingAch.ShouldSaveStaticData = true;
                ChangeSaveState(false);
            };
            preNeedCountBg.Register(preIncrease);
            left += 40;

            preCount = new("未选", drawStyle: 1);
            preCount.SetSize(preCount.TextSize);
            preCount.SetPos(-preCount.TextSize.X - left, 5, 1);
            preCount.HoverToGold();
            preCount.Events.OnLeftDown += evt =>
            {
                if (EditingAch == null)
                    return;
                ref int? need = ref EditingAch.PredecessorCountNeeded;
                string text;
                if (need.HasValue)
                {
                    need = null;
                    text = "null";
                }
                else
                {
                    need = 0;
                    text = "0";
                }
                preCount.ChangeText(text, false);
                EditingAch.ShouldSaveStaticData = true;
                ChangeSaveState(false);
            };
            preNeedCountBg.Register(preCount);
            left += preCount.Width + 10;

            UIImage preDecrease = new(AssetLoader.Decrease);
            preDecrease.Info.Left.Set(-left - 20, 1);
            preDecrease.Info.Top.Pixel = 5;
            preDecrease.Events.OnLeftDown += evt =>
            {
                if (EditingAch == null)
                    return;
                ref int? need = ref EditingAch.PredecessorCountNeeded;
                if (need == null)
                    return;
                need--;
                preCount.ChangeText(need.ToString(), false);
                EditingAch.ShouldSaveStaticData = true;
                ChangeSaveState(false);
            };
            preNeedCountBg.Register(preDecrease);

            UIVnlPanel cdsNeedCountBg = new(0, 0);
            cdsNeedCountBg.SetPos(0, y);
            cdsNeedCountBg.SetSize(0, 30, 1);
            panel.Register(cdsNeedCountBg);

            UIText cdsNeedCount = new("达成条件需求量");
            cdsNeedCount.SetPos(10, 5);
            cdsNeedCount.SetSize(cdsNeedCount.TextSize);
            cdsNeedCountBg.Register(cdsNeedCount);

            left = 0;
            UIImage cdsIncrease = new(AssetLoader.Increase);
            cdsIncrease.Info.Left.Set(-30, 1);
            cdsIncrease.Info.Top.Pixel = 5;
            cdsIncrease.Events.OnLeftDown += evt =>
            {
                if (EditingAch == null)
                    return;
                ref int need = ref EditingAch.RequirementCountNeeded;
                need += 1;
                cdsCount.ChangeText(need.ToString(), false);
                EditingAch.ShouldSaveStaticData = true;
                ChangeSaveState(false);
            };
            cdsNeedCountBg.Register(cdsIncrease);
            left += 40;

            cdsCount = new("未选", drawStyle: 1);
            cdsCount.SetSize(cdsCount.TextSize);
            cdsCount.SetPos(-cdsCount.TextSize.X - left, 5, 1);
            cdsNeedCountBg.Register(cdsCount);
            left += cdsCount.Width + 10;

            UIImage cdsDecrease = new(AssetLoader.Decrease);
            cdsDecrease.Info.Left.Set(-left - 20, 1);
            cdsDecrease.Info.Top.Pixel = 5;
            cdsDecrease.Events.OnLeftDown += evt =>
            {
                if (EditingAch == null)
                    return;
                ref int need = ref EditingAch.RequirementCountNeeded;
                if (need == 0)
                    return;
                need -= 1;
                cdsCount.ChangeText(need.ToString(), false);
                EditingAch.ShouldSaveStaticData = true;
                ChangeSaveState(false);
            };
            cdsNeedCountBg.Register(cdsDecrease);
        }
        private void RegisterConditionPanel(BaseUIElement panel)
        {
            int leftWidth = 150;
            UIVnlPanel constructBg = new(0, 0);
            constructBg.SetSize(leftWidth, -80, 0, 1);
            constructBg.SetPos(0, 80);
            panel.Register(constructBg);

            UIContainerPanel constructView = new() { spaceY = 10 };
            constructView.autoPos[0] = true;
            constructView.SetPos(10, 10);
            constructView.SetSize(-40, -20, 1, 1);
            constructBg.Register(constructView);

            VerticalScrollbar dataV = new(28, false, false);
            dataV.Info.Top.Pixel += 5;
            dataV.Info.Height.Pixel -= 10;
            constructView.SetVerticalScrollbar(dataV);
            constructBg.Register(dataV);

            UIVnlPanel cdsNeedCountBg = new(0, 0);
            cdsNeedCountBg.SetSize(0, 30, 1);
            panel.Register(cdsNeedCountBg);

            UIText needCount = new("需求达成数");
            needCount.SetPos(10, 5);
            needCount.SetSize(needCount.TextSize);
            cdsNeedCountBg.Register(needCount);

            int left = 0;
            UIImage cdsIncrease = new(AssetLoader.Increase);
            cdsIncrease.Info.Left.Set(-30, 1);
            cdsIncrease.Info.Top.Pixel = 5;
            cdsIncrease.Events.OnLeftDown += evt =>
            {
                if (editingCombine == null)
                {
                    if (EditingAch == null)
                        return;
                    else
                    {
                        ref int count = ref EditingAch.RequirementCountNeeded;
                        count++;
                        string c = count.ToString();
                        cdsCount.ChangeText(c, false);
                        combineCount.ChangeText(c, false);
                        EditingAch.ShouldSaveStaticData = true;
                        ChangeSaveState(false);
                    }
                }
                else
                {
                    editingCombine.needCount++;
                    combineCount.ChangeText(editingCombine.needCount.ToString(), false);
                    editingCombine.ShouldSaveStaticData = true;
                    CheckConditions(EditingAch.Requirements, 0);
                    ChangeSaveState(false);
                }
            };
            cdsNeedCountBg.Register(cdsIncrease);
            left += 40;

            combineCount = new("未选", drawStyle: 1);
            combineCount.SetSize(combineCount.TextSize);
            combineCount.SetPos(-combineCount.TextSize.X - left, 5, 1);
            cdsNeedCountBg.Register(combineCount);
            left += combineCount.Width + 10;

            UIImage cdsDecrease = new(AssetLoader.Decrease);
            cdsDecrease.Info.Left.Set(-left - 20, 1);
            cdsDecrease.Info.Top.Pixel = 5;
            cdsDecrease.Events.OnLeftDown += evt =>
            {
                if (editingCombine == null)
                {
                    if (EditingAch == null)
                        return;
                    else
                    {
                        ref int count = ref EditingAch.RequirementCountNeeded;
                        if (count == 0)
                            return;
                        count--;
                        string c = count.ToString();
                        combineCount.ChangeText(c, false);
                        cdsCount.ChangeText(c, false);
                        EditingAch.ShouldSaveStaticData = true;
                        ChangeSaveState(false);
                    }
                }
                else
                {
                    ref int count = ref editingCombine.needCount;
                    if (count == 0)
                        return;
                    count--;
                    combineCount.ChangeText(count.ToString(), false);
                    editingCombine.ShouldSaveStaticData = true;
                    CheckConditions(EditingAch.Requirements, 0);
                    ChangeSaveState(false);
                }
            };
            cdsNeedCountBg.Register(cdsDecrease);

            UIVnlPanel addCombineBg = new(0, 0);
            addCombineBg.SetPos(leftWidth + 10, 40);
            addCombineBg.SetSize(-leftWidth - 10, 30, 1);
            panel.Register(addCombineBg);

            UIText addCombine = new("添加组合条件") { hoverText = "默认需求一个" };
            addCombine.SetSize(addCombine.TextSize);
            addCombine.SetCenter(0, 5, 0.5f, 0.5f);
            addCombine.HoverToGold();
            addCombine.Events.OnLeftDown += evt =>
            {
                if (editingRequires == null)
                {
                    Main.NewText("请先选择一个成就栏位/条件层级");
                    return;
                }
                CombineRequirement condition = new(1)
                {
                    ShouldSaveStaticData = true
                };
                editingRequires.Add(condition);
                editingCombine = condition;
                editingRequires = condition.Requirements;
                combineCount.ChangeText(editingCombine.needCount.ToString(), false);
                CheckConditions(EditingAch.Requirements, 0);
                ChangeSaveState(false);
            };
            addCombineBg.Register(addCombine);

            UIVnlPanel conditionPanel = new(0, 0);
            conditionPanel.SetSize(-leftWidth - 10, -80, 1, 1);
            conditionPanel.SetPos(leftWidth + 10, 80);
            conditionPanel.Info.SetMargin(10);
            panel.Register(conditionPanel);

            conditionView = new();
            conditionView.SetSize(-20, -10, 1, 1);
            conditionView.autoPos[0] = true;
            conditionView.spaceY = 10;
            conditionView.Events.OnRightDown += evt => editingRequires = EditingAch.Requirements;
            conditionPanel.Register(conditionView);

            VerticalScrollbar cdsV = new();
            cdsV.Info.Left.Pixel += 10;
            conditionView.SetVerticalScrollbar(cdsV);
            conditionPanel.Register(cdsV);

            HorizontalScrollbar cdsH = new() { useScrollWheel = false };
            cdsH.Info.Top.Pixel += 10;
            conditionView.SetHorizontalScrollbar(cdsH);
            conditionPanel.Register(cdsH);

            UIDropDownList<UIText> constructList = new(panel, constructBg, x =>
            {
                UIText text = new(x.text);
                text.SetPos(10, 5);
                return text;
            })
            { buttonXoffset = 10 };

            constructList.showArea.SetPos(0, 40);
            constructList.showArea.SetSize(leftWidth, 30);

            constructList.expandArea.SetPos(0, 80);
            constructList.expandArea.SetSize(leftWidth, -80, 0, 1);

            constructList.expandView.autoPos[0] = true;
            constructList.expandView.Vscroll.canDrag = false;
            string end = "Requirement";
            int len = end.Length;

            foreach (var require in ModContent.GetContent<Requirement>())
            {
                if (require is CombineRequirement)
                    continue;
                var tables = require.GetConstructInfoTables();
                var requireName = require.GetType().Name;
                if (requireName.EndsWith(end))
                {
                    requireName = requireName[0..^len];
                }
                UIText requireType = new(requireName);
                requireType.SetSize(requireType.TextSize);
                requireType.HoverToGold();
                requireType.Events.OnLeftDown += evt =>
                {
                    constructView.ClearAllElements();
                    foreach (var constructInfo in tables)
                    {
                        RegisterRequireDataPanel(constructInfo, constructView);
                    }
                };
                constructList.AddElement(requireType);
            }
            constructList.ChangeShowElement(0);
        }
        private void RegisterRewardPanle(BaseUIElement panel)
        {
            int leftWidth = 150;
            UIVnlPanel constructBg = new(0, 0);
            constructBg.SetSize(leftWidth, -30, 0, 1);
            constructBg.SetPos(0, 30);
            panel.Register(constructBg);

            UIContainerPanel constructView = new() { spaceY = 10 };
            constructView.autoPos[0] = true;
            constructView.SetPos(10, 10);
            constructView.SetSize(-40, -20, 1, 1);
            constructBg.Register(constructView);

            UIDropDownList<UIText> constructList = new(panel, constructBg, x =>
            {
                UIText text = new(x.text);
                x.SetPos(10, 5);
                return text;
            })
            { buttonXoffset = 10 };

            constructList.showArea.SetSize(leftWidth, 30);

            constructList.expandArea.SetPos(0, 40);
            constructList.expandArea.SetSize(leftWidth, 100);

            constructList.expandView.autoPos[0] = true;
            constructList.expandView.Vscroll.canDrag = false;

            string end = "reward";
            int len = end.Length;

            foreach (Reward reward in ModContent.GetContent<Reward>())
            {
                var tables = reward.GetConstructInfoTables();
                var rewardName = reward.GetType().Name;
                if (rewardName.EndsWith(end))
                {
                    rewardName = rewardName[0..^len];
                }
                UIText rewardType = new(rewardName);
                rewardType.SetSize(rewardType.TextSize);
                rewardType.HoverToGold();
                rewardType.Events.OnLeftDown += evt =>
                {
                    constructView.ClearAllElements();
                    foreach (var constructInfo in tables)
                    {
                        RegisterRewardDataPanel(constructInfo, constructView);
                    }
                };
                constructList.AddElement(rewardType);
                constructList.ChangeShowElement(0);
            }
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
            int left = 110;

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
            left += pageList.showArea.Width + 10;

            foreach (Mod mod in ModLoader.Mods)
            {
                if (mod.Side != ModSide.Both || !mod.HasAsset("icon"))
                    continue;
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
            newProgress.SetPos(left, 5);
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
            left += newProgress.Width + 10;

            UIText deleteProgress = new("删除进度表");
            deleteProgress.SetPos(left, 5);
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
            left += deleteProgress.Width + 10;

            UIText checkPos = new("位置检查");
            checkPos.SetPos(left, 5);
            checkPos.SetSize(checkPos.TextSize);
            checkPos.HoverToGold();
            checkPos.Events.OnLeftDown += evt =>
            {
                HashSet<Vector2> pos = [];
                foreach (UIAchSlot slot in slotByFullName.Values)
                {
                    if (!pos.Add(slot.pos))
                    {
                        Vector2 p = Vector2.Zero;
                        while (pos.Contains(p))
                        {
                            if (p.Y + 1 > p.X)
                            {
                                p.X++;
                            }
                            else
                                p.Y++;
                        }
                        slot.pos = p;
                        slot.SetPos(p * 80);
                    }
                }
                AchPos = pos;
            };
            editPanel.Register(checkPos);
            left += checkPos.Width + 10;

            saveTip = new("已保存", Color.Green);
            saveTip.SetSize(saveTip.TextSize);
            saveTip.SetPos(left, 5);
            saveTip.Events.OnLeftDown += evt =>
            {
                SaveProgress();
                ChangeSaveState(true);
            };
            editPanel.Register(saveTip);
            left += saveTip.Width + 10;

            UIVnlPanel savePathInputBg = new(0, 0);
            savePathInputBg.SetPos(left, 0);
            savePathInputBg.SetSize(-left, 30, 1);
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
                if (EditingAchSlot?.pos == pos)
                    return;
                string name = BaseName;
                int i = 1;
                while (EditingPage.Achievements.ContainsKey(editingMod.Name + "." + name + i))
                    i++;
                Achievement ach = Achievement.Create(EditingPage, editingMod, name + i);
                ach.ShouldSaveStaticData = true;
                RegisterAchSlot(ach, pos);
            };
            achView.Events.OnRightDown += evt =>
            {
                collision = new();
                achView.AddElement(collision);
                if (!LeftCtrl)
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
                                Achievement orig = preSetting.ach;
                                Achievement pre = ge.ach;
                                if (pre.Predecessors.Contains(orig))
                                {
                                    Main.NewText("不可互为前置");
                                    continue;
                                }
                                if (preSetting.PreAch.Contains(ge))
                                {
                                    RemoveRequireLine(orig, pre);
                                }
                                else
                                {
                                    RegisterRequireLine(orig, pre);
                                }
                                ChangeSaveState(false);
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

            UIText report = new("不可为空", Color.Red);
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
                    var page = AchievementPage.Create(editingMod, editingPage);
                    page.ShouldSaveStaticData = true;
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
        private void AchSlotLeftCheck(BaseUIElement uie)
        {
            UIAchSlot ge = uie as UIAchSlot;
            Achievement ach = ge.ach;
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
                        Achievement orig = preSetting.ach;
                        Achievement pre = ach;
                        if (pre.Predecessors.Contains(orig))
                        {
                            Main.NewText("不可互为前置");
                            return;
                        }
                        if (preSetting.PreAch.Contains(ge))
                        {
                            RemoveRequireLine(orig, pre);
                        }
                        else
                        {
                            RegisterRequireLine(orig, pre);
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
                else
                    ge.selected = frameSelect.Add(ge);
            }
            else if (frameSelect.Any())
            {
                draggingSelected = true;
                Point mouse = (Main.MouseScreen - achView.ChildrenElements[0].HitBox(false).TopLeft()).ToPoint();
                selectedStart = new(mouse.X / 80, mouse.Y / 80);
            }
            AchPos.Remove(ge.pos);
            ChangeEditingAch(ach);
            dragging = true;
        }
        private void GESlotUpdate(BaseUIElement uie)
        {
            UIAchSlot ge = uie as UIAchSlot;
            if (collision != null)
            {
                bool intersects = ge.HitBox().Intersects(collision.selector);
                if (LeftCtrl)
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
            editingAchName = "";
            achView?.InnerUIE.RemoveAll(MatchTempGE);
            achView?.Vscroll.ForceSetPixel(0);
            achView?.Hscroll.ForceSetPixel(0);
            slotByFullName.Clear();
        }
        private void LoadPage(string pageName)
        {
            editingPage = pageName;
            ClearTemp();
            foreach (Achievement ach in EditingPage.Achievements.Values)
            {
                RegisterAchSlot(ach, ach.Position.Value, false);
            }
            foreach (UIAchSlot slot in slotByFullName.Values)
            {
                Achievement orig = slot.ach;
                foreach (Achievement pre in orig.Predecessors)
                {
                    RegisterRequireLine(orig, pre, false);
                }
            }
            ChangeEditingAch(null);
        }
        private void RegisterEventToAchSlot(UIAchSlot ge)
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
            ge.Events.OnLeftDown += AchSlotLeftCheck;
            ge.Events.OnLeftUp += evt =>
            {
                dragging = false;
                draggingSelected = false;
                if (ge.ach.Position != ge.pos)
                {
                    ge.ach.Position = ge.pos;
                    ChangeSaveState(false);
                }
                AchPos.Add(ge.pos);
            };
            ge.Events.OnUpdate += GESlotUpdate;
            ge.Events.OnRightDoubleClick += evt => RemoveAchSlot(ge);
            ge.ReDraw = sb =>
            {
                ge.DrawSelf(sb);
                if (ge.ach.FullName == editingAchName)
                {
                    RUIHelper.DrawRec(sb, ge.HitBox().Modified(4, 4, -8, -8), 2f, Color.SkyBlue);
                }
            };
        }
        private void RegisterRequireDataPanel(ConstructInfoTable<Requirement> data, UIContainerPanel constructView)
        {
            UIVnlPanel constructPanel = new(0, 0);
            constructPanel.Info.SetMargin(10);
            constructView.AddElement(constructPanel);
            int innerY = 0;
            foreach (var info in data)
            {
                UIText name = new(info.DisplayName.Value ?? "Anonymous");
                name.SetPos(0, innerY);
                name.SetSize(name.TextSize);
                constructPanel.Register(name);
                innerY += 28;

                UIText legal = new(info.Important ? "可以为空" : "不可为空");
                legal.SetPos(0, innerY);
                constructPanel.Register(legal);
                innerY += 28;

                UIVnlPanel valueInputBg = new(0, 28);
                valueInputBg.Info.Width.Percent = 1;
                valueInputBg.SetPos(0, innerY);
                constructPanel.Register(valueInputBg);

                UIInputBox valueInputer = new(info.Type.Name);
                valueInputer.SetSize(-40, 0, 1, 1);
                var bind = info;
                valueInputer.OnInputText += text =>
                {
                    if (text.Any())
                    {
                        bind.SetValue(text);
                        legal.ChangeText(bind.IsMet ? ("合法值：" + bind.GetValue()) : "不合法");
                    }
                    else
                        legal.ChangeText(bind.Important ? "可以为空" : "不可为空");
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
                if (editingRequires == null)
                {
                    Main.NewText("请先选择一个成就栏位/条件层级");
                    return;
                }
                if (data.TryConstruct(out Requirement? condition))
                {
                    condition.ShouldSaveStaticData = true;
                    editingRequires.Add(condition);
                    CheckConditions(EditingAch.Requirements, 0);
                    ChangeSaveState(false);
                }
            };
            constructPanel.Register(create);
            constructPanel.SetSize(0, innerY + 48, 1);
            constructView.Calculation();
        }
        private void RegisterRewardDataPanel(ConstructInfoTable<Reward> data, UIContainerPanel constructView)
        {
            UIVnlPanel constructPanel = new(0, 0);
            constructPanel.Info.SetMargin(10);
            constructView.AddElement(constructPanel);
            int innerY = 0;
            foreach (var info in data)
            {
                UIText name = new(info.DisplayName.Value ?? "Anonymous");
                name.SetPos(0, innerY);
                name.SetSize(name.TextSize);
                constructPanel.Register(name);
                innerY += 28;

                UIText legal = new(info.Important ? "可以为空" : "不可为空");
                legal.SetPos(0, innerY);
                constructPanel.Register(legal);
                innerY += 28;

                UIVnlPanel valueInputBg = new(0, 28);
                valueInputBg.Info.Width.Percent = 1;
                valueInputBg.SetPos(0, innerY);
                constructPanel.Register(valueInputBg);

                UIInputBox valueInputer = new(info.Type.Name);
                valueInputer.SetSize(-40, 0, 1, 1);
                var bind = info;
                valueInputer.OnInputText += text =>
                {
                    if (text.Any())
                    {
                        bind.SetValue(text);
                        legal.ChangeText(bind.IsMet ? ("合法值：" + bind.GetValue()) : "不合法");
                    }
                    else
                        legal.ChangeText(bind.Important ? "可以为空" : "不可为空");
                };
                valueInputBg.Register(valueInputer);

                UIClose clear = new();
                clear.SetCenter(-10, 0, 1, 0.5f);
                clear.Events.OnLeftDown += evt => valueInputer.ClearText();
                valueInputBg.Register(clear);
                innerY += 48;
            }
            UIText create = new("添加奖励");
            create.SetSize(create.TextSize);
            create.SetPos(0, innerY);
            create.Events.OnMouseOver += evt => create.color = Color.Gold;
            create.Events.OnMouseOut += evt => create.color = Color.White;
            create.Events.OnLeftDown += evt =>
            {
                if (EditingAch == null)
                {
                    Main.NewText("请先选择一个成就栏位");
                    return;
                }
                if (data.TryConstruct(out Reward? reward))
                {
                    reward.ShouldSaveStaticData = true;
                    EditingAch.Rewards.Add(reward);
                    CheckRewards(EditingAch.Rewards);
                    ChangeSaveState(false);
                }
            };
            constructPanel.Register(create);
            constructPanel.SetSize(0, innerY + 48, 1);
            constructView.Calculation();
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
        private UIAchSlot RegisterAchSlot(Achievement ach, Vector2 pos, bool changeToEditing = true)
        {
            UIAchSlot slot = new(ach, pos);
            RegisterEventToAchSlot(slot);
            achView.AddElement(slot);
            slotByFullName.Add(slot.ach.FullName, slot);
            AchPos.Add(pos);
            if (changeToEditing)
            {
                ChangeEditingAch(ach);
                ChangeSaveState(false);
            }
            return slot;
        }
        /// <summary>
        /// range表示是否范围删除
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="range"></param>
        private void RemoveAchSlot(UIAchSlot slot, bool range = false)
        {
            string achName = slot.ach.FullName;
            achView.RemoveElement(slot);
            slotByFullName.Remove(achName);
            if (range)
                slot.Info.NeedRemove = true;
            else
                EditingPage.Achievements.Remove(achName);
            AchPos.Remove(slot.pos);
            if (editingAchName == achName)
            {
                ChangeEditingAch(null);
            }
            ChangeSaveState(false);
        }
        private void RegisterRequireLine(Achievement orig, Achievement pre, bool addPreToAch = true)
        {
            UIAchSlot start = slotByFullName[pre.FullName];
            UIAchSlot end = slotByFullName[orig.FullName];
            UIRequireLine line = new(start, end);
            achView.AddElement(line, 100);
            end.preLine.Add(line);
            if (addPreToAch)
            {
                orig.AddPredecessor(pre.FullName, true);
            }
        }
        private void RemoveRequireLine(Achievement orig, Achievement pre)
        {
            UIAchSlot me = slotByFullName[orig.FullName];
            UIRequireLine line = me.preLine.First(x => x.start.ach == pre);
            achView.RemoveElement(line);
            me.preLine.Remove(line);
            orig.RemovePredecessor(pre.FullName, true);
        }
        private void ChangeEditingAch(Achievement ach)
        {
            editingCombine = null;
            if (ach == null)
            {
                editingRequires = null;
                editingAchName = "";
                achNameInputer.ClearText();
                preCount.ChangeText("未选", false);
                cdsCount.ChangeText("未选", false);
                combineCount.ChangeText("未选", false);
                conditionView.ClearAllElements();
                rewardView.ClearAllElements();
                return;
            }
            else
            {
                int? pre = ach.PredecessorCountNeeded;
                preCount.ChangeText(pre.HasValue ? pre.Value.ToString() : "null", false);
                cdsCount.ChangeText(ach.RequirementCountNeeded.ToString(), false);
                combineCount.ChangeText(ach.RequirementCountNeeded.ToString(), false);
                submit.ChangeText($"需要手动提交  {(ach.NeedSubmit ? "是" : "否")}");
                editingAchName = ach.FullName;
                achNameInputer.Text = ach.Name;
                achNameInputer.OnInputText?.Invoke(ach.Name);
                CheckConditions(ach.Requirements, 0);
                CheckRewards(ach.Rewards);
                editingRequires = ach.Requirements;
            }
        }
        private void CheckConditions(RequirementList requires, int index)
        {
            if (index == 0)
                conditionView.ClearAllElements();
            if (requires.Any())
            {
                foreach (Requirement require in requires)
                {
                    UIRequireText text = new(require, requires);
                    text.SetPos(index * 30, 0);
                    text.delete.Events.OnLeftDown += evt =>
                    {
                        text.requirements.Remove(text.requirement);
                        CheckConditions(EditingAch.Requirements, 0);
                        conditionView.Calculation();
                    };
                    conditionView.AddElement(text);
                    if (require is CombineRequirement combine)
                    {
                        text.text.HoverToGold();
                        var cb = combine;
                        var requireList = combine.Requirements;
                        text.text.Events.OnLeftDown += evt =>
                        {
                            editingRequires = requireList;
                            editingCombine = cb;
                            combineCount.ChangeText(editingCombine.needCount.ToString(), false);
                        };
                        text.text.Events.OnUpdate += evt =>
                        {
                            text.text.overrideColor = editingRequires == requireList ? Color.Red : null;
                        };
                        CheckConditions(combine.Requirements, index + 1);
                    }
                }
            }
            else
            {
                UIText none = new("空条件");
                none.SetPos(index * 30, 0);
                none.SetSize(none.TextSize);
                conditionView.AddElement(none);
            }
            if (index == 0)
                conditionView.Calculation();
        }
        private void CheckRewards(RewardList rewards)
        {
            rewardView.ClearAllElements();
            foreach (Reward reward in rewards)
            {
                UIRewardText text = new(reward, rewards);
                text.delete.Events.OnLeftDown += evt =>
                {
                    text.rewards.Remove(text.reward);
                    CheckConditions(EditingAch.Requirements, 0);
                };
                rewardView.AddElement(text);
            }
            rewardView.Calculation();
        }
    }
}
