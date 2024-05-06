using Microsoft.CodeAnalysis;
using ProgressSystem.Core.Requirements;
using ProgressSystem.Core.Rewards;
using ProgressSystem.UI.DeveloperMode.ExtraUI;
using System.IO;

namespace ProgressSystem.UI.DeveloperMode.AchEditor
{
    public partial class AchEditor : ContainerElement
    {
        private const string BaseName = "成就";
        internal static AchEditor Ins = null!;
        /// <summary>
        /// 当前进度组GESlot位置
        /// </summary>
        internal static HashSet<Vector2> AchPos = [];
        public AchEditor() => Ins = this;
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

        /// <summary>
        /// 用于判定包含的GE鼠标碰撞箱
        /// </summary>
        private UIAchCollision? collision;

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
            set
            {
                if (AchievementManager.PagesByMod.TryGetValue(editingMod, out var pages))
                {
                    if (pages.TryGetValue(value, out var page))
                    {
                        EditingPage = page;
                    }
                }
            }
        }
        private bool trySave;
        private bool tryDelete;
        public override void OnInitialization()
        {
            base.OnInitialization();
            if (Main.gameMenu)
                return;
            RemoveAll();

            editingMod = ProgressSystem.Instance;
            editingPanel = 0;
            RegisterEditPagePanel();
            RegisterEditAchPanel();
            RegisterNewPagePanel();
        }
        public override void Update(GameTime gt)
        {
            base.Update(gt);
            CheckKeyState();
            CheckDragging();
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
            if (slot.ach.CreatedByCode)
                return;
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
            slot.ach.Page.Remove(slot.ach);
            ChangeSaveState(false);
        }
        private void ChangeEditingAch(Achievement? ach)
        {
            if (ach == null)
            {
                EditingAchFullName = "";
                EditingCombineRequire = null;
                EditingCombineReward = null;
                achNameInputer?.ClearText();
                preCount?.ChangeText("未选", false);
                requireView?.ClearAllElements();
                rewardView?.ClearAllElements();
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
    }
}
