using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ProgressSystem.Common.Configs;
using ProgressSystem.Core.Requirements;
using ProgressSystem.Core.Rewards;
using ProgressSystem.UI.DeveloperMode.ExtraUI;
using RUIModule;
using System.Diagnostics;
using System.IO;
using System.Text;
using Terraria.GameContent;
using Terraria.UI.Chat;

namespace ProgressSystem.UI.DeveloperMode
{
    public class GEEditor : ContainerElement
    {
        private const string BaseName = "成就";
        internal static GEEditor Ins = null!;
        /// <summary>
        /// 当前进度组GESlot位置
        /// </summary>
        internal static HashSet<Vector2> AchPos = [];
        public GEEditor() => Ins = this;
        private bool dragging;
        private bool draggingSelected;
        private Vector2 selectedStart;

        /// <summary>
        /// 已经被选中的GESlot
        /// </summary>
        private readonly HashSet<UIAchSlot> frameSelect = [];

        /// <summary>
        /// 临时被选中的GESlot
        /// </summary>
        private readonly HashSet<UIAchSlot> tempSelect = [];

        /// <summary>
        /// 本次碰撞判定已经交互过的GESlot
        /// </summary>
        private readonly HashSet<UIAchSlot> interacted = [];

        /// <summary>
        /// 成就视区
        /// </summary>
        private UIContainerPanel achView = null!;

        /// <summary>
        /// 已添加的需求视区
        /// </summary>
        private UIContainerPanel requireView = null!;

        /// <summary>
        /// 已添加的奖励视区
        /// </summary>
        private UIContainerPanel rewardView = null!;
        private UIVnlPanel mainPanel = null!;
        private UIVnlPanel pagePanel = null!;
        private UIInputBox pageInputer = null!;
        private UIInputBox achNameInputer = null!;
        private UIInputBox savePathInputer = null!;
        private UIText submit = null!;
        private UIText preCount = null!;
        /// <summary>
        /// require组合需求数文本
        /// </summary>
        private UIText rqsCountText = null!;
        /// <summary>
        /// Reward组合可选数文本
        /// </summary>
        private UIText rwsCountText = null!;

        private UIDropDownList<UIText> pageList = null!;
        /// <summary>
        /// 用于判定包含的GE鼠标碰撞箱
        /// </summary>
        private static UIAchCollision? collision;

        /// <summary>
        /// 选中的作为前置的Ach
        /// </summary>
        private UIAchSlot? preSetting;
        /// <summary>
        /// 正在编辑的成就的全名
        /// </summary>
        private string EditingAchFullName
        {
            get => EditingAch?.FullName ?? string.Empty;
            set => EditingAchSlot = slotByFullName.TryGetValue(value, out var achSlot) ? achSlot : null;
        }
        private CombineRequirement? _editingCombineRequire;
        private CombineRequirement? EditingCombineRequire
        {
            get => _editingCombineRequire;
            set
            {
                _editingCombineRequire = value;
                UpdateRqsCountText();
            }
        }
        private IList<Requirement>? EditingRequires => EditingCombineRequire?.Requirements ?? EditingAch?.Requirements;
        private CombineReward? _editingCombineReward;
        private CombineReward? EditingCombineReward
        {
            get => _editingCombineReward;
            set
            {
                _editingCombineReward = value;
                UpdateRwsCountText();
            }
        }
        private IList<Reward>? EditingRewards => EditingCombineReward?.Rewards ?? EditingAch?.Rewards;
        private readonly Dictionary<string, UIAchSlot> slotByFullName = [];
        private AchievementPage? EditingPage { get; set; }

        private Achievement? EditingAch
        {
            get => EditingAchSlot?.ach;
            set => EditingAchSlot = value == null ? null :
                slotByFullName.TryGetValue(value.FullName, out var achSlot) ? achSlot : null;
        }

        private UIAchSlot? EditingAchSlot { get; set; }
        private Mod editingMod = null!;
        private int editingPanel;
        private UIText saveTip = null!;
        private static bool LeftShift;
        private static bool LeftCtrl;
        private static bool LeftAlt;
        /// <summary>
        /// 正在编辑的进度组名
        /// </summary>
        private string EditingPageName
        {
            get => EditingPage?.Name ?? string.Empty;
            set => _ = AchievementManager.PagesByMod.TryGetValue(editingMod, out var pages)
                && pages.TryGetValue(value, out var page) ? EditingPage = page : null;
        }
        private bool trySave;
        private bool tryDelete;
        public override void OnInitialization()
        {
            base.OnInitialization();
            if (Main.gameMenu)
                return;
            Events.OnUpdate += evt =>
            {
                if (Main.playerInventory || !ClientConfig.Instance.DeveloperMode)
                {
                    Info.IsVisible = false;
                }
            };
            RemoveAll();

            editingMod = ProgressSystem.Instance;
            editingPanel = 0;
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
                editingPanel = id;
                for (int i = 0; i < 3; i++)
                    editPanels[i].Info.IsVisible = i == id;
            }

            void DrawFocusFrame(SpriteBatch sb, BaseUIElement uie, int id)
            {
                uie.DrawSelf(sb);
                if (editingPanel == id)
                {
                    RUIHelper.DrawRec(sb, uie.HitBox(), 2f, Color.Gold);
                }
            }

            UIImage baseInfo = new(RUIHelper.T2D(path + "BaseInfo")) { hoverText = "基本信息" };
            baseInfo.SetPos(-22, 0, 1);
            baseInfo.Events.OnLeftDown += evt => SwitchPanel(0);
            baseInfo.ReDraw = sb => DrawFocusFrame(sb, baseInfo, 0);
            editBg.Register(baseInfo);

            UIImage require = new(RUIHelper.T2D(path + "Require")) { hoverText = "需求设置" };
            require.SetPos(-22, 32, 1);
            require.Events.OnLeftDown += evt => SwitchPanel(1);
            require.ReDraw = sb => DrawFocusFrame(sb, require, 1);
            editBg.Register(require);

            UIImage reward = new(RUIHelper.T2D(path + "Reward")) { hoverText = "奖励设置" };
            reward.SetPos(-22, 64, 1);
            reward.Events.OnLeftDown += evt => SwitchPanel(2);
            reward.ReDraw = sb => DrawFocusFrame(sb, reward, 2);
            editBg.Register(reward);

            UIAdjust adjust = new();
            editBg.Register(adjust);

            RegisterBaseInfoPanel(editPanels[0]);
            RegisterRequirePanel(editPanels[1]);
            RegisterRewardPanle(editPanels[2]);
        }
        private void RegisterBaseInfoPanel(BaseUIElement panel)
        {
            int y = 0;
            UIVnlPanel achNameInputBg = new(0, 0);
            achNameInputBg.SetSize(0, 30, 1);
            achNameInputBg.Info.RightMargin.Pixel = 10;
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
                if (EditingPage == null || EditingAch == null)
                {
                    return;
                }
                Achievement current = EditingAch;
                var achs = EditingPage.Achievements;
                if (text.Any())
                {
                    if (text == current.Name)
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
                UIAchSlot? currentSlot = EditingAchSlot;
                if (nameChecker.color == Color.Green && currentSlot != null)
                {
                    Achievement ach = EditingAch!;
                    string oldName = EditingAchFullName;
                    slotByFullName.Remove(oldName);
                    EditingPage!.Remove(oldName);

                    ach.Name = achNameInputer.Text;
                    EditingPage!.Add(ach);
                    slotByFullName.Add(ach.FullName, currentSlot);
                    ach.ShouldSaveStaticData = true;
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
                if (EditingAch != null)
                {
                    ref bool needSubmit = ref EditingAch.NeedSubmit;
                    needSubmit = !needSubmit;
                    submit.ChangeText($"需要手动提交    {(needSubmit ? "是" : "否")}");
                    EditingAch.ShouldSaveStaticData = true;
                    ChangeSaveState(false);
                }
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
                {
                    Main.NewText("请先选择一个成就栏位");
                    return;
                }
                ref int? need = ref EditingAch.PredecessorCountNeeded;
                if (need == null)
                    return;
                need++;
                preCount.ChangeText(need.Value.ToString(), false);
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
                {
                    Main.NewText("请先选择一个成就栏位");
                    return;
                }
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
                {
                    Main.NewText("请先选择一个成就栏位");
                    return;
                }
                ref int? need = ref EditingAch.PredecessorCountNeeded;
                if (need == null)
                    return;
                need--;
                preCount.ChangeText(need.Value.ToString(), false);
                EditingAch.ShouldSaveStaticData = true;
                ChangeSaveState(false);
            };
            preNeedCountBg.Register(preDecrease);
        }
        private void RegisterRequirePanel(BaseUIElement panel)
        {
            int leftWidth = 150;
            UIVnlPanel constructBg = new(0, 0);
            constructBg.SetSize(leftWidth, -80, 0, 1);
            constructBg.SetPos(0, 80);
            panel.Register(constructBg);

            UIContainerPanel constructView = new();
            constructView.autoPos[0] = 10;
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
            UIImage increase = new(AssetLoader.Increase);
            increase.Info.Left.Set(-30, 1);
            increase.Info.Top.Pixel = 5;
            increase.Events.OnLeftDown += evt =>
            {
                CombineRequirement? combine = EditingCombineRequire;
                if (combine != null)
                {
                    combine.Count += 1;
                    UpdateRqsCountText();
                    combine.ShouldSaveStaticData = true;
                    CheckRequirements();
                    ChangeSaveState(false);
                }
                else if (EditingAch != null)
                {
                    EditingAch.RequirementCountNeeded += 1;
                    UpdateRqsCountText();
                    EditingAch.ShouldSaveStaticData = true;
                    CheckRequirements();
                    ChangeSaveState(false);
                }
                else
                {
                    Main.NewText("请先选择一个需求层级/成就栏位");
                }
            };
            cdsNeedCountBg.Register(increase);
            left += 40;

            rqsCountText = new("未选", drawStyle: 1);
            rqsCountText.SetSize(rqsCountText.TextSize);
            rqsCountText.SetPos(-rqsCountText.TextSize.X - left, 5, 1);
            cdsNeedCountBg.Register(rqsCountText);
            left += rqsCountText.Width + 10;

            UIImage decrease = new(AssetLoader.Decrease);
            decrease.Info.Left.Set(-left - 20, 1);
            decrease.Info.Top.Pixel = 5;
            decrease.Events.OnLeftDown += evt =>
            {
                CombineRequirement? combine = EditingCombineRequire;
                if (combine != null)
                {
                    combine.Count -= 1;
                    UpdateRqsCountText();
                    combine.ShouldSaveStaticData = true;
                    CheckRequirements();
                    ChangeSaveState(false);
                }
                else if (EditingAch != null)
                {
                    EditingAch.RequirementCountNeeded -= 1;
                    UpdateRqsCountText();
                    EditingAch.ShouldSaveStaticData = true;
                    CheckRequirements();
                    ChangeSaveState(false);
                }
                else
                {
                    Main.NewText("请先选择一个需求层级/成就栏位");
                }
            };
            cdsNeedCountBg.Register(decrease);

            UIVnlPanel addCombineBg = new(0, 0);
            addCombineBg.SetPos(leftWidth + 10, 40);
            addCombineBg.SetSize(-leftWidth - 10, 30, 1);
            panel.Register(addCombineBg);

            UIText addCombine = new("添加组合需求") { hoverText = "默认需求一个" };
            addCombine.SetSize(addCombine.TextSize);
            addCombine.SetCenter(0, 5, 0.5f, 0.5f);
            addCombine.HoverToGold();
            addCombine.Events.OnLeftDown += evt =>
            {
                var editingRequires = EditingRequires;
                if (editingRequires == null)
                {
                    Main.NewText("请先选择一个需求层级/成就栏位");
                    return;
                }
                CombineRequirement require = new(1)
                {
                    ShouldSaveStaticData = true
                };
                editingRequires.Add(require);
                EditingCombineRequire = require;
                UpdateRqsCountText();
                CheckRequirements();
                ChangeSaveState(false);
            };
            addCombineBg.Register(addCombine);

            UIVnlPanel requirePanel = new(0, 0);
            requirePanel.SetSize(-leftWidth - 10, -80, 1, 1);
            requirePanel.SetPos(leftWidth + 10, 80);
            requirePanel.Info.SetMargin(10);
            panel.Register(requirePanel);

            requireView = new();
            requireView.SetSize(-20, -10, 1, 1);
            requireView.autoPos[0] = 10;
            requireView.Events.OnRightDown += evt => EditingCombineRequire = null;
            requirePanel.Register(requireView);

            VerticalScrollbar rqsV = new();
            rqsV.Info.Left.Pixel += 10;
            requireView.SetVerticalScrollbar(rqsV);
            requirePanel.Register(rqsV);

            HorizontalScrollbar rqsH = new() { useScrollWheel = false };
            rqsH.Info.Top.Pixel += 10;
            requireView.SetHorizontalScrollbar(rqsH);
            requirePanel.Register(rqsH);

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

            constructList.expandView.autoPos[0] = 5;
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
            constructBg.SetPos(0, 80);
            constructBg.SetSize(leftWidth, -80, 0, 1);
            panel.Register(constructBg);

            UIContainerPanel constructView = new();
            constructView.autoPos[0] = 10;
            constructView.SetPos(10, 10);
            constructView.SetSize(-40, -20, 1, 1);
            constructBg.Register(constructView);

            VerticalScrollbar dataV = new(28, false, false);
            dataV.Info.Top.Pixel += 5;
            dataV.Info.Height.Pixel -= 10;
            constructView.SetVerticalScrollbar(dataV);
            constructBg.Register(dataV);

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

            constructList.expandView.autoPos[0] = 5;
            constructList.expandView.Vscroll.canDrag = false;

            string end = "Reward";
            int len = end.Length;

            foreach (Reward reward in ModContent.GetContent<Reward>())
            {
                if (reward is CombineReward)
                    continue;
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
            UIVnlPanel rwsSelectCountBg = new(0, 0);
            rwsSelectCountBg.SetSize(0, 30, 1);
            panel.Register(rwsSelectCountBg);

            UIText selectCount = new("奖励可选数");
            selectCount.SetPos(10, 5);
            selectCount.SetSize(selectCount.TextSize);
            rwsSelectCountBg.Register(selectCount);

            int left = 0;
            UIImage increase = new(AssetLoader.Increase);
            increase.Info.Left.Set(-30, 1);
            increase.Info.Top.Pixel = 5;
            increase.Events.OnLeftDown += evt =>
            {
                CombineReward? combine = EditingCombineReward;
                if (combine != null)
                {
                    combine.Count += 1;
                    UpdateRwsCountText();
                    combine.ShouldSaveStaticData = true;
                    CheckRewards();
                    ChangeSaveState(false);
                }
                else
                {
                    Main.NewText("请先选择一个奖励层级");
                }
            };
            rwsSelectCountBg.Register(increase);
            left += 40;

            rwsCountText = new("未选", drawStyle: 1);
            rwsCountText.SetSize(rwsCountText.TextSize);
            rwsCountText.SetPos(-rwsCountText.TextSize.X - left, 5, 1);
            rwsSelectCountBg.Register(rwsCountText);
            left += rwsCountText.Width + 10;

            UIImage decrease = new(AssetLoader.Decrease);
            decrease.Info.Left.Set(-left - 20, 1);
            decrease.Info.Top.Pixel = 5;
            decrease.Events.OnLeftDown += evt =>
            {
                CombineReward? combine = EditingCombineReward;
                if (combine != null)
                {
                    combine.Count -= 1;
                    UpdateRwsCountText();
                    combine.ShouldSaveStaticData = true;
                    CheckRewards();
                    ChangeSaveState(false);
                }
                else
                {
                    Main.NewText("请先选择一个奖励层级");
                }
            };
            rwsSelectCountBg.Register(decrease);

            UIVnlPanel addCombineBg = new(0, 0);
            addCombineBg.SetPos(leftWidth + 10, 40);
            addCombineBg.SetSize(-leftWidth - 10, 30, 1);
            panel.Register(addCombineBg);

            UIText addCombine = new("添加选择奖励") { hoverText = "默认可选一个" };
            addCombine.SetSize(addCombine.TextSize);
            addCombine.SetCenter(0, 5, 0.5f, 0.5f);
            addCombine.HoverToGold();
            addCombine.Events.OnLeftDown += evt =>
            {
                var editingRewards = EditingRewards;
                if (editingRewards == null)
                {
                    Main.NewText("请先选择一个奖励层级");
                    return;
                }
                CombineReward combine = new()
                {
                    ShouldSaveStaticData = true
                };
                editingRewards.Add(combine);
                EditingCombineReward = combine;
                CheckRewards();
                ChangeSaveState(false);
            };
            addCombineBg.Register(addCombine);

            UIVnlPanel rewardPanel = new(0, 0);
            rewardPanel.SetPos(leftWidth + 10, 80);
            rewardPanel.SetSize(-leftWidth - 10, -80, 1, 1);
            rewardPanel.Info.SetMargin(10);
            panel.Register(rewardPanel);

            rewardView = new();
            rewardView.SetSize(-20, -10, 1, 1);
            rewardView.autoPos[0] = 5;
            rewardView.Events.OnRightDown += evt => EditingCombineReward = null;
            rewardPanel.Register(rewardView);

            VerticalScrollbar rwsV = new();
            rwsV.Info.Left.Pixel += 10;
            rewardView.SetVerticalScrollbar(rwsV);
            rewardPanel.Register(rwsV);

            HorizontalScrollbar rwsH = new() { useScrollWheel = false };
            rwsH.Info.Top.Pixel += 10;
            rewardView.SetHorizontalScrollbar(rwsH);
            rewardPanel.Register(rwsH);
        }
        private void RegisterEditPagePanel()
        {
            #region 主板
            mainPanel = new(1000, 800);
            mainPanel.SetCenter(0, 0, 0.55f, 0.5f);
            mainPanel.Info.SetMargin(10);
            mainPanel.canDrag = true;
            Register(mainPanel);
            #endregion

            #region ItemSlot
            UIItemSlot itemSlot = new();
            itemSlot.Events.OnLeftDown += evt =>
            {
                if (Main.mouseItem.type > ItemID.None)
                {
                    itemSlot.item = Main.mouseItem.Clone();
                }
                else
                {
                    itemSlot.item.SetDefaults();
                }
            };
            itemSlot.ReDraw = sb =>
            {
                itemSlot.DrawSelf(sb);
                Item item = itemSlot.item;
                StringBuilder text = new();
                text.Append("物品ID：" + item.type);
                text.AppendLine();
                text.Append("物块ID：" + item.createTile);
                text.AppendLine();
                text.Append("BuffID：" + item.buffType);
                ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.MouseText.Value, text.ToString(),
                    itemSlot.HitBox().BottomLeft() + Vector2.UnitY * 5, Color.White, 0, Vector2.Zero, Vector2.One);
            };
            mainPanel.Register(itemSlot);
            #endregion

            #region Mod 列表
            UIVnlPanel groupFilter = new(0, 0);
            groupFilter.SetPos(0, 150);
            groupFilter.SetSize(120, -150, 0, 1);
            groupFilter.Info.SetMargin(10);
            mainPanel.Register(groupFilter);

            UIContainerPanel groupView = new();
            groupView.SetSize(0, 0, 1, 1);
            groupView.autoPos[0] = 10;
            groupFilter.Register(groupView);

            VerticalScrollbar gv = new(90, false, false);
            groupView.SetVerticalScrollbar(gv);
            gv.Info.Left.Pixel += 10;
            groupFilter.Register(gv);
            #endregion

            #region 事件版
            int left = 130;
            UIVnlPanel eventPanel = new(0, 0);
            eventPanel.Info.SetMargin(10);
            eventPanel.SetPos(left, 40);
            eventPanel.SetSize(-left, -40, 1, 1, false);
            mainPanel.Register(eventPanel);
            #endregion

            #region 进度表列表
            pageList = new(mainPanel, eventPanel, x =>
            {
                UIText text = new(x.text);
                text.SetPos(10, 5);
                return text;
            })
            { buttonXoffset = 10 };

            pageList.showArea.SetPos(left, 0);
            pageList.showArea.SetSize(200, 30);

            pageList.expandArea.SetPos(left, 30);
            pageList.expandArea.SetSize(200, 100);

            pageList.expandView.autoPos[0] = 5;
            left += pageList.showArea.Width + 10;
            #endregion

            #region 注册所有的 Mod
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
                        foreach (var (name, page) in modPages)
                        {
                            UIText pageName = new(page.DisplayName.Value ?? name);
                            pageName.SetSize(pageName.TextSize);
                            pageName.Events.OnMouseOver += evt => pageName.color = Color.Gold;
                            pageName.Events.OnMouseOut += evt => pageName.color = Color.White;
                            pageName.Events.OnLeftDown += evt => LoadPage(name);
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
            groupView.Calculation();
            #endregion

            #region 新建进度表
            UIText newProgress = new("新建进度表");
            newProgress.SetPos(left, 5);
            newProgress.SetSize(newProgress.TextSize);
            newProgress.Events.OnMouseOver += evt => newProgress.color = Color.Gold;
            newProgress.Events.OnMouseOut += evt => newProgress.color = Color.White;
            newProgress.Events.OnLeftDown += evt =>
            {
                pageInputer.ClearText();
                pagePanel.Info.IsVisible = true;
                mainPanel.LockInteract(false);
            };
            mainPanel.Register(newProgress);
            left += newProgress.Width + 10;
            #endregion

            #region 删除进度表
            UIText deleteProgress = new("删除进度表");
            deleteProgress.SetPos(left, 5);
            deleteProgress.SetSize(deleteProgress.TextSize);
            deleteProgress.Events.OnMouseOver += evt => deleteProgress.color = Color.Gold;
            deleteProgress.Events.OnMouseOut += evt => deleteProgress.color = Color.White;
            deleteProgress.Events.OnLeftDown += evt =>
            {
                if (EditingPage == null)
                {
                    return;
                }
                if (AchievementManager.RemovePage(EditingPage))
                {
                    pageList.ChangeShowElement(0);
                    ClearTemp();
                }
            };
            mainPanel.Register(deleteProgress);
            left += deleteProgress.Width + 10;
            #endregion

            #region 位置检查
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
            mainPanel.Register(checkPos);
            left += checkPos.Width + 10;
            #endregion

            #region 保存提示
            saveTip = new("已保存", Color.Green);
            saveTip.SetSize(saveTip.TextSize);
            saveTip.SetPos(left, 5);
            saveTip.Events.OnLeftDown += evt =>
            {
                SaveProgress();
                ChangeSaveState(true);
            };
            mainPanel.Register(saveTip);
            left += saveTip.Width + 10;
            #endregion

            #region 保存路径
            UIVnlPanel savePathInputBg = new(0, 0);
            savePathInputBg.SetPos(left, 0);
            savePathInputBg.SetSize(-left, 30, 1);
            savePathInputBg.Info.RightMargin.Pixel = 10;
            mainPanel.Register(savePathInputBg);

            savePathInputer = new("输入保存路径");
            savePathInputer.SetSize(-40, 0, 1, 1);
            savePathInputBg.Register(savePathInputer);

            #region 打开资源管理器按钮
            UI3FrameImage selectSavePath = new(AssetLoader.VnlAdjust, x => false)
            {
                hoverText = "打开资源管理器选择复制路径"
            };
            selectSavePath.SetCenter(-40, 0, 1, 0.5f);
            selectSavePath.Events.OnLeftDown += evt =>
            {
                Main.QueueMainThreadAction(() =>
                {
                    var p = Process.Start("explorer.exe", "/select");
                });
            };
            savePathInputBg.Register(selectSavePath);
            #endregion

            #region 清除路径按钮
            UIClose clearSavePath = new();
            clearSavePath.SetCenter(-10, 0, 1, 0.5f);
            clearSavePath.Events.OnLeftDown += evt => savePathInputer.ClearText();
            savePathInputBg.Register(clearSavePath);
            #endregion

            #endregion

            #region 成就界面
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
                if (EditingPage == null)
                {
                    return;
                }
                Point mouse = (Main.MouseScreen - achView.ChildrenElements[0].HitBox(false).TopLeft()).ToPoint();
                Vector2 pos = new(mouse.X / 80, mouse.Y / 80);
                if (EditingAchSlot?.pos == pos)
                    return;
                string name = BaseName;
                int i = 1;
                while (EditingPage.Achievements.ContainsKey(editingMod.Name + "." + name + i))
                    i++;
                Achievement ach = new(EditingPage, editingMod, name + i);
                EditingPage.AddF(ach);
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
                if (!LeftAlt || preSetting == null || frameSelect.Count == 0)
                {
                    return;
                }
                foreach (UIAchSlot ge in frameSelect)
                {
                    if (preSetting == ge)
                    {
                        ge.selected = false;
                        continue;
                    }
                    Achievement orig = preSetting.ach;
                    Achievement pre = ge.ach;
                    /*
                    // 可以互为前置
                    if (pre.Predecessors.Contains(orig))
                    {
                        Main.NewText("不可互为前置");
                        continue;
                    }
                    */
                    if (preSetting.PreAch.Contains(ge))
                    {
                        RemoveRequireLine(orig, pre);
                    }
                    else
                    {
                        RegisterRequireLine(orig, pre);
                    }
                    ChangeSaveState(false);
                    ge.selected = false;
                }
                ChangeSaveState(false);
                frameSelect.Clear();
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
            #endregion
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
            clearText.SetCenter(-20, 0, 1, 0.5f);
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
                    string name = pageInputer.Text;
                    mainPanel.LockInteract(true);
                    pagePanel.Info.IsVisible = false;
                    UIText pageName = new(name);
                    pageName.SetSize(pageName.TextSize);
                    pageName.Events.OnMouseOver += evt => pageName.color = Color.Gold;
                    pageName.Events.OnMouseOut += evt => pageName.color = Color.White;
                    pageName.Events.OnLeftDown += evt => LoadPage(pageName.text);
                    pageList.AddElement(pageName);
                    pageList.ChangeShowElement(pageName);
                    EditingPage = AchievementPage.Create(editingMod, name);
                    EditingPage.ShouldSaveStaticData = true;
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
                mainPanel.LockInteract(true);
                pagePanel.Info.IsVisible = false;
            };
            pagePanel.Register(cancel);
        }
        private void AchSlotLeftCheck(BaseUIElement uie)
        {
            UIAchSlot ge = (UIAchSlot)uie;
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
            else if (frameSelect.Count != 0)
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
            UIAchSlot ge = (UIAchSlot)uie;
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
            EditingAchFullName = "";
            achView?.InnerUIE.RemoveAll(MatchTempGE);
            achView?.Vscroll.ForceSetPixel(0);
            achView?.Hscroll.ForceSetPixel(0);
            slotByFullName.Clear();
        }
        private void LoadPage(string pageName)
        {
            EditingPageName = pageName;
            if (EditingPage == null)
            {
                return;
            }
            ClearTemp();
            EditingPage.SetDefaultPositionForAchievements();
            foreach (Achievement ach in EditingPage.Achievements.Values)
            {
                RegisterAchSlot(ach, ach.Position!.Value, false);
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
                if (ge.ach.FullName == EditingAchFullName)
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
                        legal.ChangeText(bind.IsMet ? "合法值：" + bind.GetValue() : "不合法");
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
            UIText create = new("添加需求");
            create.SetSize(create.TextSize);
            create.SetPos(0, innerY);
            create.Events.OnMouseOver += evt => create.color = Color.Gold;
            create.Events.OnMouseOut += evt => create.color = Color.White;
            create.Events.OnLeftDown += evt =>
            {
                var editingRequires = EditingRequires;
                if (editingRequires == null)
                {
                    Main.NewText("请先选择一个成就栏位/需求层级");
                    return;
                }
                if (data.TryConstruct(out Requirement? require))
                {
                    require!.ShouldSaveStaticData = true;
                    editingRequires.Add(require);
                    CheckRequirements();
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
                        legal.ChangeText(bind.IsMet ? "合法值：" + bind.GetValue() : "不合法");
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
                    reward!.ShouldSaveStaticData = true;
                    EditingRewards.Add(reward);
                    CheckRewards();
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
                EditingPage!.Remove(achName);
            AchPos.Remove(slot.pos);
            if (EditingAchFullName == achName)
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
        private void ChangeEditingAch(Achievement? ach)
        {
            if (ach == null)
            {
                EditingAchFullName = "";
                EditingCombineRequire = null;
                EditingCombineReward = null;
                achNameInputer.ClearText();
                preCount.ChangeText("未选", false);
                requireView.ClearAllElements();
                rewardView.ClearAllElements();
                return;
            }
            else
            {
                EditingAchFullName = ach.FullName;
                int? pre = ach.PredecessorCountNeeded;
                preCount.ChangeText(pre.HasValue ? pre.Value.ToString() : "null", false);
                submit.ChangeText($"需要手动提交  {(ach.NeedSubmit ? "是" : "否")}");
                achNameInputer.Text = ach.Name;
                achNameInputer.OnInputText?.Invoke(ach.Name);
                CheckRequirements();
                CheckRewards();
                EditingCombineRequire = null;
                EditingCombineReward = null;
            }
        }
        private void CheckRequirements(RequirementList? requires = null, int index = 0)
        {
            if (index == 0)
                requireView.ClearAllElements();
            requires ??= EditingAch!.Requirements;
            if (requires.Count != 0)
            {
                foreach (Requirement require in requires)
                {
                    UIRequireText text = new(require, requires);
                    text.SetPos(index * 30, 0);
                    text.delete.Events.OnLeftDown += evt =>
                    {
                        if (EditingAch == null)
                        {
                            return;
                        }
                        text.requirements.Remove(text.requirement);
                        if (EditingCombineRequire == text.requirement)
                        {
                            EditingCombineRequire = null;
                        }
                        CheckRequirements();
                    };
                    requireView.AddElement(text);
                    if (require is CombineRequirement combine)
                    {
                        text.text.HoverToGold();
                        text.text.Events.OnLeftDown += evt =>
                        {
                            EditingCombineRequire = combine;
                            UpdateRqsCountText();
                        };
                        text.text.Events.OnUpdate += evt =>
                        {
                            text.text.overrideColor = EditingCombineRequire == combine ? Color.Red : null;
                        };
                        CheckRequirements(combine.Requirements, index + 1);
                    }
                }
            }
            else
            {
                UIText none = new("空需求");
                none.SetPos(index * 30, 0);
                none.SetSize(none.TextSize);
                requireView.AddElement(none);
            }
            if (index == 0)
                requireView.Calculation();
        }
        private void CheckRewards(RewardList? rewards = null, int index = 0)
        {
            if (index == 0)
                rewardView.ClearAllElements();
            rewards ??= EditingAch!.Rewards;
            if (rewards.Count != 0)
            {
                foreach (Reward reward in rewards)
                {
                    UIRewardText text = new(reward, rewards);
                    text.SetPos(index * 30, 0);
                    text.delete.Events.OnLeftDown += evt =>
                    {
                        if (EditingAch == null)
                        {
                            return;
                        }
                        text.rewards.Remove(text.reward);
                        if (EditingCombineReward == text.reward)
                        {
                            EditingCombineReward = null;
                        }
                        CheckRewards();
                    };
                    rewardView.AddElement(text);
                    if (reward is CombineReward combine)
                    {
                        text.text.HoverToGold();
                        var cb = combine;
                        text.text.Events.OnLeftDown += evt =>
                        {
                            EditingCombineReward = cb;
                        };
                        text.text.Events.OnUpdate += evt =>
                        {
                            text.text.overrideColor = EditingCombineReward == cb ? Color.Red : null;
                        };
                        CheckRewards(cb.Rewards, index + 1);
                    }
                }
            }
            else
            {
                UIText none = new("空奖励");
                none.SetPos(index * 30, 0);
                none.SetSize(none.TextSize);
                rewardView.AddElement(none);
            }
            if (index == 0)
                rewardView.Calculation();
        }
        private void UpdateRqsCountText() => rqsCountText.ChangeText(EditingCombineRequire?.Count.ToString() ??
            EditingAch?.RequirementCountNeeded.ToString() ?? "未选", false);
        private void UpdateRwsCountText() => rwsCountText.ChangeText(EditingCombineReward?.Count.ToString() ?? "未选", false);
    }
}
