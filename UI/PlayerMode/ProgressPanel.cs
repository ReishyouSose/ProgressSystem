using ProgressSystem.Core.Requirements;
using ProgressSystem.Core.Rewards;
using ProgressSystem.UI.PlayerMode.ExtraUI;
using RUIModule;
using UIModSlot = ProgressSystem.UI.DeveloperMode.ExtraUI.UIModSlot;

namespace ProgressSystem.UI.PlayerMode
{
    public class ProgressPanel : ContainerElement
    {
        internal static ProgressPanel Ins = null!;
        public ProgressPanel() => Ins = this;
        private Mod selectedMod;
        private Achievement? focusAch;
        private UIContainerPanel achView;
        private UIContainerPanel descriptionView;
        private UIContainerPanel requireView;
        private UIContainerPanel rewardView;
        private UIText submit;
        private UIText receive;
        private UIVnlPanel achPanel;
        private UIVnlPanel detailsPanel;
        public override void OnInitialization()
        {
            base.OnInitialization();
            if (Main.gameMenu)
                return;
            RemoveAll();

            achPanel = new(1000, 800);
            achPanel.SetCenter(0, 0, 0.55f, 0.5f);
            achPanel.Info.SetMargin(10);
            achPanel.canDrag = true;
            Register(achPanel);

            RegisterMainPanel(achPanel);
            RegisterFocus(achPanel);
        }
        private void RegisterMainPanel(UIVnlPanel bg)
        {
            UIVnlPanel groupFilter = new(0, 0);
            groupFilter.SetSize(120, 0, 0, 1);
            groupFilter.Info.SetMargin(10);
            bg.Register(groupFilter);

            UIContainerPanel groupView = new();
            groupView.SetSize(0, 0, 1, 1);
            groupView.autoPos[0] = 10;
            groupFilter.Register(groupView);

            VerticalScrollbar gv = new(100);
            groupView.SetVerticalScrollbar(gv);
            groupFilter.Register(gv);

            int left = 130;
            UIVnlPanel eventPanel = new(0, 0);
            eventPanel.Info.SetMargin(10);
            eventPanel.SetPos(left, 40);
            eventPanel.SetSize(-left, -40, 1, 1, false);
            bg.Register(eventPanel);

            UIDropDownList<UIText> pageList = new(bg, eventPanel, x => new(x.text)) { buttonXoffset = 10 };

            pageList.showArea.SetPos(left, 0);
            pageList.showArea.SetSize(200, 30);
            pageList.showArea.SetMargin(10, 5);

            pageList.expandArea.SetPos(left, 30);
            pageList.expandArea.SetSize(200, 100);

            pageList.expandView.autoPos[0] = 5;
            left += pageList.showArea.Width + 10;

            // 挪到上面来以避免 LoadPage 有概率爆 NullReference 的 bug
            achView = new();
            achView.SetSize(-20, -20, 1, 1);
            eventPanel.Register(achView);

            VerticalScrollbar ev = new(80);
            ev.Info.Left.Pixel += 10;
            achView.SetVerticalScrollbar(ev);
            eventPanel.Register(ev);

            HorizontalScrollbar eh = new(80) { useScrollWheel = false };
            eh.Info.Top.Pixel += 10;
            achView.SetHorizontalScrollbar(eh);
            eventPanel.Register(eh);

            foreach (Mod mod in AchievementManager.PagesByMod.Keys)
            {
                if (mod.Side != ModSide.Both || mod.Assets == null || !mod.HasAsset("icon"))
                    continue;
                string modName = mod.Name;
                UIModSlot modSlot = new(RUIHelper.T2D(modName + "/icon"), modName) { hoverText = mod.DisplayName };
                modSlot.ReDraw = sb =>
                {
                    modSlot.DrawSelf(sb);
                    if (selectedMod.Name == modSlot.modName)
                    {
                        RUIHelper.DrawRec(sb, modSlot.HitBox(), 5f, Color.Red);
                    }
                };
                modSlot.Events.OnLeftDown += evt =>
                {
                    pageList.ClearAllElements();
                    ClearTemp();
                    selectedMod = ModLoader.GetMod(modSlot.modName);
                    if (AchievementManager.PagesByMod.TryGetValue(selectedMod, out var modPages))
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
            groupView.InnerUIE[0].Events.LeftDown(null);

            UIText refresh = new("刷新");
            refresh.SetPos(left, 5);
            refresh.SetSize(refresh.TextSize);
            refresh.HoverToGold();
            refresh.Events.OnLeftDown += evt =>
            {
                OnInitialization();
                Info.IsVisible = true;
            };
            bg.Register(refresh);
        }
        private Action<bool> SetFocusNeedSubmit = null!;
        private void RegisterFocus(UIVnlPanel bg)
        {
            #region 任务详情面板
            detailsPanel = new(800, 600) { canDrag = true };
            detailsPanel.SetCenter(0, 0, 0.5f, 0.5f);
            detailsPanel.Info.SetMargin(10);
            detailsPanel.Info.IsVisible = false;
            detailsPanel.Events.OnUpdate += evt =>
            {
                if (evt.ContainsPoint(Main.MouseScreen) && !bg.Info.IsLocked)
                {
                    bg.LockInteract(false);
                }
                else if (bg.Info.IsLocked)
                {
                    bg.LockInteract(true);
                }
            };
            Register(detailsPanel);
            #endregion

            #region 描述
            UIVnlPanel descriptionBg = new(0, 0);
            descriptionBg.Info.SetMargin(10);
            descriptionBg.SetSize(-5, -40, 0.33f, 1);
            detailsPanel.Register(descriptionBg);

            UIText desc = new("详情");
            desc.SetSize(desc.TextSize);
            desc.SetCenter(0, 15, 0.5f);
            descriptionBg.Register(desc);

            descriptionView = new();
            descriptionView.SetPos(0, 30);
            descriptionView.SetSize(-10, -30, 1, 1);
            descriptionView.autoPos[0] = 0;
            descriptionBg.Register(descriptionView);

            VerticalScrollbar dV = new(30);
            dV.Info.Left.Pixel += 10;
            descriptionView.SetVerticalScrollbar(dV);
            descriptionBg.Register(dV);
            #endregion

            #region 提交按钮
            submit = new("提交");
            submit.SetSize(submit.TextSize);
            submit.SetCenter(0, 5, 0.5f, 0.5f);

            UIVnlPanel submitBg = new(0, 0);
            submitBg.SetPos(0, -30, 0, 1);
            submitBg.SetSize(-5, 30, 0.33f);
            submitBg.Info.IsSensitive = true;
            submitBg.Events.OnMouseOver += evt => submit.color = Color.Gold;
            submitBg.Events.OnMouseOut += evt => submit.color = Color.White;
            submitBg.Events.OnLeftDown += evt => focusAch?.Submit();
            detailsPanel.Register(submitBg);
            submitBg.Register(submit);
            #endregion

            #region 需求
            UIVnlPanel requireBg = new(0, 0);
            requireBg.SetPos(5, 0, 0.33f, 0);
            requireBg.SetSize(-2, -40, 0.33f, 1);
            requireBg.Info.SetMargin(10);
            detailsPanel.Register(requireBg);

            UIText require = new("需求");
            require.SetSize(require.TextSize);
            require.SetCenter(0, 15, 0.5f);
            requireBg.Register(require);

            requireView = new();
            requireView.SetPos(0, 30);
            requireView.SetSize(-10, -40, 1, 1);
            requireView.autoPos[0] = 5;
            requireBg.Register(requireView);

            VerticalScrollbar requireV = new(30);
            requireV.Info.Left.Pixel += 10;
            requireView.SetVerticalScrollbar(requireV);
            requireBg.Register(requireV);

            HorizontalScrollbar requireH = new() { useScrollWheel = false };
            requireH.Info.Top.Pixel += 10;
            requireView.SetHorizontalScrollbar(requireH);
            requireBg.Register(requireH);
            #endregion

            #region 领取按钮
            receive = new("领取");
            receive.SetSize(receive.TextSize);
            receive.SetCenter(0, 5, 0.5f, 0.5f);

            UIVnlPanel receiveBg = new(0, 0);
            receiveBg.SetPos(5, -30, 0.33f, 1);
            receiveBg.SetSize(-2, 30, 0.33f);
            receiveBg.Info.IsSensitive = true;
            receiveBg.Events.OnMouseOver += evt => receive.color = Color.Gold;
            receiveBg.Events.OnMouseOut += evt => receive.color = Color.White;
            receiveBg.Events.OnLeftDown += evt =>
            {
                focusAch?.TryReceiveAllReward();
                // CheckRewards();
            };
            detailsPanel.Register(receiveBg);
            receiveBg.Register(receive);
            #endregion

            #region 奖励
            UIVnlPanel rewardBg = new(0, 0);
            rewardBg.SetPos(5, 0, 0.67f, 0);
            rewardBg.SetSize(-5, -40, 0.33f, 1);
            rewardBg.Info.SetMargin(10);
            detailsPanel.Register(rewardBg);

            UIText reward = new("奖励");
            reward.SetSize(reward.TextSize);
            reward.SetCenter(0, 15, 0.5f);
            rewardBg.Register(reward);

            rewardView = new();
            rewardView.SetPos(0, 30);
            rewardView.SetSize(-10, -40, 1, 1);
            rewardView.autoPos[0] = 5;
            rewardBg.Register(rewardView);

            VerticalScrollbar rewardV = new(30);
            rewardV.Info.Left.Pixel += 10;
            rewardView.SetVerticalScrollbar(rewardV);
            rewardBg.Register(rewardV);

            HorizontalScrollbar rewardH = new() { useScrollWheel = false };
            rewardH.Info.Top.Pixel += 10;
            rewardView.SetHorizontalScrollbar(rewardH);
            rewardBg.Register(rewardH);
            #endregion

            #region 关闭按钮
            UIText close = new("关闭");
            close.SetSize(close.TextSize);
            close.SetCenter(0, 5, 0.5f, 0.5f);

            UIVnlPanel closeBg = new(0, 0);
            closeBg.SetPos(5, -30, 0.67f, 1);
            closeBg.SetSize(-5, 30, 0.33f);
            closeBg.Info.IsSensitive = true;
            closeBg.Events.OnMouseOver += evt => close.color = Color.Gold;
            closeBg.Events.OnMouseOut += evt => close.color = Color.White;
            closeBg.Events.OnLeftDown += evt =>
            {
                detailsPanel.Info.IsVisible = false;
                bg.LockInteract(true);
            };
            detailsPanel.Register(closeBg);
            closeBg.Register(close);
            #endregion

            #region 调整三个按钮或者两个按钮
            SetFocusNeedSubmit = needSubmit =>
            {
                if (needSubmit)
                {
                    submitBg.Info.IsVisible = true;

                    receiveBg.SetPos(5, -30, 0.33f, 1);
                    receiveBg.SetSize(-2, 30, 0.33f);

                    closeBg.SetPos(5, -30, 0.67f, 1);
                    closeBg.SetSize(-5, 30, 0.33f);
                }
                else
                {
                    submitBg.Info.IsVisible = false;

                    receiveBg.SetPos(0, -30, 0, 1);
                    receiveBg.SetSize(-5, 30, 0.5f);

                    closeBg.SetPos(5, -30, 0.5f, 1);
                    closeBg.SetSize(-5, 30, 0.5f);
                }
            };
            #endregion
        }
        private void ClearTemp()
        {
            achView?.InnerUIE.RemoveAll(MatchTempGE);
            achView?.Vscroll.ForceSetPixel(0);
            achView?.Hscroll.ForceSetPixel(0);
            descriptionView?.ClearAllElements();
            requireView?.ClearAllElements();
            rewardView?.ClearAllElements();
        }
        private static bool MatchTempGE(BaseUIElement uie) => uie is UIAchSlot or UIRequireLine;
        private void LoadPage(string pageName)
        {
            ClearTemp();
            Dictionary<string, UIAchSlot> slotByFullName = [];
            var page = AchievementManager.PagesByMod[selectedMod][pageName];
            page.SetDefaultPositionForAchievements();
            foreach (Achievement ach in page.Achievements.Values)
            {
                UIAchSlot slot = new(ach);
                slot.Events.OnLeftDown += evt =>
                {
                    ChangeFocus(slot.ach);
                    slot.isFocus = true;
                };
                slot.Events.UnLeftDown += evt => slot.isFocus = false;
                achView.AddElement(slot);
                slotByFullName.Add(ach.FullName, slot);
            }
            foreach (UIAchSlot slot in slotByFullName.Values)
            {
                Achievement orig = slot.ach;
                foreach (Achievement pre in orig.Predecessors)
                {
                    RegisterRequireLine(slotByFullName, orig, pre);
                }
            }
        }
        private void RegisterRequireLine(Dictionary<string, UIAchSlot> slotByFullName, Achievement orig, Achievement pre)
        {
            UIAchSlot start = slotByFullName[pre.FullName];
            UIAchSlot end = slotByFullName[orig.FullName];
            UIRequireLine line = new(start, end);
            achView.AddElement(line, 0);
            end.preLine.Add(line);
        }
        private void ChangeFocus(Achievement? ach)
        {
            descriptionView.ClearAllElements();
            requireView.ClearAllElements();
            rewardView.ClearAllElements();
            focusAch = ach;
            if (ach != null)
            {
                SetFocusNeedSubmit(ach.NeedSubmit);
                UIText name = new(ach.DisplayName.Value ?? ach.Name);
                name.SetSize(name.TextSize);
                descriptionView.AddElement(name);

                UIText desc = new(ach.Description.Value ?? "没有描述");
                desc.SetSize(desc.TextSize);
                descriptionView.AddElement(desc);

                CheckRequirements();
                CheckRewards();
                detailsPanel.Info.IsVisible = true;
            }
        }
        private void CheckRequirements(IList<Requirement>? requires = null, int index = 0)
        {
            requires ??= focusAch!.Requirements;
            if (requires.Count != 0)
            {
                foreach (Requirement require in requires)
                {
                    UIRequireText text = new(require, requires);
                    text.SetPos(index * 30, 0);
                    requireView.AddElement(text);
                    if (require is CombineRequirement combine)
                    {
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
        private void CheckRewards(IList<Reward>? rewards = null, int index = 0)
        {
            if (index == 0)
                rewardView.ClearAllElements();
            rewards ??= focusAch!.Rewards;
            if (rewards.Count != 0)
            {
                foreach (Reward reward in rewards)
                {
                    if (reward.IsDisabled())
                        continue;
                    UIRewardText text = new(reward);
                    text.SetPos(index * 30, 0);
                    rewardView.AddElement(text);
                    if (reward is CombineReward combine)
                    {
                        CheckRewards(combine.Rewards, index + 1);
                    }
                    text.text.Events.OnMouseHover += evt =>
                    {
                        text.text.color = reward.State is Reward.StateEnum.Unlocked or Reward.StateEnum.Receiving ? Color.Gold : Color.White;
                    };
                    text.text.Events.OnMouseOut += evt => text.text.color = Color.White;
                    text.text.Events.OnLeftDown += evt =>
                    {
                        text.reward.TryReceive();
                        // CheckRewards();
                    };

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
    }
}
